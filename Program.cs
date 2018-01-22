using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using static System.String;
using static System.Console;
using static System.Environment;

using Newtonsoft.Json;

namespace PhotoSorterUtility
{
    internal static class Program
    {
        private static void ShowUsage()
        {
            WriteLine("DESCRIPTION");
            WriteLine("\tUtility sorts photos in a given directory by year and month of their creation. " +
                      "Results will be placed in a set of respectively named sub directories placed " +
                      $"in a defined or default folder. Original photos won't be deleted.{NewLine}");
            WriteLine("NAME");
            WriteLine($"\tnet-core-photo-sorter-utility{NewLine}");
            WriteLine("SYNTAX");
            WriteLine("\tnet-core-photo-sorter-utility input_dir [output_dir]");
            WriteLine($"\tnet-core-photo-sorter-utility [-? | -help]{NewLine}");
            WriteLine("ARGUMENTS");
            WriteLine($"\tinput_dir\tObligatory argument. Path to input directory containing photos to sort{NewLine}");
            WriteLine("\t[output_dir]\tNon-obligatory argument. Path to directory where to put sorted photos." +
                     $"Sorted photos will be placed into default 'input_dir\\Output' directory{NewLine}");
            WriteLine("\t[-? | -help]\tNon-obligatory argument. Should be specified as first argument, " +
                      "other arguments will be ignored. This help will be displayed, if argument is specified");
        }

        private static Boolean CheckArguments(String[] args)
        {
            // *******************
            // * Arguments list: *
            // *******************
            // [-? | -help] - non-obligatory argument. Should be specified as first argument,
            //                other arguments will be ignored. Program help will be displayed,
            //                if argument is specified
            // input_dir    - obligatory argument. Path to input directory containing
            //                images to sort
            // [output_dir] - non-obligatory argument. Path to directory where to put
            //                sorted images. Images will be placed into respective sub-directories
            //                named by next pattern 'yyyy-MM'

            // Arguments list should contain at least one argument
            if (args == null || args.Length < 1 ||
                args[0].Equals("-?") || args[0].ToLower().Equals("-help"))
            {
                ShowUsage();
                return false;
            }

            // Check input directory argument
            if (!Directory.Exists(args[0]))
            {
                WriteLine($"Input directory '{args[0]}' doesn't exist{NewLine}");
                WriteLine("Please check first command prompt argument and restart program");
                return false;
            }

            // Check output directory argument
            if (args.Length > 1 && !Directory.Exists(args[1]))
            {
                WriteLine($"Output directory '{args[1]}' doesn't exist{NewLine}");
                WriteLine("Please check second command prompt argument and restart program");
                return false;
            }

            return true;
        }

        private static void RecreateOutputDir(String outputDirPath)
        {
            try
            {
                if (IsNullOrWhiteSpace(outputDirPath))
                {
                    throw new ArgumentNullException(nameof (outputDirPath), "Argument cannot be null or empty");
                }

                if (Directory.Exists(outputDirPath))
                {
                    // Remove ouput directory and all sub dirs
                    Directory.Delete(outputDirPath, true);
                }
                Directory.CreateDirectory(outputDirPath);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Cannot re-create {outputDirPath} directory. Reason: {ex.Message}", ex);
            }
        }

        private static List<String> GetInputImageFiles(String inputDir, String imageTypes) =>
            imageTypes
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(imageType =>
                    Directory.EnumerateFiles(inputDir, $"*.{imageType}", SearchOption.AllDirectories))
                .ToList();

        public static void Main(String[] args)
        {
            Console.InputEncoding = Encoding.GetEncoding("UTF-8");
            Console.OutputEncoding = Encoding.GetEncoding("UTF-8");

            if (!CheckArguments(args))
            {
                ReadKey(true);
                return;
            }

            UInt16 chunkSize;
            String knownImageTypes;
            try
            {
                (chunkSize, knownImageTypes) = new Configuration();
            }
            catch (Exception ex)
            {
                WriteLine($"Error! {ex.Message}{NewLine}");
                WriteLine("Program will stop");
                return;
            }

            var inputDir = args[0];
            var outputDir = args.Length < 2 || IsNullOrWhiteSpace(args[1])
                ? Path.Combine(inputDir, "Output")
                : args[1];

            WriteLine("(Re)сreating output directory");
            RecreateOutputDir(outputDir);

            ExifToolWrapper.RemoveAllTmpArgFiles();

            var files = GetInputImageFiles(inputDir, knownImageTypes);

            WriteLine($"Input directory is: \"{inputDir}\"");
            WriteLine($"Output directory is: \"{outputDir}\"");
            WriteLine($"Number of files to process in total: {files.Count}");
            WriteLine($"Number of files to process in chunk: {chunkSize}{NewLine}");

            WriteLine($"Processing started...{NewLine}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            List<ImageMetadata> imagesMetadata;
            Int32 stepsCount;
            Int32 stepNum;
            using (var progress = new ProgressBar())
            {
                stepNum = 0;
                imagesMetadata = files
                    .ChunkBy(chunkSize)
                    .AsParallel()
                    .SelectMany(filePaths =>
                        {
                            progress.Report((Double) stepNum / files.Count);
                            Interlocked.Add(ref stepNum, chunkSize);

                            var jsonString = Empty;
                            using (var exifTool = new ExifToolWrapper())
                            {
                                jsonString = exifTool.GetImagesMetadataAsJsonString(filePaths);
                            }

                            return JsonConvert.DeserializeObject<List<ImageMetadata>>(jsonString) ?? new List<ImageMetadata>();
                        })
                    .ToList();
            }

            // Create sub-directories structure inside 'Output' directory
            var unsortedImagesDirPath = Path.Combine(outputDir, "Unsorted");
            var outputSubDirs = new HashSet<String>();
            foreach (var imageMetadata in imagesMetadata)
            {
                var creationDate = imageMetadata.ExtractImageCreationDate();
                if (!creationDate.HasValue)
                {
                    imageMetadata.CopyToDirectoryPath = unsortedImagesDirPath;
                    // Add folder for unsorted images
                    if (!outputSubDirs.Contains(unsortedImagesDirPath))
                    {
                        outputSubDirs.Add(unsortedImagesDirPath);
                    }
                    continue;
                }

                imageMetadata.CopyToDirectoryPath = Path.Combine(outputDir, creationDate?.ToString("yyyy-MM"));
                if (!outputSubDirs.Contains(imageMetadata.CopyToDirectoryPath))
                {
                    outputSubDirs.Add(imageMetadata.CopyToDirectoryPath);
                }
            }

            WriteLine("(Re)creating output sub-directories");
            foreach (var outputSubDir in outputSubDirs)
            {
                if (Directory.Exists(outputSubDir))
                {
                    continue;
                }

                Directory.CreateDirectory(outputSubDir);
            }

            WriteLine($"{NewLine}Copying images to output sub-directories");
            using (var progress = new ProgressBar())
            {
                stepsCount = imagesMetadata.Count;
                stepNum = 0;
                foreach (var imageMetadata in imagesMetadata.AsParallel())
                {
                    progress.Report((Double) stepNum / stepsCount);
                    stepNum++;

                    File.Copy(imageMetadata.SourceFilePath, Path.Combine(imageMetadata.CopyToDirectoryPath, imageMetadata.SourceFileName));
                }
            }

            stopwatch.Stop();
            WriteLine($"File(s) copied total: {stepNum}{NewLine}");
            WriteLine($"{NewLine}Time elapsed: {stopwatch.Elapsed}");
            WriteLine($"{NewLine}Press any key to exit...");

            ReadKey(true);
        }
    }
}
