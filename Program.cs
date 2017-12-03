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
            var files = Directory.EnumerateFiles(dir, "*.jpg", SearchOption.AllDirectories);
            //? files.AddRange(Directory.EnumerateFiles(dir, "*.jpeg", SearchOption.AllDirectories));

            const Int32 chunkSize = 1;
            var imagesMetadata = new List<ImageMetadata>();
            using (var exifTool = new ExifToolWrapper())
            {
                var stopwatch = new Stopwatch();
                Console.WriteLine("Processing start");
                stopwatch.Start();

                var jsonString = Empty;
                // 3678
                foreach (var item in files.Skip(0).ChunkBy(chunkSize).Select((v, i) => new { i, v }))
                {
                    jsonString = exifTool.GetImagesMetadataAsJsonString(item.v);
                    if (IsNullOrEmpty(jsonString))
                    {
                        Console.WriteLine($"Empty JSON response for chunk {item.i} detected. File: {item.v?.FirstOrDefault()}");
                        // Console.WriteLine($"Empty JSON response for chunk {item.i} detected. File: {item.v?.Count()}");
                        continue;
                    }

                    try
                    {
                        imagesMetadata.AddRange(JsonConvert.DeserializeObject<List<ImageMetadata>>(jsonString));

                        Console.WriteLine($"Chunk {item.i} processed. File: {item.v?.FirstOrDefault()}");
                        // Console.WriteLine($"Chunk {item.i} processed. File: {item.v?.Count()}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                        Console.WriteLine($"Inner exception: {ex?.InnerException?.Message ?? "<EMPTY>"}");
                        Console.WriteLine($"JSON string: {jsonString}");
                    }
                }

                stopwatch.Stop();
                Console.WriteLine($"Time elapsed (sync): {stopwatch.Elapsed}");

                //* CreateDate
                // var dateTimeString = coll[1].ExifTags["DateTimeOriginal"].Value;
                // var dateTimeOriginal = DateTime.ParseExact(dateTimeString, "yyyy:MM:dd hh:mm:ss", CultureInfo.InvariantCulture);
                // var folderName = dateTimeOriginal.ToString("MM-yyyy");
            }
        }
    }
}
