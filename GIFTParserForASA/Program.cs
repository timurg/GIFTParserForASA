using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using asaGiftParser;

namespace GIFTParserForASA
{
    class Program
    {
        
        
        
        private static Dictionary<string, MemoryStream> GetStreamsFromZip(string FileName)
        {
            var result = new Dictionary<string, MemoryStream>();
            using (var zipToOpen = new FileStream(FileName, FileMode.Open))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    foreach (var entity in archive.Entries)
                    {
                        var ms = new MemoryStream();
                        using (var os = entity.Open())
                        {
                            os.CopyTo(ms);
                            os.Close();
                            ms.Position = 0;
                        }
                        result.Add(entity.FullName, ms);
                    }
                }
            }

            return result;
        }
        
        static void Main(string[] args)
        {
            var result = GetStreamsFromZip(@"D:\YandexDisk\Work\JetBrains\RiderProjects\razdell.zip");
            var parser = new asaParser(result);
            var testUnits = parser.ExtracTestUnitEx();
            foreach (var (key, value) in testUnits)
            {
                Console.WriteLine($"{key.Title} - {value.Count()}");
                var questIndex = 0;
                foreach (var testUnit in value)
                {
                    //Console.WriteLine($"{questIndex++}) {testUnit.QuestContent}");
                }
            }
            Console.WriteLine("");
            //Console.ReadLine();
        }
    }
}