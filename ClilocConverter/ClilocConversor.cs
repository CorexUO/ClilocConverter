using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ClilocConverter
{
    public class ClilocConversor
    {
        private static readonly string OutputFolder = "./output/";
        private static readonly string InputFolder = "./input/";

        private static readonly List<string> Clilocs = new List<string>();
        private static readonly List<string> JsonCliloc = new List<string>();

        private static ConsoleColor DefaultColor;

        static void Main(string[] args)
        {
            DefaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine($"UON Cliloc Converter V {typeof(ClilocConversor).Assembly.GetName().Version}\n");
            Console.ForegroundColor = DefaultColor;

            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            if (!Directory.Exists(InputFolder))
                Directory.CreateDirectory(InputFolder);


            GetClilocs();
            GetJson();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nInit the convertion process");
            Console.ForegroundColor = DefaultColor;

            ConvertToJson();
            ConvertToCliloc();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nPress any key to exit");
            Console.ReadLine();
        }

        public static void GetClilocs()
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(InputFolder);
            foreach (string fileName in fileEntries)
                ProcessCliloc(fileName);
        }

        public static void GetJson()
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(InputFolder);
            foreach (string fileName in fileEntries)
                ProcessJSON(fileName);
        }

        public static void ProcessCliloc(string path)
        {
            string filename = Path.GetFileName(path);
            string filenameNoExtension = Path.GetFileNameWithoutExtension(filename);
            if (filenameNoExtension.ToLower() == "cliloc")
            {
                Console.WriteLine("Add Cliloc file '{0}' to process.", filename);
                Clilocs.Add(path);
            }
        }

        public static void ProcessJSON(string path)
        {
            string filename = Path.GetFileName(path);
            string fileextension = Path.GetExtension(filename);
            if (fileextension.ToLower() == ".json")
            {
                Console.WriteLine("Add JSON file '{0}' to process.", filename);
                JsonCliloc.Add(path);
            }
        }

        public static void ConvertToJson()
        {
            foreach (var clilocName in Clilocs)
            {
                string fileName = Path.GetFileName(clilocName);
                string outputFile = $"{fileName.Replace(".", "-")}.json";
                Dictionary<string, string> clilocDictionary = new Dictionary<string, string>();

                var fi = new FileInfo(clilocName);
                FileStream fs;
                try
                {
                    fs = new FileStream(fi.FullName, FileMode.Open);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ForegroundColor = DefaultColor;
                    return;
                }

                var br = new BinaryReader(fs, Encoding.UTF8);
                br.BaseStream.Seek((long)6, SeekOrigin.Begin);

                while (br.BaseStream.Length != br.BaseStream.Position)
                {
                    uint number = br.ReadUInt32();
                    br.ReadByte();
                    uint length = br.ReadUInt16();
                    byte[] textBytes = br.ReadBytes(Convert.ToInt32(length));
                    var text = Encoding.UTF8.GetString(textBytes);

                    if (text == "0")
                    {
                        text = "";
                    }

                    if (clilocDictionary.ContainsKey(number.ToString()))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Duplicate value as number {number} and Text: {text}");
                        Console.WriteLine($"Dictionary have value: {clilocDictionary[number.ToString()]}");
                        Console.ForegroundColor = DefaultColor;
                    }
                    else
                    {
                        clilocDictionary.Add(number.ToString(), text);
                    }
                }

                fs.Close();
                br.Close();

                var jsonString = JsonSerializer.Serialize(clilocDictionary);
                File.WriteAllText($"{OutputFolder}/{outputFile}", jsonString);

                Console.WriteLine($"Convert {fileName} to {outputFile}!");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nConvert cliloc to json finished!\n");
            Console.ForegroundColor = DefaultColor;
        }

        public static void ConvertToCliloc()
        {
            foreach (var jsonFile in JsonCliloc)
            {
                string fileWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile);
                string output = fileWithoutExtension.Replace("-", ".");

                string jsonString = File.ReadAllText(jsonFile);
                Dictionary<string, string> jsonCliloc = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                var fi = new FileInfo($"{OutputFolder}/{output}");
                FileStream fs;

                try
                {
                    fs = new FileStream(fi.FullName, FileMode.Create);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ForegroundColor = DefaultColor;
                    return;
                }

                var bw = new BinaryWriter(fs);
                bw.Write(new byte[] { 2, 0, 0, 0, 1, 0 });


                foreach (var item in jsonCliloc)
                {
                    byte[] textBytes = Encoding.UTF8.GetBytes(item.Value);

                    bw.Write(Convert.ToUInt32(item.Key));
                    bw.Write((byte)0);

                    bw.Write(Convert.ToUInt16(textBytes.Length));
                    bw.Write(textBytes);
                }

                fs.Close();
                bw.Close();

                Console.WriteLine($"Convert {Path.GetFileName(jsonFile)} to {output}!");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nConvert json to cliloc finished!\n");
            Console.ForegroundColor = DefaultColor;
        }
    }
}
