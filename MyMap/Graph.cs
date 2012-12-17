using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using OSMPBF;

namespace MyMap
{
    public class Graph
    {
        // All edges are kept in memory
        ListTree<Curve> curves = new ListTree<Curve>();

        // A cache of previously requested nodes, for fast repeated access
        RBTree<Node> nodeCache = new RBTree<Node>();

        // Blob start positions indexed by containing nodes
        RBTree<long> nodeBlockIndexes = new RBTree<long>();

        string datasource;

        // Cache the latest read primitivegroups
        // Tuples are <primitiveblock, lastreadnode, howmanythnodethatwas>
        LRUCache<Tuple<PrimitiveBlock, Node, int>> cache = new LRUCache<Tuple<PrimitiveBlock, Node, int>>(100);

        public Graph(string path)
        {
            datasource = path;
            FileStream file = new FileStream(path,FileMode.Open, FileAccess.Read, FileShare.Read);

            //TODO: less misleading name, 'cuz these blocks may also have relations
            List<long> wayBlocks = new List<long>();

            while(true) {

                long blockstart = file.Position;

                BlobHeader blobHead = readBlobHeader(file);

                //EOF
                if(blobHead == null)
                    break;

                byte[] blockData = readBlockData(file, blobHead.Datasize);

                /* Note: This check is done every time, because it
                 * is not guaranteed that the first block is OSMHeader
                 */
                if(blobHead.Type == "OSMHeader")
                {
                    HeaderBlock filehead = HeaderBlock.ParseFrom(blockData);
                    for(int i = 0; i < filehead.RequiredFeaturesCount; i++)
                    {
                        string s = filehead.GetRequiredFeatures(i);
                        if(s != "DenseNodes" && s != "OsmSchema-V0.6")
                        {
                            throw new NotSupportedException(s);
                        }
                    }

                } else if(blobHead.Type == "OSMData")
                {

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        if(pg.HasDense)
                        {
                            // Remember the start of every blob with nodes
                            nodeBlockIndexes.Insert(pg.Dense.GetId(0), blockstart);
                        } else {
                            wayBlocks.Add(blockstart);
                        }
                    }
                } else
                    Console.WriteLine("Unknown blocktype: " + blobHead.Type);

            }

            foreach(long block in wayBlocks)
            {
                file.Position = block;
                BlobHeader blobHead = readBlobHeader(file);

                byte[] blockData = readBlockData(file, blobHead.Datasize);
                if(blobHead.Type == "OSMData")
                {
                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);
                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        // Insert curves in the curve tree
                        for(int j = 0; j < pg.WaysCount; j++)
                        {
                            OSMPBF.Way w = pg.GetWays(j);
                            for(int k = 1; k < w.KeysCount; k++)
                            {
                                switch(w.GetKeys(k))
                                {
                                    // TODO: STUB: fill this
                                case (uint)Keys.Highway:
                                    break;
                                }
                            }

                            List<long> nodes = new List<long>();

                            /*
                             * TODO: hier zijn vast betere manieren voor
                             */
                            long id = 0;
                            for(int k = 0; k < w.RefsCount; k++)
                            {
                                id += w.GetRefs(k);

                                nodes.Add(id);
                            }

                            Curve c = new Curve(nodes.ToArray(), "TODO");
                            foreach(long n in nodes)
                            {
                                curves.Insert(n, c);
                            }
                        }
                    }
                }
            }

            file.Close();
        }

        public Graph(string path, int cachesize) : this(path)
        {
            cache.Size = cachesize;
        }

        private BlobHeader readBlobHeader(FileStream file)
        {
            // Getting length of BlobHeader
            int blobHeadLength = 0;
            for(int i = 0; i < 4; i++)
                blobHeadLength = blobHeadLength*255 + file.ReadByte();

            // EOF after 4 reads of -1 (blobHeadLength == ((-1*255-1)*255-1)*255-1)
            if(blobHeadLength == -16646656) {
                return null;
            }

            // Reading BlobHeader
            byte[] blobHeadData = new byte[blobHeadLength];

            file.Read(blobHeadData, 0, blobHeadLength);
            return BlobHeader.ParseFrom(blobHeadData);
        }

        private byte[] readBlockData(FileStream file, int size)
        {
            byte[] blobdata = new byte[size];
            file.Read(blobdata, 0, size);
            Blob blob = Blob.ParseFrom(blobdata);

            if(!blob.HasRawSize)
            {
                return blob.Raw.ToByteArray();
            } else {
                return zlibdecompress(blob.ZlibData.ToByteArray());
            }
        }

        public Edge[] GetEdgesFromNode(long node)
        {
            List<Edge> edges = new List<Edge>();
            long start = 0, end = 0;
            foreach(Curve curve in curves.Get(node).ToArray())
            {
                foreach(long n in curve.Nodes)
                {
                    end = start;
                    start = n;
                    if(end != 0 && (start == node || end == node))
                    {
                        edges.Add(new Edge(start, end));
                    }
                }
            }
            return edges.ToArray();

            /*List<Edge> edges = new List<Edge>();
            Node start = null, end = null;
            foreach (Curve curve in curves.Get(node.ID).ToArray())
            {
                foreach (Node n in curve.Nodes)
                {
                    end = start;
                    start = n;
                    if (end != null && (start == node || end == node))
                    {
                        if (start != node)
                        {
                            end = n;
                            start = node;
                        }
                        edges.Add(new Edge(start, end));
                    }
                }
            }
            return edges.ToArray();*/
        }

        public Node[] GetNodesInBBox(BBox box)
        {
            List<Node> nds = new List<Node>();

            // TODO: Actual implementation that includes everything
            // and hopefully doesn't need O(n) time.
            foreach (Node nd in nodeCache)
            {
                if (box.Contains(nd.Longitude, nd.Latitude))
                {
                    nds.Add(nd);
                }
            }

            if(datasource != null)
            {

                // Now, check the disk (epicly slow)
                // TODO: Find a way not to have to do this 
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read, FileShare.Read);

                while(true) {
                    long blockstart = file.Position;

                    BlobHeader blobHead = readBlobHeader(file);

                    //EOF
                    if(blobHead == null)
                        break;

                    byte[] blockData = readBlockData(file, blobHead.Datasize);

                    if(blobHead.Type == "OSMData")
                    {
                        PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                        for(int i = 0; i < pb.PrimitivegroupCount; i++)
                        {
                            PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                            if(pg.HasDense)
                            {
                                long id = 0;
                                double latitude = 0;
                                double longitude = 0;
                                for(int j = 0; j < pg.Dense.IdCount; j++)
                                {
                                    id += pg.Dense.GetId(j);
                                    latitude += .000000001 * (pb.LatOffset + pb.Granularity * pg.Dense.GetLat(j));
                                    longitude += .000000001 * (pb.LonOffset + pb.Granularity * pg.Dense.GetLon(j));
                                    if(box.Contains(longitude, latitude))
                                    {
                                        Node node = new Node(longitude, latitude, id);
                                        nodeCache.Insert(id, node);
                                        nds.Add(node);
                                    }
                                }
                            }
                        }
                    }
                }

                file.Close();
            }

            return nds.ToArray();
        }

        // TODO: inefficient denk ik
        public Curve[] GetCurvesInBbox(BBox box)
        {
            Node[] curveNodes = GetNodesInBBox(box);
            Console.WriteLine(curveNodes.Length + " curvenodes");
            HashSet<Curve> set = new HashSet<Curve>();

            foreach(Node n in curveNodes)
            {
                foreach (Curve curve in curves.Get(n.ID))
                {
                    set.Add(curve);
                    curve.Type = CurveType.Road;
                }
            }
            List<Curve> res = new List<Curve>();
            res.AddRange(set);
            Console.WriteLine(res.Count + " curves");
            return res.ToArray();
        }


        //doet nu even dit maar gaat heel anders werken later
        // Hashmap? Tree? Of nog heel iets anders?
        public long GetNodeByName(string s)
        {
            foreach (Curve curve in curves)
            {
                if (curve.Name == s)
                    return curve.Start;
            }

            return 0;
        }


        /// <summary>
        /// returns the node that is the nearest to the position (longitude, latitude)
        /// TODO: be faster than O(n)
        /// </summary>
        public Node GetNodeByPos(double refLongitude, double refLatitude)
        {
            Node res = null;
            double min = double.PositiveInfinity;

            // First, check cache
            foreach (Node node in nodeCache)
            {
                double dist = (node.Latitude - refLatitude) * (node.Latitude - refLatitude) +
                    (node.Longitude - refLongitude) * (node.Longitude - refLongitude);
                if (dist < min)
                {
                    min = dist;
                    res = node;
                }
            }

            if(datasource == null)
                return res;

            // Now, check the disk (epicly slow)
            // TODO: Find a way not to have to do this 
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read, FileShare.Read);

            while(true) {
                BlobHeader blobHead = readBlobHeader(file);

                //EOF
                if(blobHead == null)
                    break;

                byte[] blockData = readBlockData(file, blobHead.Datasize);

                if(blobHead.Type == "OSMData")
                {
                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        if(pg.HasDense)
                        {
                            long id = 0;
                            double latitude = 0;
                            double longitude = 0;
                            for(int j = 0; j < pg.Dense.IdCount; j++)
                            {
                                id += pg.Dense.GetId(j);
                                latitude += .000000001 * (pb.LatOffset + pb.Granularity * pg.Dense.GetLat(j));
                                longitude += .000000001 * (pb.LonOffset + pb.Granularity * pg.Dense.GetLon(j));
                                double dist = (refLatitude - latitude) * (refLatitude - latitude) + 
                                    (refLongitude - longitude) * (refLongitude - longitude);
                                if (dist < min)
                                {
                                    min = dist;

                                    res = new Node(longitude, latitude, id);
                                    nodeCache.Insert(id, res);
                                }
                            }
                        }
                    }
                }
            }

            file.Close();
            return res;
        }


        public void ResetNodeDistance()
        {
            foreach (Node node in nodeCache)
                node.TentativeDist = double.PositiveInfinity;
        }


        /*
         * Returns the node with the given id, either from cache
         * or from disk.
         * Returns null if the node is not found on either one.
         */
        public Node GetNode(long id)
        {

            // First check if we have it in the cache
            Node n = nodeCache.Get(id);
            if(n != null)
                return n;

            if(datasource == null)
                throw new Exception("Node not found");

            // At what position in the file would our node be?
            RBNode<long> node = nodeBlockIndexes.GetNode(id);
            // That is the node that would be in the tree if the id
            // was in the tree, which it prolly isn't
            long blockToRead = node.Content;
            // The id was not in the tree, so we need to find the first
            // node with a lower id than that. Remember, the node has no
            // children.
           if(blockToRead == 0)
            {
                if (node.Parent != null)
                    node = node.Parent;

                while (node.ID > id)
                    if (node.Parent != null)
                        node = node.Parent;
                    else
                        break;
                
                blockToRead = node.Content;  
            }

            PrimitiveBlock pb = null;

            // Only read from disk if we don't have the right block in memory already
            Tuple<PrimitiveBlock, Node, int> cacheTuple = cache.Get(blockToRead);

            if(cacheTuple != null)
            {
                // Never happens if the node cache is never emptied
                if(cacheTuple.Item2.ID == id)
                    return cacheTuple.Item2;

                pb = cacheTuple.Item1;
            } else {
                // Now, check the needed block from the disk
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read, FileShare.Read);
                file.Position = blockToRead;

                BlobHeader blobHead = readBlobHeader(file);

                byte[] blockData = readBlockData(file, blobHead.Datasize);

                pb = PrimitiveBlock.ParseFrom(blockData);

                file.Close();
            }

            for(int i = 0; i < pb.PrimitivegroupCount; i++)
            {
                PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                if(pg.HasDense)
                {
                    long tmpid = 0;
                    double latitude = 0;
                    double longitude = 0;
                    int direction = 1;
                    int startpoint = 0;
                    
                    if(cacheTuple != null)
                    {
                        tmpid = cacheTuple.Item2.ID;
                        direction = id > tmpid ? 1 : -1;
                        latitude = cacheTuple.Item2.Latitude;
                        longitude = cacheTuple.Item2.Longitude;
                        startpoint = cacheTuple.Item3;
                    }

                    for(int j = startpoint; j < pg.Dense.IdCount; j += direction)
                    {
                        tmpid += direction * pg.Dense.GetId(j);
                        latitude += (double)direction * .000000001
                            * (pb.LatOffset + pb.Granularity * pg.Dense.GetLat(j));
                        longitude += (double)direction * .000000001
                            * (pb.LonOffset + pb.Granularity * pg.Dense.GetLon(j));
                        if(tmpid == id)
                        {
                            n = new Node(longitude, latitude, id);
                            nodeCache.Insert(id, n);
                            
                            cache.Add(blockToRead, Tuple.Create(pb, n, j));

                            return n;
                        }
                    }
                }
            }

            n = new Node(0, 0, id);
            nodeCache.Insert(id, n);
#if WARNING
            Console.WriteLine("Could not find node " + id);
#endif
            return n;
            //throw new Exception("Node not found");
        }

        /* TODO: deprecate
        /// <summary>
        /// Returns the index of a node item in a sorted list of nodes by it's id
        /// the used method is binary searching
        /// </summary>
        private int IndexOfId(List<Node> sortedList, int id)
        {
            int min = 0;
            int max = sortedList.Count - 1;
            int mid = (min + max) / 2;

            while (max >= min)
            {
                if (sortedList[mid].ID > id)
                    max = mid - 1;
                else if (sortedList[mid].ID < id)
                    min = mid + 1;
                else
                    return mid;

                mid = (min + max) / 2;
            }

            return -1;
        }*/

        static byte[] zlibdecompress(byte[] compressed)
        {

            MemoryStream input = new MemoryStream(compressed);
            MemoryStream output = new MemoryStream();

            // Please please please don't expect me to be able to explain why
            // this is neccessary, because I do not understand it either
            input.ReadByte();
            // Anything will do :S
            input.WriteByte((byte)'A');

            DeflateStream decompressor = new DeflateStream(input, CompressionMode.Decompress);
            decompressor.CopyTo(output);

            output.Seek(0, SeekOrigin.Begin);
            return output.ToArray();
        }

        // TODO: incomplete
        enum Keys {
            Highway = 3,
            Source = 5,
            Building = 7,
            Service = 10,
            Name = 12,
            Landuse = 13,
            Street = 21,
            Postcode = 22,
            Housenumber = 23,
            Amenity = 24,
            Foot = 26,
            Operator = 35,
            Layer = 39,
            Bicycle = 40,
            Note = 45,
            Surface = 53,
            Power = 60,
            Area = 79
        }
    }
}

