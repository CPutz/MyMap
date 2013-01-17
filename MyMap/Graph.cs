using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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


        // A List that contains all busRoutes as straight lines.
        // This is a list so it is possible to remove items from it.
        List<Edge> abstractBusWays = new List<Edge>();
        RBTree<Node> busStations = new RBTree<Node>();
        Thread busThread;

        /*
         * GeoBlocks, by lack of a better term and lack of imagination,
         * are lists of id's of nodes in a certain part of space.
         */
        double geoBlockWidth = 0.05, geoBlockHeight = 0.05;
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
            List<long>[,] geoBlocks = null;

            datasource = path;

            // Buffer is 1024 fileblocks big
            FileStream file = new FileStream(datasource, FileMode.Open, FileAccess.Read,
                                             FileShare.Read, 8 * 1024 * 1024);

            // List of file-positions where blocks with ways and/or
            // relations start.
            List<long> wayBlocks = new List<long>();

            // We will read the fileblocks in parallel
            List<long> blocks = new List<long>();

            List<long> busNodes = new List<long>();
            List<string> busNames = new List<string>();

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
                //else
                    //Console.WriteLine("Unknown blocktype: " + blobHead.Type);

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
                            CurveType type = CurveType.UnTested;

                            OSMPBF.Way w = pg.GetWays(j);

                            string name = "";
                            int maxSpeed = 0;

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
                                            case "track":
                                                type = CurveType.Track;
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
                                        type = CurveType.Building;
                                        break;
                                    case "natural":
                                        if (value == "water")
                                            type = CurveType.Water;
                                        break;
                                    case "name":
                                        name = value;
                                        break;
                                    case "maxspeed":
                                        int.TryParse(value, out maxSpeed);
                                        break;
                                    case "amenity":
                                        if (value == "parking")
                                            type = CurveType.Parking;
                                        break;
                                    // Not used by us:
                                    case "source":
                                    case "3dshapes:ggmodelk":
                                    case "created_by":
                                        break;
                                    default:
                                        if (key.StartsWith("building"))
                                        {
                                            type = CurveType.Building;
                                        }
                                        //Console.WriteLine("TODO: key= " + key + ", with value= " + value);
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

                                if (maxSpeed > 0)
                                {
                                    c.MaxSpeed = maxSpeed;
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

                        ListTree<long> endIdStartId = new ListTree<long>();

                        //Parallel.For(0, pg.RelationsCount, j =>
                        for(int j = 0; j < pg.RelationsCount; j++)
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

                                //List<long> nodes = new List<long>();


                                for (int k = 0; k < rel.MemidsCount; k++)
                                {
                                    id += rel.GetMemids(k);
                                    string role = pb.Stringtable.GetS((int)rel.GetRolesSid(k)).ToStringUtf8();
                                    string type = rel.GetTypes(k).ToString();

                                    //Console.WriteLine(type + " " + id + " is " + role);
                                    if (type == "NODE" && role.StartsWith("stop"))
                                    {
                                        busNodes.Add(id);
                                        busNames.Add(name);
                                    }
                                }

                                for (int l = 0; l < busNodes.Count - 1; l++)
                                {
                                    Edge e = new Edge(busNodes[l], busNodes[l + 1]);
                                    e.Type = CurveType.AbstractBusRoute;

                                    if (!endIdStartId.Get(e.End).Contains(e.Start))
                                    {
                                        abstractBusWays.Add(e);
                                        endIdStartId.Insert(e.End, e.Start);
                                    }
                                }
                                Edge e2 = new Edge(busNodes[busNodes.Count - 1], 0);
                                e2.Type = CurveType.AbstractBusRoute;
                                abstractBusWays.Add(e2);
                                endIdStartId.Insert(busNodes[busNodes.Count - 1], 0);
                            }
                        }
                    }
                }
            });


            file.Close();

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

            Console.WriteLine("Routing busses");

            /*if (busNodes.Count > 0)
            {
                RouteFinder rf = new RouteFinder(this);

                for (int l = 0; l < busNodes.Count; l++)
                {
                    Route r;
                    Curve curve = null;
                    int next = l + 1;

                    if (next >= busNodes.Count)
                        next = 0;


                    //tijdelijk!
                    Node n1 = GetNode(busNodes[l]);
                    Node n2 = GetNode(busNodes[next]);


                    if (n1.Longitude == 0 || n2.Longitude == 0)
                    {

                    }
                    else
                    {
                        Node street1 = GetNodeByPos(n1.Longitude, n1.Latitude, Vehicle.Bus);
                        Node street2 = GetNodeByPos(n2.Longitude, n2.Latitude, Vehicle.Bus);

                        if (street1 != default(Node) && street2 != default(Node))
                        {
                            curve = new Curve(new long[] { busNodes[l], busNodes[next] }, busNames[l]);
                            //curve = new BusCurve(new long[] { street1.ID, street2.ID }, name);

                            //r = rf.Dijkstra(nodes[l], nodes[next], Vehicle.Bus, RouteMode.Fastest);
                            r = rf.Dijkstra(street1.ID, street2.ID, new Vehicle[] { Vehicle.Bus }, RouteMode.Fastest, false);

                            r = new Route(new Node[] { n1 }, Vehicle.Bus) + r + new Route(new Node[] { n2 }, Vehicle.Bus);

                            curve.Type = CurveType.Bus;
                            curve.Route = r;

                            // We calculate with 30 seconds of waiting time for the bus
                            r.Time += 30;

                            ways.Insert(busNodes[l], curve);
                            ways.Insert(busNodes[next], curve);
                        }
                    }

                    if (busStations.Get(busNodes[l]) == null)
                    {
                        Node n = GetNode(busNodes[l]);

                        if (n.Longitude != 0 && n.Latitude != 0)
                        {
                            busStations.Insert(busNodes[l], n);
                            extras.Insert(busNodes[l], new Location(n, LocationType.BusStation));
                        }
                    }
                }
            }*/

            // Add busstations
            for (int i = 0; i < busNodes.Count; i++)
            {
                if (busStations.Get(busNodes[i]) == null)
                {
                    Node n = GetNode(busNodes[i]);

                    if (n.Longitude != 0 && n.Latitude != 0)
                    {
                        busStations.Insert(busNodes[i], n);
                        extras.Insert(busNodes[i], new Location(n, LocationType.BusStation));
                    }
                }
            }

            busThread = new Thread(new ThreadStart(() => { LoadBusses(); }));
            busThread.Start();
        }

        public Graph(string path, int cachesize) : this(path)
        {
            cache.Capacity = cachesize;
        }


        public void LoadBusses()
        {
            foreach (Node busNode in busStations)
            {
                Node footNode = GetNodeByPos(busNode.Longitude, busNode.Latitude, Vehicle.Foot, new List<long> { busNode.ID });
                Node carNode = GetNodeByPos(busNode.Longitude, busNode.Latitude, Vehicle.Car, new List<long> { busNode.ID });

                if (footNode != null && carNode != null)
                {
                    Curve footWay = new Curve(new long[] { footNode.ID, busNode.ID }, "Walkway to bus station");
                    footWay.Type = CurveType.Bus;
                    Curve busWay = new Curve(new long[] { carNode.ID, busNode.ID }, "Way from street to bus station");
                    busWay.Type = CurveType.Bus;

                    ways.Insert(busNode.ID, footWay);
                    ways.Insert(footNode.ID, footWay);
                    ways.Insert(busNode.ID, busWay);
                    ways.Insert(carNode.ID, busWay);
                }
            }

            Thread.CurrentThread.Abort();
        }


        public void AddWay(long identifier, Curve c)
        {
            ways.Insert(identifier, c);
        }


        public bool isBusStation(long identifier)
        {
            return busStations.Get(identifier) != null;
        }

        public List<Edge> GetAbstractBusEdges()
        {
            return abstractBusWays;
        }

        public void RemoveAbstractBus(Edge e)
        {
            abstractBusWays.Remove(e);
        }


        public BBox FileBounds
        {
            get { return fileBounds; }
        }


        public List<long> GetExtras()
        {
            List<long> res = new List<long>();

            foreach (Location location in extras)
            {
                res.Add(location.ID);
            }

            return res;
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
            
            foreach(Curve curve in ways.Get(node))
            {
                long start = 0, end = 0;

                foreach(long n in curve.Nodes)
                {
                    end = start;
                    start = n;

                    if(end != 0 && (start == node || end == node))
                    {
                        Edge e = new Edge(start, end);
                        e.Type = curve.Type;
                        e.name = curve.Name;

                        if (curve.Type == CurveType.Bus && curve.Route != null)
                            e.Route = curve.Route;

                        if (curve.MaxSpeed > 0)
                            e.MaxSpeed = curve.MaxSpeed;

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
            List<Location> res = new List<Location>();

            foreach (Location l in extras)
            {
                if (box.Contains(l.Longitude, l.Latitude))
                    res.Add(l);
            }

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
            return GetNodeByPos(refLongitude, refLatitude, v, new List<long>());
        }


        /// <summary>
        /// Returns the node that is the nearest to the position (longitude, latitude)
        /// and where a vehicle of type v can drive
        /// </summary>
        public Node GetNodeByPos(double refLongitude, double refLatitude,
                                 Vehicle v, List<long> exceptions)
        {
            return GetNodeByPos(refLongitude, refLatitude, v, exceptions, 0);
        }

        private Node GetNodeByPos(double refLongitude, double refLatitude,
                                 Vehicle v, List<long> exceptions, int blocksExtra)
        {
            Node res = null;
            double min = double.PositiveInfinity;

            int blockX = XBlock(refLongitude);
            int blockY = YBlock(refLatitude);

            int minBlockX = Math.Max(blockX - blocksExtra, 0);
            int maxBlockX = Math.Min(blockX + blocksExtra, horizontalGeoBlocks - 1);

            int minBlockY = Math.Max(blockY - blocksExtra, 0);
            int maxBlockY = Math.Min(blockY + blocksExtra, verticalGeoBlocks - 1);

            // TODO: Could be done parallel I guess
            for(int x = minBlockX; x <= maxBlockX; x++)
            {
                for(int y = minBlockY; y <= maxBlockY; y++)
                {
                    foreach (long id in wayGeoBlocks[x, y])
                    {
                        if (!exceptions.Contains(id))
                        {
                            Node node = GetNode(id);

                            double dist = DistanceSquared(node.Latitude - refLatitude,
                                                          node.Longitude - refLongitude);

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
                    }
                }
            }

            // Closest edge should be farther than found node
            double nearestEdge = Math.Min(Math.Min(
                Math.Abs(refLongitude - minBlockX * geoBlockWidth - fileBounds.XMin),
                Math.Abs((maxBlockX + 1) * geoBlockWidth + fileBounds.XMin - refLongitude)),
                     Math.Min(
                Math.Abs(refLatitude - minBlockY * geoBlockHeight - fileBounds.YMin),
                Math.Abs((maxBlockY + 1) * geoBlockHeight + fileBounds.YMin - refLatitude)));
            if(nearestEdge > Math.Sqrt(min))
                return res;

            // No good answer found, try searching wider
            if (blocksExtra < 10)
                return GetNodeByPos(refLongitude, refLongitude, v, exceptions, blocksExtra + 1);
            else
                return default(Node);
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

        private static double DistanceSquared(double a, double b)
        {
            return a*a + b*b;
        }
    }
}

