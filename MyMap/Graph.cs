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
            // 8M cache = 1000 blocks :D
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                             FileShare.Read, 8*1024*1024);

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
                            CurveType type = default(CurveType);

                            OSMPBF.Way w = pg.GetWays(j);
                             /*public enum CurveType
    {
        //streets
        Motorway = 22,
        Motorway_link = 21,
        Trunk = 28,
        Trunk_link = 25,
        Primary = 17,
        Primary_link = 27,
        Secondary = 34,
        Secondary_link = 13,
        Tertiary = 15,
        Tertiary_link = 14,
        Living_street = 52,
        Pedestrian = 38,
        Residential_street = 41,
        Unclassified = 8,
        Service = 49,
        Track = 42,
        Bus_guideway = 7,
        Raceway = 16,
        Road = 35,

        //landuses
        Allotments = 4,
        Basin = 6,
        Brownfield = 11,
        Cemetery = 9,
        Commercial = 19,
        Conservation = 39,
        Construction = 40,
        Farm = 36,
        Farmland = 10,
        Farmyard = 30,
        Forest = 24,
        Garages = 47,
        Grass = 23,
        Greenfield = 33,
        Greenhouse_horticulture = 51,
        Industrial = 46,
        Landfill = 50,
        Meadow = 48,
        Military = 12,
        Orchard = 43,
        Plant_nursery = 26,
        Quarry = 45,
        Railway = 29,
        Recreation_ground = 20,
        Reservoir = 37,
        Residential_land = 31,
        Retail = 32,
        Salt_pond = 18,
        Village_green = 44,
        Vineyard = 5,
        Building,
        Water
    }*/
                            for(int k = 1; k < w.KeysCount; k++)
                            {
                                string key = pb.Stringtable.GetS((int)w.GetKeys(k)).ToStringUtf8();
                                string value = pb.Stringtable.GetS((int)w.GetVals(k)).ToStringUtf8();
                                switch(key)
                                {
                                case "highway":
                                    switch(value)
                                    {
                                    case "pedestrian":
                                        type = CurveType.Pedestrian;
                                        break;
                                    case "primary":
                                        type = CurveType.Primary;
                                        break;
                                    case "primary_link":
                                        type = CurveType.Primary_link;
                                        break;
                                    case "secondary":
                                        type = CurveType.Secondary;
                                        break;
                                    case "secondary_link":
                                        type = CurveType.Secondary_link;
                                        break;
                                    case "tertiary":
                                        type = CurveType.Tertiary;
                                        break;
                                    case "tertiary_link":
                                        type = CurveType.Tertiary_link;
                                        break;
                                    case "motorway":
                                        type = CurveType.Motorway;
                                        break;
                                    case "motorway_link":
                                        type = CurveType.Motorway_link;
                                        break;
                                    case "trunk":
                                        type = CurveType.Trunk;
                                        break;
                                    case "trunk_link":
                                        type = CurveType.Trunk_link;
                                        break;
                                    case "Living_street":
                                        type = CurveType.Living_street;
                                        break;
                                    case "residential":
                                        type = CurveType.Residential_street;
                                        break;
                                    case "service":
                                        type = CurveType.Service;
                                        break;
                                    case "unclassified":
                                        type = CurveType.Unclassified;
                                        break;
                                    case "bus_guideway":
                                        type = CurveType.Bus_guideway;
                                        break;
                                    case "raceway":
                                        type = CurveType.Raceway;
                                        break;
                                    case "road":
                                        type = CurveType.Road;
                                        break;
                                    case "cycleway":
                                        type = CurveType.Cycleway;
                                        break;
                                    case "construction":
                                        type = CurveType.Construction_street;
                                        break;
                                    case "path":
                                        type = CurveType.Path;
                                        break;
                                    case "footway":
                                        type = CurveType.Footway;
                                        break;
                                    default:
                                        Console.WriteLine("TODO: implement highway=" + value);
                                        break;
                                    }
                                    break;
                                case "landuse":
                                    switch(value)
                                    {
                                    case "recreation_centre":
                                        type = CurveType.Recreation_ground;
                                        break;
                                    case "construction":
                                        type = CurveType.Construction_land;
                                        break;
                                    case "grass":
                                        type = CurveType.Grass;
                                        break;
                                    case "forest":
                                        type = CurveType.Forest;
                                        break;
                                    case "farm":
                                        type = CurveType.Farm;
                                        break;
                                    default:
                                        Console.WriteLine("TODO: implement landuse=" + value);
                                        break;
                                    }
                                    break;
                                case "building":
                                    type = CurveType.Building;
                                    break;
                                case "water":
                                     type = CurveType.Water;
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
                            c.Type = type;

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
                // 8M cache = 1000 blocks :D
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                                 FileShare.Read, 8*1024*1024);

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
                    //curve.Type = CurveType.Road;
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
            // 8M cache = 1000 blocks :D
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                             FileShare.Read, 8*1024*1024);

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
                // Cache genoeg om zeker het blok te bevatten
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                                 FileShare.Read, 8600);

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

