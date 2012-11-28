using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using OSMPBF;

namespace MyMap
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if(args.Length == 1)
            {
                analyzeFile(args[0]);
                return;
            } else {
                analyzeFile("/home/sophie/Projects/Introductie/MyMap/netherlands.osm.pbf");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        // Primary documentation is in http://wiki.openstreetmap.org/wiki/PBF_Format
        // Feel free to ask me anything you don't understand of course
        static void analyzeFile(string s) {
            FileStream f = new FileStream(s, FileMode.Open);

            while(true) {
                // Getting length of BlobHeader
                int blobHeadLength = 0;
                for(int i = 0; i < 4; i++)
                        blobHeadLength = blobHeadLength*255 + f.ReadByte();

                // EOF at blobHeadLength == ((-1*255-1)*255-1)*255-1
                if(blobHeadLength == -16646656) {
                    break;
                }

                // Reading BlobHeader
                byte[] blobHeadData = new byte[blobHeadLength];

                f.Read(blobHeadData, 0, blobHeadLength);
                BlobHeader blobHead = BlobHeader.ParseFrom(blobHeadData);

                byte[] blobData = new byte[blobHead.Datasize];
                f.Read(blobData, 0, blobHead.Datasize);
                Blob blob = Blob.ParseFrom(blobData);

                byte[] blockData;

                if(!blob.HasRawSize)
                {
                    blockData = blob.Raw.ToByteArray();
                } else {
                    blockData = zlibdecompress(blob.ZlibData.ToByteArray());
                }

                if(blobHead.Type == "OSMHeader")
                {
                    HeaderBlock filehead = HeaderBlock.ParseFrom(blockData);

                    Console.WriteLine("Some info's about the file you are loading:");
                    Console.WriteLine("Required features:");
                    for(int i = 0; i < filehead.RequiredFeaturesCount; i++)
                    {
                        Console.WriteLine(filehead.GetRequiredFeatures(i));
                    }
                    
                    Console.WriteLine("Optional features:");
                    for(int i = 0; i < filehead.OptionalFeaturesCount; i++)
                    {
                        Console.WriteLine(filehead.GetOptionalFeatures(i));
                    }
                    
                    Console.Write("Bounding Box: ");
                    if(filehead.HasBbox)
                        Console.WriteLine(filehead.Bbox.ToString());

                    /*
                    Console.Write("Source:");
                    if(filehead.HasSource)
                        Console.WriteLine(filehead.Source);
                    
                    Console.Write("Writing Program:");
                    if(filehead.HasWritingprogram)
                        Console.WriteLine(filehead.Writingprogram);
                    */

                    //return;
                } else if(blobHead.Type == "OSMData")
                {
                    Console.WriteLine("-------------------------------------------------");
                    Console.WriteLine("Block data info");

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    Console.WriteLine("StringTable with {0} entries", pb.Stringtable.SCount);
                    if(pb.HasGranularity)
                        Console.WriteLine("Granularity: " + pb.Granularity);
                    if(pb.HasLatOffset)
                        Console.WriteLine("Latitude offset: " + pb.LatOffset);
                    if(pb.HasLonOffset)
                        Console.WriteLine("Longitude offset: " + pb.LonOffset);
                    if(pb.HasDateGranularity)
                        Console.WriteLine("Date granularity: " + pb.DateGranularity);

                    Console.WriteLine("Block has {0} groups of primitives", pb.PrimitivegroupCount);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);
                        Console.WriteLine("primitivegroup {0} {1} {2} {3} {4} {5}", i,
                        pg.ChangesetsCount,
                        pg.NodesCount,
                        pg.RelationsCount,
                        pg.WaysCount,
                        pg.HasDense);
                        if(pg.HasDense)
                        {
                            DenseNodes dense = pg.Dense;
                            Console.WriteLine("densenodes {0} {1} {2} {3}",
                                              dense.LatCount,
                                              dense.LonCount,
                                              dense.KeysValsCount,
                                              dense.IdCount);

                            double lat = 0, lon = 0;
                            long did = 0;
                            for(int j = 0; j < dense.LatCount; j++)
                            {
                                long dlat = dense.GetLat(j);
                                long dlon = dense.GetLon(j);
                                double dlatitude = .000000001 * (pb.LatOffset + (pb.Granularity * dlat));
                                double dlongitude = .000000001 * (pb.LonOffset + (pb.Granularity * dlon));
                                Console.WriteLine("coordinates {0} {1} {2} {3}",
                                                  lat += dlatitude,
                                                  lon += dlongitude,
                                                  did += dense.GetId(j),
                                                  dense.GetKeysVals(j));
                            }
                        } else {
                            /*for(int j = 0; j < pg.NodesCount; j++)
                            {
                                pg.GetNodes(j)
                            }*/
                        }
                    }
                } else
                    Console.WriteLine("Unknown blocktype: " + blobHead.Type);
            }
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

        static void echo(string s) {
            Console.WriteLine(s);
        }
    }
}
