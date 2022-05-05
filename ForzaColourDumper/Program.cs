using Ionic.Crc;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForzaColourDumper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                int i = 1;

                var fileList = Directory.GetFiles(args[0]).Where(x => x.Contains(".zip"));
                int numFiles = fileList.Count();

                using (StreamWriter csv = File.CreateText(Directory.GetCurrentDirectory() + "//ManufacturerColourData.csv"))
                {
                    csv.WriteLine("Name,Path,R,G,B,CarFile");

                    Console.WriteLine("Extracting manufacturer colour data...");
                    foreach (var file in fileList)
                    {
                        Console.Write($"[{i}/{numFiles}] - {Path.GetFileName(file)}");

                        using (ZipFile zip = ZipFile.Read(file))
                        {
                            bool hasColFile = zip.EntryFileNames.Count(x => x.Contains("ManufacturerColors.bin")) == 1;

                            if (!hasColFile)
                            {
                                i++;
                                Console.WriteLine(" - Skipped (No ManufacturerColors.bin present)");
                                continue;
                            }

                            ZipEntry entry = zip["ManufacturerColors.bin"];

                            MemoryStream ms = new MemoryStream();

                            entry.Extract(ms);

                            using (BinaryReader reader = new BinaryReader(ms))
                            {
                                // Number of colours is always here
                                reader.BaseStream.Position = 0x2C;

                                int numColours = reader.ReadByte();

                                for (int j = 0; j < numColours; j++)
                                {
                                    int type = reader.ReadInt32();

                                    switch (type)
                                    {
                                        // Some kind of bit flags that are unknown, but this list means a length of 4, otherwise a length of 5
                                        case 2:
                                        case 29:
                                        case 3840:
                                        case 61440:
                                        case 65536:
                                        case 131072:
                                        case 262144:
                                        case 524288:
                                        case 1048576:
                                        case 2097152:
                                        case 4194304:
                                        case 8388608:
                                            break;

                                        default:
                                            reader.BaseStream.Position += 0x1;
                                            break;
                                    }

                                    // Three floats following the type
                                    float r = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                                    float g = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                                    float b = BitConverter.ToSingle(reader.ReadBytes(4), 0);

                                    int len = reader.ReadByte();
                                    string path = new string(reader.ReadChars(len));

                                    csv.WriteLine($"{path.Substring(path.LastIndexOf("\\") + 1)},{path},{r},{g},{b},{Path.GetFileName(file)}");
                                }
                            }
                        }

                        Console.WriteLine(" - Done");
                        i++;
                    }
                }
            }
        }
    }
}
