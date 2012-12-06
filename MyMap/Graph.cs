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

#warning Gekke code
            /* TODO
             * Waarom wordt de routefinder in de graph class gemaakt?
             * Dat klinkt mij een beetje raar eerlijk gezegt..
             */
            RouteFinder rf = new RouteFinder();

            // Dummy, first and last node in the treee
            Console.WriteLine(rf.Dijkstra(this, nodeCache.GetNode(-100000000).Parent.Content,
                        nodeCache.GetNode(1000000000000000).Parent.Content, new Vehicle()));
        }

        public Graph(string path)
        {
            datasource = path;
            FileStream file = new FileStream(path, FileMode.Open);
            while(true) {

                long blockstart = file.Position;

                BlobHeader blobHead = readBlobHeader(file);

                //EOF
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

                } else if(blobHead.Type == "OSMData")
                {

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);

                        // Insert nodes in node tree
                        if(pg.HasDense)
                        {
                            nodeBlockIndexes.Insert(pg.Dense.GetId(0), blockstart);

                            double longitude = 0;
                            double latitude = 0;
                            long id = 0;

                            for(int j = 0; j < pg.Dense.LonCount; j++)
                            {
                                longitude += .000000001 *
                                    (pb.LonOffset +
                                     (pb.Granularity * pg.Dense.GetLon(i)));
                                
                                latitude += .000000001 *
                                    (pb.LatOffset +
                                     (pb.Granularity * pg.Dense.GetLat(i)));

                                id += pg.Dense.GetId(i);
                            }
                        } else {

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
                } else
                    Console.WriteLine("Unknown blocktype: " + blobHead.Type);

            }
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
            int width = 500, height = 500;
            int id = 0;
            int numOfPoints = 3;
            int d = (int)((Math.Min(width, height)) / (numOfPoints - 1));


            for (int x = 0; x < d * numOfPoints; x += d)
            {
                for (int y = 0; y < d * numOfPoints; y += d)
                {
                    nodeCache.Insert(id, new Node(x + rand.Next(-d / 2, d / 2), y + rand.Next(-d / 2, d / 2), id));
                    id++;
                }
            }

            for (int i = 0; i < nodeCache.Count; i++)
            {
                if (i < nodeCache.Count - numOfPoints)
                {
                    Edge newEdge = new Edge((Node)nodeCache.GetNode(i).Content,
                                            (Node)nodeCache.GetNode(i + numOfPoints).Content, "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
                if (i % numOfPoints != numOfPoints - 1)
                {
                    Edge newEdge = new Edge((Node)nodeCache.GetNode(i).Content,
                                            (Node)nodeCache.GetNode(i + 1).Content, "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
            }
        }


        //tijdelijk
        public Edge[] GetEdgesFromNode(Node node)
        {
            RBNode<List<Edge>> n = edges.GetNode(node.ID);
            if(n.Content == null)
                n = n.Parent;
            Console.WriteLine("returning " + n.Content.Count + " elements");
            return n.Content.ToArray();
        }


        //tijdelijk
        public Curve[] GetCurvesInBBox(BBox box)
        {
            List<Curve> curves = new List<Curve>();

            foreach (Edge edge in edges)
            {
                if (box.Contains(edge.Start.Longitude, edge.Start.Latitude) || box.Contains(edge.End.Longitude, edge.End.Latitude))
                {
                    Node[] nds = { edge.Start, edge.End };
                    Curve newCurve = new Curve(nds, edge.Name);
                    newCurve.Type = CurveType.Street;
                    curves.Add(newCurve);
                }
            }

            return curves.ToArray();
        }


        /*
         * Hoezo zijn er een GetCurvesInBBox en een GetNodesInBBox? 1 van de twee moet voldoen...
         */
        public Node[] GetNodesInBBox(BBox box)
        {
            List<Node> nds = new List<Node>();

            foreach (Node nd in nodeCache)
            {
                if (box.Contains(nd.Longitude, nd.Latitude))
                {
                    nds.Add(nd);
                }
            }

            return nds.ToArray();
        }


        //doet nu even dit maar gaat heel anders werken later
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
        /// </summary>
        public Node GetNodeByPos(double longitude, double latitude)
        {
            Node res = null;
            double min = 0;

            foreach (Node node in nodeCache)
            {
                if (node.Latitude * node.Latitude + node.Longitude * node.Longitude < min)
                    res = node;
            }

            return res;
        }

        public Node GetNode(long id)
        {
            // First check if we have it in the cache
            Node n = nodeCache.Get(id);
            if(n != null)
                return n;

            // The starting positions of blocks of nodes are stored in memory
            RBNode<long> indexNode = nodeBlockIndexes.GetNode(id);

            /* Only the first nodes of every block are in the tree, so if
             * we need another one, we need the parent of the found RBNode.
             */
            if(indexNode.Content == default(long))
                indexNode = indexNode.Parent;

            /* The most recent PrimitiveGroup is cached, because nodes
             * tend to group by id.
             * So, this if makes sure we only read the file when needed.
             */
            if(indexNode.Content != cacheFilePosition)
            {
#if DEBUG
            Console.WriteLine("Reading from file");
#endif
                FileStream file = new FileStream(datasource, FileMode.Open);
                file.Position = indexNode.Content;

                // Also set the new cache of course
                cacheFilePosition = indexNode.Content;
                cache = PrimitiveBlock.ParseFrom(
                readBlockData(file, readBlobHeader(file).Datasize));
            }

            for(int i = 0; i < cache.PrimitivegroupCount; i++)
            {
                PrimitiveGroup pg = cache.GetPrimitivegroup(i);

                // Insert nodes in node tree
                if(pg.HasDense)
                {
                    
                    double longitude = 0;
                    double latitude = 0;
                    long tmpid = 0;

                    for(int j = 0; j < pg.Dense.LonCount; j++)
                    {
                        longitude += .000000001 *
                            (cache.LonOffset +
                             (cache.Granularity * pg.Dense.GetLon(i)));

                        latitude += .000000001 *
                            (cache.LatOffset +
                             (cache.Granularity * pg.Dense.GetLat(i)));

                        tmpid += pg.Dense.GetId(i);
                    }

                    if(id == tmpid) {
                        n = new Node(longitude, latitude, id);

                        // Add to the cache
                        nodeCache.Insert(id, n);

                        return n;
                    }
                }
            }

            // Nonexistant node
            return null;
        }


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
        }

        static byte[] zlibdecompress(byte[] compressed)
        {

            MemoryStream input = new MemoryStream(compressed);
            MemoryStream output = new MemoryStream();

            // Please please please don't expect me to be able to explain the neccessity of this
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

