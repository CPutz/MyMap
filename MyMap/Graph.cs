#define DEBUG

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
        ListTree<Edge> edges = new ListTree<Edge>();

        // A cache of previously requested nodes, for fast repeated access
        RBTree<Node> nodeCache = new RBTree<Node>();

        // Blob start positions indexed by containing nodes
        RBTree<long> nodeBlockIndexes = new RBTree<long>();

        string datasource;


        // Cache the latest read primitivegroup
        long cacheFilePosition;
        PrimitiveBlock cache;

        public Graph()
        {
            CreateFakeEdges();
        }

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

                    cache = PrimitiveBlock.ParseFrom(blockData);
                    cacheFilePosition = blockstart;

                    for(int i = 0; i < cache.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = cache.GetPrimitivegroup(i);

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
                    cache = PrimitiveBlock.ParseFrom(blockData);
                    cacheFilePosition = block;
                    for(int i = 0; i < cache.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = cache.GetPrimitivegroup(i);

                        // Insert edges in the edge tree
                        for(int j = 0; j < pg.WaysCount; j++)
                        {
                            OSMPBF.Way w = pg.GetWays(j);
                            long id1 = w.GetRefs(0);
                            long id2 = id1;
                            for(int k = 1; k < w.RefsCount; k++)
                            {
                                id1 += w.GetRefs(k);
                                Edge e = new Edge(GetNode(id1), GetNode(id2), "TODO");
                                edges.Insert(id1, e);
                                edges.Insert(id2, e);
                                id2 = id1;
                            }
                        }
                    }
                }
            }

            file.Close();
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

        //temporary function for testing
        //sets up a network of random edges
        private void CreateFakeEdges()
        {
            Random rand = new Random();
            double width = 0.0065, height = 0.0065;
            double offsetX = 5.1630, offsetY = 52.0855;
            int id = 1;
            int numOfPoints = 100;
            double d = (double)((Math.Min(width, height)) / (numOfPoints - 1));
            int i_d = (int)(100000000 * d);

            for (double x = offsetX; x < offsetX + d * numOfPoints; x += d)
            {
                for (double y = offsetY; y < offsetY + d * numOfPoints; y += d)
                {
                    nodeCache.Insert(id, new Node(x + rand.Next(-i_d / 4, i_d / 4) / 100000000d,
                                                  y + rand.Next(-i_d / 2, i_d / 2) / 100000000d, id));
                    id++;
                }
            }

            for (int i = 1; i < nodeCache.Count; i++)
            {
                if (i <= nodeCache.Count - numOfPoints)
                {
                    Edge newEdge = new Edge((Node)nodeCache.GetNode(i).Content,
                                            (Node)nodeCache.GetNode(i + numOfPoints).Content, "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) *
                        (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) *
                            (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
                if (i % numOfPoints != 0)
                {
                    Edge newEdge = new Edge((Node)nodeCache.GetNode(i).Content,
                                            (Node)nodeCache.GetNode(i + 1).Content, "");
                    double time = Math.Sqrt((newEdge.Start.Longitude - newEdge.End.Longitude) *
                        (newEdge.Start.Longitude - newEdge.End.Longitude) +
                                  (newEdge.Start.Latitude - newEdge.End.Latitude) *
                            (newEdge.Start.Latitude - newEdge.End.Latitude));
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
            }
        }



        public Edge[] GetEdgesFromNode(Node node)
        {
            return edges.Get(node.ID).ToArray();
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
                        cache = PrimitiveBlock.ParseFrom(blockData);
                        cacheFilePosition = blockstart;

                        for(int i = 0; i < cache.PrimitivegroupCount; i++)
                        {
                            PrimitiveGroup pg = cache.GetPrimitivegroup(i);

                            if(pg.HasDense)
                            {
                                long id = 0;
                                double latitude = 0;
                                double longitude = 0;
                                for(int j = 0; j < pg.Dense.IdCount; j++)
                                {
                                    id += pg.Dense.GetId(j);
                                    latitude += .000000001 * (cache.LatOffset + cache.Granularity * pg.Dense.GetLat(j));
                                    longitude += .000000001 * (cache.LonOffset + cache.Granularity * pg.Dense.GetLon(j));
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

        // tijdelijk
        public Curve[] GetCurvesInBbox(BBox box)
        {
            Node[] curveNodes = GetNodesInBBox(box);
            Console.WriteLine(curveNodes.Length + " curvenodes");
            List<Curve> res = new List<Curve>();


            for (int i = 0; i < curveNodes.Length; i++)
            {
                Edge[] e = GetEdgesFromNode(curveNodes[i]);

                foreach (Edge edge in e)
                {
                    Curve c = new Curve(new Node[] { edge.Start, edge.End }, "");
                    c.Type = CurveType.Road;
                    res.Add(c);
                }
            }
            return res.ToArray();
        }


        //doet nu even dit maar gaat heel anders werken later
        // Hashmap? Tree? Of nog heel iets anders?
        public Node GetNodeByName(string s)
        {
            Node res = null;

            foreach (Edge edge in edges)
            {
                if (edge.Name == s)
                    return edge.Start;
            }

            return res;
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

            // Only read from disk if we don't have the right block in memory already
            if(cacheFilePosition != blockToRead)
            {

                // Now, check the needed block from the disk
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read, FileShare.Read);
                file.Position = blockToRead;

                BlobHeader blobHead = readBlobHeader(file);

                byte[] blockData = readBlockData(file, blobHead.Datasize);

                cache = PrimitiveBlock.ParseFrom(blockData);

                file.Close();
            }

            for(int i = 0; i < cache.PrimitivegroupCount; i++)
            {
                PrimitiveGroup pg = cache.GetPrimitivegroup(i);

                if(pg.HasDense)
                {
                    long tmpid = 0;
                    double latitude = 0;
                    double longitude = 0;
                    for(int j = 0; j < pg.Dense.IdCount; j++)
                    {
                        tmpid += pg.Dense.GetId(j);
                        latitude += .000000001 * (cache.LatOffset + cache.Granularity * pg.Dense.GetLat(j));
                        longitude += .000000001 * (cache.LonOffset + cache.Granularity * pg.Dense.GetLon(j));
                        if(tmpid == id)
                        {
                            n = new Node(longitude, latitude, id);
                            nodeCache.Insert(id, n);
                            return n;
                        }
                    }
                }
            }

            throw new Exception("Node not found");
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
    }
}

