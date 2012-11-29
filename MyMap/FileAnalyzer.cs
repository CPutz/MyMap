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
        double leftbound = -3;
        double rightbound = 9;
        double upperbound = 59;
        double lowerbound = 49;

        int renderwidth = 2000;
        int renderheight = 2000;

        public FileAnalyzer(string path)
        {
            Bitmap bm = analyzeFile(path);
            bm.Save("output.png");
            Application.Exit();
        }

        // Primary documentation is in http://wiki.openstreetmap.org/wiki/PBF_Format
        // Feel free to ask me anything you don't understand of course
        Bitmap analyzeFile(string s) {
            FileStream f = new FileStream(s, FileMode.Open);

            //long[,] counter = new long[renderwidth, renderheight];
            List<long>[,] counter = new List<long>[renderwidth, renderheight];
            long maxid = 0;

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
                    Console.WriteLine("-------------------------------------------------");
                    Console.WriteLine("Block data info");

                    PrimitiveBlock pb = PrimitiveBlock.ParseFrom(blockData);

                    for(int i = 0; i < pb.PrimitivegroupCount; i++)
                    {
                        PrimitiveGroup pg = pb.GetPrimitivegroup(i);
                        Console.WriteLine("primitivegroup " + i + " " +
                        pg.ChangesetsCount + " " +
                        pg.NodesCount + " " +
                        pg.RelationsCount + " " +
                        pg.WaysCount + " " +
                        pg.HasDense);
                        if(pg.HasDense)
                        {
                            DenseNodes dense = pg.Dense;
                            Console.WriteLine("densenodes " +
                                              dense.LatCount + " " +
                                              dense.LonCount + " " +
                                              dense.KeysValsCount + " " +
                                              dense.IdCount);

                            double lat = 0, lon = 0;
                            long id = 0;
                            for(int j = 0; j < dense.LatCount; j++)
                            {
                                long dlat = dense.GetLat(j);
                                long dlon = dense.GetLon(j);
                                double dlatitude = .000000001 * (pb.LatOffset + (pb.Granularity * dlat));
                                double dlongitude = .000000001 * (pb.LonOffset + (pb.Granularity * dlon));
                                Console.WriteLine("coordinates " +
                                                  (lat += dlatitude) + " " +
                                                  (lon += dlongitude) + " " +
                                                  (id += dense.GetId(j)) + " " +
                                                  dense.GetKeysVals(j));
                                List<long> idlist = counter[
                                        (int)((lon - leftbound)/(rightbound - leftbound)*renderwidth),
                                        (int)((upperbound - lat)/(upperbound - lowerbound)*renderheight)];

                                if(idlist == null) {
                                    idlist = counter[
                                        (int)((lon - leftbound)/(rightbound - leftbound)*renderwidth),
                                        (int)((upperbound - lat)/(upperbound - lowerbound)*renderheight)]
                                    = new List<long>();
                                }

                                idlist.Add(id);

                                maxid = Math.Max(idlist.Count, maxid);

                                if((int)(255*(nodes/50545454)) != (int)(255*((nodes-1)/50545454)))
                                    Console.WriteLine("Greyscale is now at " + (int)(255*(nodes/50545454)));
                                nodes++;
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
            Console.WriteLine(nodes + " nodes found");

            double minavgid = 0, maxavgid = 0;
            double minidstddev = 0, maxidstddev = 0;
            double totavgid = 0, totidstddev = 0;
            double[,] avgid = new double[renderwidth, renderheight];
            double[,] idstddev = new double[renderwidth, renderheight];
            long[,] amounts = new long[renderwidth, renderheight];

            for(int i = 0; i < renderwidth; i++) {
                for(int j = 0; j < renderheight; j++) {

                    if(counter[i,j] == null) {
                        amounts[i, j] = 0;
                        avgid[i, j] = 0;
                        idstddev[i, j] = 0;
                        continue;
                    }

                    long amount = counter[i, j].Count;

                    decimal total = 0;
                    foreach(long k in counter[i,j]) {
                        total += k;
                    }

                    double totaldeviation = 0;
                    double average = (double)(total / amount);
                    foreach(long k in counter[i,j]) {
                        totaldeviation += (k - average)*(k - average);
                    }

                    double standarddeviation = Math.Sqrt(totaldeviation/amount);

                    amounts[i, j] = amount;
                    avgid[i, j] = average;
                    idstddev[i, j] = standarddeviation;

                    minavgid = Math.Min(minavgid, average);
                    maxavgid = Math.Max(maxavgid, average);
                    totavgid += average;

                    minidstddev = Math.Min(minidstddev, standarddeviation);
                    maxidstddev = Math.Max(maxidstddev, standarddeviation);
                    totidstddev += standarddeviation;
                }
            }

            counter = null;
            Bitmap bm = new Bitmap(renderwidth, renderheight);

            double avgavgid = totavgid/(renderwidth*renderheight);
            double avgidstddev = totidstddev/(renderwidth*renderheight);

            for(int i = 0; i < renderwidth; i++) {
                for(int j = 0; j < renderheight; j++) {
                    int red, green, blue;

                    // amount of nodes in pixel
                    red = green = blue = (int)(255*((float)amounts[i, j])/maxid);

                    /*/ average id
                    if(avgid[i, j] >= avgavgid) {
                        green = (int)(128 + 127*(avgid[i, j]-avgavgid)/(maxavgid-avgavgid));
                    } else {
                        green = (int)(127*(avgid[i,j]-minavgid)/(avgavgid-minavgid));
                    }

                    // id std dev
                    if(idstddev[i, j] >= avgidstddev) {
                        blue = (int)(128 + 127*(idstddev[i, j]-avgidstddev)/(maxidstddev-avgidstddev));
                    } else {
                        blue = (int)(127*(idstddev[i,j]-minidstddev)/(avgidstddev-minidstddev));
                    }*/

                    bm.SetPixel(i, j, Color.FromArgb(red, green, blue));
                }
            }
            
            Console.WriteLine("");
            Console.WriteLine("avgavgid:" + avgavgid);
            Console.WriteLine("minavgid:" + minavgid);
            Console.WriteLine("maxavgid:" + maxavgid);

            Console.WriteLine("");
            Console.WriteLine("avgidstddev:" + avgidstddev);
            Console.WriteLine("minidstddev:" + minidstddev);
            Console.WriteLine("maxidstddev:" + maxidstddev);

            return bm;
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

