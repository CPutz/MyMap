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
        // All curves are kept in memory
        ListTree<Curve> ways = new ListTree<Curve>();
        ListTree<Curve> buildings = new ListTree<Curve>();
        ListTree<Curve> lands = new ListTree<Curve>();
        ListTree<Location> extras = new ListTree<Location>();

        // A cache of previously requested nodes, for fast repeated access
        RBTree<Node> nodeCache = new RBTree<Node>();

        // Blob start positions indexed by containing nodes
        RBTree<long> nodeBlockIndexes = new RBTree<long>();


        RBTree<long> busStations = new RBTree<long>();
        Thread busThread;

        /*
         * GeoBlocks, by lack of a better term and lack of imagination,
         * are lists of id's of nodes in a certain part of space.
         */
        double geoBlockWidth = 0.005, geoBlockHeight = 0.005;
        int horizontalGeoBlocks, verticalGeoBlocks;
        List<long>[,] wayGeoBlocks;
        List<long>[,] landGeoBlocks;
        List<long>[,] buildingGeoBlocks;
        BBox fileBounds;

        string datasource;

        // Cache the 100 last used primitivegroups
        LRUCache<PrimitiveBlock> cache = new LRUCache<PrimitiveBlock>(100);

        public Graph(string path)
        {
            List<long>[,] geoBlocks;

            datasource = path;

            // Buffer is 1024 fileblocks big
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                             FileShare.Read, 8 * 1024 * 1024);

            // List of file-positions where blocks with ways and/or
            // relations start.
            List<long> wayBlocks = new List<long>();

            // We will read the fileblocks in parallel
            List<long> blocks = new List<long>();

            Console.WriteLine("Finding blocks");

            while (true)
            {
                long blockstart = file.Position;

                BlobHeader blobHead = readBlobHeader(file);
                if (blobHead == null)
                    break;

                if (blobHead.Type == "OSMHeader")
                {
                    HeaderBlock filehead = HeaderBlock.ParseFrom(
                        readBlockData(file, blobHead.Datasize));
                    for (int i = 0; i < filehead.RequiredFeaturesCount; i++)
                    {
                        string s = filehead.GetRequiredFeatures(i);
                        if (s != "DenseNodes" && s != "OsmSchema-V0.6")
                        {
                            throw new NotSupportedException(s);
                        }
                    }

                    // The .000000001 is 'cause longs are stored
                    fileBounds = new BBox(.000000001 * filehead.Bbox.Left,
                                          .000000001 * filehead.Bbox.Top,
                                          .000000001 * filehead.Bbox.Right,
                                          .000000001 * filehead.Bbox.Bottom);

                    horizontalGeoBlocks = (int)(fileBounds.Width / geoBlockWidth) + 1;
                    verticalGeoBlocks = (int)(fileBounds.Height / geoBlockHeight) + 1;
                    Console.WriteLine("geoblocks {0}x{1}", horizontalGeoBlocks, verticalGeoBlocks);
                    
                    geoBlocks = new List<long>[horizontalGeoBlocks + 1,
                                               verticalGeoBlocks + 1];
                    wayGeoBlocks = new List<long>[horizontalGeoBlocks + 1,
                                               verticalGeoBlocks + 1];
                    landGeoBlocks = new List<long>[horizontalGeoBlocks + 1,
                                               verticalGeoBlocks + 1];
                    buildingGeoBlocks = new List<long>[horizontalGeoBlocks + 1,
                                               verticalGeoBlocks + 1];

                }
                else
                {
                    file.Position += blobHead.Datasize;

                    blocks.Add(blockstart);
                }
            }

            Console.WriteLine("Reading nodes");

            Parallel.ForEach(blocks, blockstart =>
            //foreach(long blockstart in blocks)
            {
                BlobHeader blobHead;
                byte[] blockData;

                lock (file)
                {
                    file.Position = blockstart;

                    blobHead = readBlobHeader(file);

                    // This means End Of File
                    if (blobHead == null || blobHead.Type == "OSMHeader")
                        throw new Exception("Should never happen");

                    blockData = readBlockData(file, blobHead.Datasize);
                }


                if (blobHead.Type == "OSMData")
                {

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for (int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        if (pg.HasDense)
                        {
                            // Remember the start of every blob with nodes
                            nodeBlockIndexes.Insert(pg.Dense.GetId(0), blockstart);
                            long id = 0;
                            double latitude = 0, longitude = 0;
                            for (int j = 0; j < pg.Dense.IdCount; j++)
                            {
                                id += pg.Dense.GetId(j);
                                latitude += .000000001 * (pb.LatOffset +
                                                          pb.Granularity * pg.Dense.GetLat(j));
                                longitude += .000000001 * (pb.LonOffset +
                                                           pb.Granularity * pg.Dense.GetLon(j));

                                if (fileBounds.Contains(longitude, latitude))
                                {
                                    int blockX = XBlock(longitude);
                                    int blockY = YBlock(latitude);

                                    List<long> list = geoBlocks[blockX, blockY];
                                    if (list == null)
                                        geoBlocks[blockX, blockY] = list = new List<long>();
                                    list.Add(id);
                                }
                            }
                        }
                        else
                        {
                            wayBlocks.Add(blockstart);
                        }
                    }
                }
                else
                    Console.WriteLine("Unknown blocktype: " + blobHead.Type);

            });

            Console.WriteLine("Reading ways");

            Parallel.ForEach(blocks, blockstart =>
            //foreach(long blockstart in wayBlocks)
            {
                BlobHeader blobHead;
                byte[] blockData;

                lock (file)
                {
                    file.Position = blockstart;

                    blobHead = readBlobHeader(file);

                    // This means End Of File
                    if (blobHead == null || blobHead.Type == "OSMHeader")
                        throw new Exception("Should never happen");

                    blockData = readBlockData(file, blobHead.Datasize);
                }

                if (blobHead.Type == "OSMData")
                {
                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);
                    for (int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        /*
                         * Part one: read all the curves and add them
                         */

                        // Insert curves in the curve tree
                        for (int j = 0; j < pg.WaysCount; j++)
                        {
                            CurveType type = CurveType.Unclassified;

                            OSMPBF.Way w = pg.GetWays(j);

                            string name = "";

                            for (int k = 0; k < w.KeysCount; k++)
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
                            for (int k = 0; k < w.RefsCount; k++)
                            {
                                id += w.GetRefs(k);

                                nodes.Add(id);
                            }

                            Curve c = new Curve(nodes.ToArray(), name);
                            c.Name = name;
                            c.Type = type;

                            if (type.IsStreet())
                            {
                                foreach (long n in nodes)
                                {
                                    ways.Insert(n, c);
                                }
                            }
                            else
                            {
                                if (type == CurveType.Building)
                                {
                                    foreach (long n in nodes)
                                    {
                                        buildings.Insert(n, c);
                                    }
                                }
                                else
                                {
                                    foreach (long n in nodes)
                                    {
                                        lands.Insert(n, c);
                                    }
                                }
                            }
                        }

                        /*
                         * Part two: adding bus routes and the likes
                         */

                        Parallel.For(0, pg.RelationsCount, j =>
                        //for(int j = 0; j < pg.RelationsCount; j++)
                        {
                            Relation rel = pg.GetRelations(j);

                            bool publictransport = false;
                            string name = "";

                            for (int k = 0; k < rel.KeysCount; k++)
                            {
                                string key = pb.Stringtable.GetS((int)rel.GetKeys(k)).ToStringUtf8();
                                string value = pb.Stringtable.GetS((int)rel.GetVals(k)).ToStringUtf8();

                                if (key == "route" && (value == "bus" ||
                                                      value == "trolleybus" ||
                                                      value == "share_taxi" ||
                                                      value == "tram"))
                                    publictransport = true;

                                if (key == "ref")
                                    name = value;
                            }

                            if (publictransport)
                            {
                                long id = 0;

                                List<long> nodes = new List<long>();


                                for (int k = 0; k < rel.MemidsCount; k++)
                                {
                                    id += rel.GetMemids(k);
                                    string role = pb.Stringtable.GetS((int)rel.GetRolesSid(k)).ToStringUtf8();
                                    string type = rel.GetTypes(k).ToString();

                                    //Console.WriteLine(type + " " + id + " is " + role);
                                    if (type == "NODE" && role.StartsWith("stop"))
                                    {
                                        nodes.Add(id);
                                    }
                                }

                                if (nodes.Count != 0)
                                {
                                    Curve curve = new Curve(nodes.ToArray(), name);
                                    curve.Type = CurveType.Bus;
                                    foreach (long id2 in nodes)
                                    {
                                        ways.Insert(id2, curve);

                                        if (busStations.Get(id2) == default(long))
                                        {
                                            busStations.Insert(id2, id2);
                                            extras.Insert(id2, new Location(GetNode(id2), LocationType.BusStation));
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            });

            Console.WriteLine("Sorting nodes");

            Parallel.For(0, horizontalGeoBlocks, (x) =>
                         {
                for(int y = 0; y <= verticalGeoBlocks; y++)
                {
                    if(geoBlocks[x, y] != null)
                    {
                        List<long> wayList = new List<long>();
                        List<long> landList = new List<long>();
                        List<long> buildingList = new List<long>();

                        foreach(long id in geoBlocks[x, y])
                        {

                            if(ways.Get(id).Count != 0)
                                wayList.Add(id);

                            if(lands.Get(id).Count != 0)
                                landList.Add(id);

                            if(buildings.Get(id).Count != 0)
                                buildingList.Add(id);
                        }

                        wayGeoBlocks[x, y] = wayList;
                        landGeoBlocks[x, y] = landList;
                        buildingGeoBlocks[x, y] = buildingList;
                    }
                }
            });

            file.Close();

            busThread = new Thread(new ThreadStart(() => { LoadBusses(); }));
            busThread.Start();
        }

        public Graph(string path, int cachesize) : this(path)
        {
            cache.Capacity = cachesize;
        }


        public void LoadBusses()
        {
            foreach (long id in busStations)
            {
                Node busNode = GetNode(id);


                if (busNode.Latitude != 0 && busNode.Longitude != 0)
                {
                    Node footNode = GetNodeByPos(busNode.Longitude, busNode.Latitude, Vehicle.Foot);
                    Node carNode = GetNodeByPos(busNode.Longitude, busNode.Latitude, Vehicle.Car);

                    if (footNode != null && carNode != null)
                    {
                        Curve footWay = new Curve(new long[] { footNode.ID, busNode.ID }, "Walkway to bus station");
                        footWay.Type = CurveType.Footway;
                        Curve busWay = new Curve(new long[] { carNode.ID, busNode.ID }, "Way from street to bus station");
                        busWay.Type = CurveType.Bus;

                        ways.Insert(busNode.ID, footWay);
                        ways.Insert(footNode.ID, footWay);
                        ways.Insert(busNode.ID, busWay);
                        ways.Insert(carNode.ID, busWay);
                    }
                }
            }

            Thread.CurrentThread.Abort();
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
            foreach(Curve curve in ways.Get(node))
            {
                foreach(long n in curve.Nodes)
                {
                    end = start;
                    start = n;

                    if(end != 0 && (start == node || end == node))
                    {
                        Edge e = new Edge(start, end);
                        e.Type = curve.Type;
                        edges.Add(e);
                    }
                }
            }
            return edges.ToArray();
        }

        public Node[] GetWayNodesInBBox(BBox box)
        {
            // Only search if we have data about this area
            if(!box.IntersectWith(fileBounds))
                return new Node[0];

            List<Node> nds = new List<Node>();
            int xStart = XBlock(box.XMin);
            int xEnd = XBlock(box.XMax);

            int yStart = YBlock(box.YMin);
            int yEnd = YBlock(box.YMax);

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
                    if(wayGeoBlocks[x, y] != null)
                    {
                        foreach (long id in wayGeoBlocks[x, y])
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

        public Node[] GetLandNodesInBBox(BBox box)
        {
            // Only search if we have data about this area
            if(!box.IntersectWith(fileBounds))
                return new Node[0];

            List<Node> nds = new List<Node>();
            int xStart = XBlock(box.XMin);
            int xEnd = XBlock(box.XMax);

            int yStart = YBlock(box.YMin);
            int yEnd = YBlock(box.YMax);

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
                    if(landGeoBlocks[x, y] != null)
                    {
                        foreach (long id in landGeoBlocks[x, y])
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

        public Node[] GetBuildingNodesInBBox(BBox box)
        {
            // Only search if we have data about this area
            if(!box.IntersectWith(fileBounds))
                return new Node[0];


            List<Node> nds = new List<Node>();
            int xStart = XBlock(box.XMin);
            int xEnd = XBlock(box.XMax);

            int yStart = YBlock(box.YMin);
            int yEnd = YBlock(box.YMax);

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
                    if(buildingGeoBlocks[x, y] != null)
                    {
                        foreach (long id in buildingGeoBlocks[x, y])
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

        public Curve[] GetCurvesInBBox(BBox box)
        {
            List<Curve> list = new List<Curve>();

            list.AddRange(GetLandsInBBox(box));
            list.AddRange(GetBuildingsInBBox(box));
            list.AddRange(GetWaysInBBox(box));

            return list.ToArray();
        }

        public Curve[] GetWaysInBBox(BBox box)
        {
            Node[] curveNodes = GetWayNodesInBBox(box);

            HashSet<Curve> set = new HashSet<Curve>();

            foreach(Node n in curveNodes)
            {
                foreach (Curve curve in ways.Get(n.ID))
                {
                    set.Add(curve);
                }
            }
            List<Curve> res = new List<Curve>();
            res.AddRange(set);

            return res.ToArray();
        }

        public Curve[] GetBuildingsInBBox(BBox box)
        {
            Node[] curveNodes = GetBuildingNodesInBBox(box);

            HashSet<Curve> set = new HashSet<Curve>();

            foreach(Node n in curveNodes)
            {
                foreach (Curve curve in buildings.Get(n.ID))
                {
                    set.Add(curve);
                }
            }
            List<Curve> res = new List<Curve>();
            res.AddRange(set);

            return res.ToArray();
        }

        public Curve[] GetLandsInBBox(BBox box)
        {
            Node[] curveNodes = GetLandNodesInBBox(box);

            HashSet<Curve> set = new HashSet<Curve>();

            foreach(Node n in curveNodes)
            {
                foreach (Curve curve in lands.Get(n.ID))
                {
                    set.Add(curve);
                }
            }
            List<Curve> res = new List<Curve>();
            res.AddRange(set);

            return res.ToArray();
        }


        public Location[] GetExtrasInBBox(BBox box)
        {
            Node[] nodes = GetWayNodesInBBox(box);

            HashSet<Location> set = new HashSet<Location>();

            foreach (Node n in nodes)
            {
                foreach (Location loc in extras.Get(n.ID))
                {
                    set.Add(loc);
                }
            }

            List<Location> res = new List<Location>();
            res.AddRange(set);

            return res.ToArray();
        }


        //doet nu even dit maar gaat heel anders werken later?
        // Hashmap? Tree? Of nog heel iets anders?
        public long GetNodeByName(string s)
        {
            foreach (Curve curve in ways)
            {
                if (curve.Name == s)
                    return curve.Start;
            }

            return 0;
        }


        public List<Curve> GetCurvesByName(string s)
        {
            List<Curve> res = new List<Curve>();

            foreach (Curve curve in ways)
            {
                //if (curve.Name != null && (curve.Name.StartsWith(s.ToLower()) || curve.Name.StartsWith(s.ToUpper())))
                if (curve.Name != null && curve.Name.StartsWith(s))
                    res.Add(curve);
            }

            return res;
        }


        /// <summary>
        /// Returns the node that is the nearest to the position (longitude, latitude)
        /// and where a vehicle of type v can drive.
        /// </summary>
        public Node GetNodeByPos(double refLongitude, double refLatitude, Vehicle v)
        {
            Node res = null;
            double min = double.PositiveInfinity;

            int blockX = XBlock(refLongitude);
            int blockY = YBlock(refLatitude);

            if(blockX < 0 || blockY < 0 ||
               blockX > horizontalGeoBlocks ||
               blockY > verticalGeoBlocks ||
               wayGeoBlocks[blockX, blockY] == null)
                return null;

            foreach (long id in wayGeoBlocks[blockX, blockY])
            {
                Node node = GetNode(id);

                double dist = (node.Latitude - refLatitude) * (node.Latitude - refLatitude) +
                    (node.Longitude - refLongitude) * (node.Longitude - refLongitude);
                if (dist < min)
                {
                    foreach (Curve c in ways.Get(node.ID))
                    {
                        bool allowed;

                        switch (v)
                        {
                            case Vehicle.Foot:
                                allowed = CurveTypeExtentions.FootAllowed(c.Type);
                                break;
                            case Vehicle.Bicycle:
                                allowed = CurveTypeExtentions.BicyclesAllowed(c.Type);
                                break;
                            case Vehicle.Car:
                            case Vehicle.Bus:
                                allowed = CurveTypeExtentions.CarsAllowed(c.Type);
                                break;
                            default:
                                allowed = CurveTypeExtentions.IsStreet(c.Type);
                                break;
                        }

                        if (allowed)
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
            {
                node.TentativeDist = double.PositiveInfinity;
                node.TrueDist = double.PositiveInfinity;
            }
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

        private int XBlock(double longitude)
        {
            return (int)((double)horizontalGeoBlocks
                               * fileBounds.XFraction(longitude));
        }

        private int YBlock(double latitude)
        {
            return (int)((double)verticalGeoBlocks
                               * fileBounds.YFraction(latitude));
        }
    }
}

