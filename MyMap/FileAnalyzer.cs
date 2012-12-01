using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using OSMPBF;

namespace MyMap
{
    public class FileAnalyzer : Form
    {
        // In whole coordinates
        /*double leftbound = -3;
        double rightbound = 9;
        double upperbound = 59;
        double lowerbound = 49;*/

        int renderwidth = 2000;
        int renderheight = 2000;

        public FileAnalyzer(string path)
        {
            analyzeFile(path);
            Application.Exit();
        }

        // Primary documentation is in http://wiki.openstreetmap.org/wiki/PBF_Format
        // Feel free to ask me anything you don't understand of course
        void analyzeFile(string s) {
            long previousid = 0;

            //long[,] counter = new long[renderwidth, renderheight];

            FileStream f = new FileStream(s, FileMode.Open);

            double nodes = 0;
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
                    
                    Console.WriteLine("Bounding Box: ");
                    if(filehead.HasBbox)
                        Console.WriteLine(filehead.Bbox.ToString());

                } else if(blobHead.Type == "OSMData")
                {

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);
                        if(pg.HasDense)
                        {
                            DenseNodes dense = pg.Dense;

                            double lat = 0, lon = 0;
                            long id = 0;
                            for(int j = 0; j < dense.LatCount; j++)
                            {
                                long dlat = dense.GetLat(j);
                                long dlon = dense.GetLon(j);
                                double dlatitude = .000000001 * (pb.LatOffset + (pb.Granularity * dlat));
                                double dlongitude = .000000001 * (pb.LonOffset + (pb.Granularity * dlon));
                                lat += dlatitude;
                                lon += dlongitude;
                                id += dense.GetId(j);

                                if(id > previousid)
                                    previousid = id;
                                else if(id == previousid)
                                    Console.WriteLine("Same id at " + id);
                                else
                                    Console.WriteLine("Lower id: " + previousid + " to " + id);

                                nodes++;
                            }
                        } else {
                            for(int j = 0; j < pg.NodesCount; j++)
                            {
                                nodes++;
                                long id = pg.GetNodes(j).Id;

                                if(id > previousid)
                                    previousid = id;
                                else if(id == previousid)
                                    Console.WriteLine("Same id at " + id);
                                else
                                    Console.WriteLine("Lower id: " + previousid + " to " + id);

                            }
                        }
                    }
                } else
                    Console.WriteLine("Unknown blocktype: " + blobHead.Type);

            }
            Console.WriteLine(nodes + " nodes found");

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

