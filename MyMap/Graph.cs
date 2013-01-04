using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
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

        /*
         * GeoBlocks, by lack of a better term and lack of imagination,
         * are lists of id's of nodes in a certain part of space.
         */
        double geoBlockWidth = 0.01, geoBlockHeight = 0.01;
        int horizontalGeoBlocks, verticalGeoBlocks;
        List<long>[,] geoBlocks;
        BBox fileBounds;

        string datasource;

        // Cache the 100 last used primitivegroups
        LRUCache<PrimitiveBlock> cache = new LRUCache<PrimitiveBlock>(100);

        public Graph(string path)
        {
            datasource = path;

            // Buffer is 8 fileblocks big
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                             FileShare.Read, 8*8*1024);

            // List of file-positions where blocks with ways and/or
            // relations start.
            List<long> wayBlocks = new List<long>();

            while(true) {

                long blockstart = file.Position;

                BlobHeader blobHead = readBlobHeader(file);

                // This means End Of File
                if(blobHead == null)
                    break;

                byte[] blockData = readBlockData(file, blobHead.Datasize);


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

                    // The .000000001 is cause people prever longs over doubles
                    fileBounds = new BBox(.000000001 * filehead.Bbox.Left,
                                          .000000001 * filehead.Bbox.Top,
                                          .000000001 * filehead.Bbox.Right,
                                          .000000001 * filehead.Bbox.Bottom);

                    horizontalGeoBlocks = (int)(fileBounds.Width / geoBlockWidth) + 1;
                    verticalGeoBlocks = (int)(fileBounds.Height / geoBlockHeight) + 1;

                    geoBlocks = new List<long>[horizontalGeoBlocks + 1,
                                               verticalGeoBlocks + 1];

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
                            long id = 0;
                            double latitude = 0, longitude = 0;
                            for(int j = 0; j < pg.Dense.IdCount; j++)
                            {
                                id += pg.Dense.GetId(j);
                                latitude += .000000001 * (pb.LatOffset +
                                                          pb.Granularity * pg.Dense.GetLat(j));
                                longitude += .000000001 * (pb.LonOffset +
                                                           pb.Granularity * pg.Dense.GetLon(j));

                                if(fileBounds.Contains(longitude, latitude))
                                {
                                    int blockX = (int)((double)horizontalGeoBlocks
                                                       * fileBounds.XFraction(longitude));
                                    int blockY = (int)((double)verticalGeoBlocks
                                                       * fileBounds.YFraction(latitude));

                                    List<long> list = geoBlocks[blockX, blockY];
                                    if(list == null)
                                        geoBlocks[blockX, blockY] = list = new List<long>();
                                    list.Add(id);
                                }
                            }
                        } else {
                            wayBlocks.Add(blockstart);
                        }
                    }
                } //else
                      //Console.WriteLine("Unknown blocktype: " + blobHead.Type);

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

                            string name = "";

                            for(int k = 0; k < w.KeysCount; k++)
                            {
                                string key = pb.Stringtable.GetS(
                                    (int)w.GetKeys(k)).ToStringUtf8();
                                string value = pb.Stringtable.GetS(
                                    (int)w.GetVals(k)).ToStringUtf8();

                                switch (key)
                                {
                                    case "highway":
                                        switch (value)
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
                                            case "proposed":
                                                type = CurveType.Proposed;
                                                break;
                                            case "steps":
                                                type = CurveType.Steps;
                                                break;
                                            default:
                                                //Console.WriteLine("TODO: highway=" + value);
                                                break;
                                        }
                                        break;
                                    case "landuse":
                                        switch (value)
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
                                            case "orchard":
                                                type = CurveType.Orchard;
                                                break;
                                            case "basin":
                                                type = CurveType.Basin;
                                                break;
                                            case "allotments":
                                                type = CurveType.Allotments;
                                                break;
                                            case "pond":
                                                type = CurveType.Salt_pond;
                                                break;
                                            case "military":
                                                type = CurveType.Military;
                                                break;
                                            default:
                                                //Console.WriteLine("TODO: landuse=" + value);
                                                break;
                                        }
                                        break;
                                    case "building":
                                        if (value == "yes")
                                            type = CurveType.Building;
                                        break;
                                    case "natural":
                                    if (value == "water")
                                        type = CurveType.Water;
                                        break;
                                    case "name":
                                        name = value;
                                        break;
                                    // Not used by us:
                                    case "source":
                                    case "3dshapes:ggmodelk":
                                    case "created_by":
                                        break;
                                    default:
                                        //Console.WriteLine("TODO: key=" + key);
                                        break;
                                }
                            }

                            // Nodes in this way
                            List<long> nodes = new List<long>();

                            long id = 0;
                            for(int k = 0; k < w.RefsCount; k++)
                            {
                                id += w.GetRefs(k);

                                nodes.Add(id);
                            }

                            if (type != default(CurveType))
                            {
                                Curve c = new Curve(nodes.ToArray(), name);
                                c.Type = type;

                                foreach (long n in nodes)
                                {
                                    curves.Insert(n, c);
                                }
                            }
                        }
                    }
                }
            }

            file.Close();
        }

        public Graph(string path, int cachesize) : this(path)
        {
            cache.Capacity = cachesize;
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

            if(blob.HasRaw)
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
            foreach(Curve curve in curves.Get(node))
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
            // Only search if we have data about this area
            if(!box.IntersectWith(fileBounds))
                return new Node[0];

            List<Node> nds = new List<Node>();
            int xStart = (int)(fileBounds.XFraction(box.XMin)
                               * (double)horizontalGeoBlocks);
            int xEnd = (int)(fileBounds.XFraction(box.XMax)
                             * (double)horizontalGeoBlocks);

            int yStart = (int)(fileBounds.YFraction(box.YMin)
                               * (double)verticalGeoBlocks);
            int yEnd = (int)(fileBounds.YFraction(box.YMax)
                             * (double)verticalGeoBlocks);

            if(xStart < 0)
                xStart = 0;
            if(xEnd >= horizontalGeoBlocks)
                xEnd = horizontalGeoBlocks;
            if(yStart < 0)
                yStart = 0;
            if(yEnd >= verticalGeoBlocks)
                yEnd = verticalGeoBlocks;

            for(int x = xStart; x <= xEnd; x++)
            {
                for(int y = yStart; y <= yEnd; y++)
                {
                    if(geoBlocks[x, y] != null)
                    {
                        foreach (long id in geoBlocks[x, y])
                        {
                            Node nd = GetNode(id);
                            if (box.Contains(nd.Longitude, nd.Latitude))
                            {
                                nds.Add(nd);
                            }
                        }
                    }
                }
            }

            return nds.ToArray();
        }

        public Curve[] GetCurvesInBbox(BBox box)
        {
            Node[] curveNodes = GetNodesInBBox(box);

            HashSet<Curve> set = new HashSet<Curve>();

            foreach(Node n in curveNodes)
            {
                foreach (Curve curve in curves.Get(n.ID))
                {
                    set.Add(curve);
                }
            }
            List<Curve> res = new List<Curve>();
            res.AddRange(set);

            return res.ToArray();
        }


        //doet nu even dit maar gaat heel anders werken later?
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
        /// </summary>
        public Node GetNodeByPos(double refLongitude, double refLatitude)
        {
            Node res = null;
            double min = double.PositiveInfinity;
            
            int blockX = (int)(fileBounds.XFraction(refLongitude)
                               * (double)horizontalGeoBlocks);
            int blockY = (int)(fileBounds.YFraction(refLatitude)
                               * (double)verticalGeoBlocks);

            if(geoBlocks[blockX,blockY] == null)
                return null;

            foreach (long id in geoBlocks[blockX, blockY])
            {
                Node node = GetNode(id);

                double dist = (node.Latitude - refLatitude) * (node.Latitude - refLatitude) +
                    (node.Longitude - refLongitude) * (node.Longitude - refLongitude);
                if (dist < min)
                {
                    foreach (Curve c in curves.Get(node.ID))
                    {
                        if (CurveTypeExtentions.IsStreet(c.Type))
                        {
                            min = dist;
                            res = node;
                            break;
                        }
                    }
                }
            }

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

            PrimitiveBlock pb = cache.Get(blockToRead);

            if(pb == null)
            {
                // Now, check the needed block from the disk
                FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                                 FileShare.Read, 8600);

                file.Position = blockToRead;

                BlobHeader blobHead = readBlobHeader(file);

                byte[] blockData = readBlockData(file, blobHead.Datasize);

                pb = PrimitiveBlock.ParseFrom(blockData);

                file.Close();

                cache.Add(blockToRead, pb);
            }

            for(int i = 0; i < pb.PrimitivegroupCount; i++)
            {
                PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                if(pg.HasDense)
                {
                    long tmpid = 0;
                    double latitude = 0;
                    double longitude = 0;

                    for(int j = 0; j < pg.Dense.IdCount; j++)
                    {
                        tmpid += pg.Dense.GetId(j);
                        latitude += .000000001 * (pb.LatOffset +
                                                  pb.Granularity * pg.Dense.GetLat(j));
                        longitude += .000000001 * (pb.LonOffset +
                                                   pb.Granularity * pg.Dense.GetLon(j));
                        if(tmpid == id)
                        {
                            n = new Node(longitude, latitude, id);
                            nodeCache.Insert(id, n);

                            return n;                           
                        }
                    }
                }
            }

            n = new Node(0, 0, id);
            nodeCache.Insert(id, n);

            return n;
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

