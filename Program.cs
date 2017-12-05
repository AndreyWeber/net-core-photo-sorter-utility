using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Diagnostics; // TODO: Remove this

using static System.String;

using Newtonsoft.Json;

namespace PhotoSorterUtility
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.GetEncoding("UTF-8");
            Console.OutputEncoding = Encoding.GetEncoding("UTF-8");

            const String dir = @"C:\Users\Andrey\Pictures\From Nexus 5";
            var files = Directory.EnumerateFiles(dir, "*.jpg", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.EnumerateFiles(dir, "*.jpeg", SearchOption.AllDirectories));

            ExifToolWrapper.RemoveAllTmpArgFiles();

            Console.WriteLine("Processing start");

            const Int32 chunkSize = 200;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var imagesMetadata = files
                .ChunkBy(chunkSize)
                .AsParallel()
                .SelectMany(filePaths =>
                    {
                        var jsonString = Empty;
                        using (var exifTool = new ExifToolWrapper())
                        {
                            jsonString = exifTool.GetImagesMetadataAsJsonString(filePaths);
                        }

                        return JsonConvert.DeserializeObject<List<ImageMetadata>>(jsonString) ?? new List<ImageMetadata>();
                    });

            var folderNames = new HashSet<String>();
            foreach (var imageMetadata in imagesMetadata)
            {
                var creationDate = imageMetadata.ExtractImageCreationDate();
                if (!creationDate.HasValue)
                {
                    continue;
                }

                var folderName = creationDate?.ToString("MM-yyyy");
                if (!folderNames.Contains(folderName))
                {
                    folderNames.Add(folderName);

                    Console.WriteLine(folderName);
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Time elapsed (sync): {stopwatch.Elapsed}");
        }
    }
}
