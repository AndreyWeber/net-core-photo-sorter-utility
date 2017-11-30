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
    // TODO:
    /**
        //// 1. DateTime string convert
        //// 2. Hanging while reading error in ExifToolWrapper
        //// 3. Async read of data inside ExifToolWrapper
        4. Logging to console
        5. Find files for all specified extensions
     */

    class Program
    {
        public static void Main(string[] args)
        {
            const String dir = @"C:\Users\Andrey\Pictures\From Nexus 5";
            var files = Directory.EnumerateFiles(dir, "*.jpg", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.EnumerateFiles(dir, "*.jpeg", SearchOption.AllDirectories));

            using (var exifTool = new ExifToolWrapper())
            {
                var chunks = files.ChunkBy(100).ToList();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var jsonString = exifTool.GetImagesMetadataAsJsonString(files.Take(200));

                stopwatch.Stop();
                Console.WriteLine($"Time elapsed (async): {stopwatch.Elapsed}");

                stopwatch.Restart();

                jsonString = exifTool.GetImagesMetadataAsJsonString(files.Take(200));

                stopwatch.Stop();
                Console.WriteLine($"Time elapsed (sync): {stopwatch.Elapsed}");

                var coll = JsonConvert.DeserializeObject<List<ImageMetadata>>(jsonString);

                var dateTimeString = coll[1].ExifTags["DateTimeOriginal"].Value;
                var dateTimeOriginal = DateTime.ParseExact(dateTimeString, "yyyy:MM:dd hh:mm:ss", CultureInfo.InvariantCulture);
                var folderName = dateTimeOriginal.ToString("MM-yyyy");
            }
        }
    }
}
