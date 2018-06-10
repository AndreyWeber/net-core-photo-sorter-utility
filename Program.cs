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

using Newtonsoft.Json;

using static System.String;
using static System.Console;
using static System.Environment;

namespace PhotoSorterUtility
{
    internal static class Program
    {
        private static String InputDir;
        private static String OutputDir;
        private static Boolean Verbose;

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

        private static IDictionary<String, String> GetArguments(String[] args)
        {
            if (args == null || !args.Any())
            {
                throw new ApplicationException("Utility cannot be started without arguments");
            }

            var result = new Dictionary<String, String>();
            foreach (var arg in args.Select(a => a.ToLower().Trim()))
            {
                if (!arg.First().Equals('-'))
                {
                    throw new ApplicationException($"Unknown utility argument: '{arg}'");
                }

                var argToken = arg.Any(c => c == ':')
                    ? (
                        ArgName: arg.Substring(0, arg.IndexOf(':')).Trim(),
                        ArgVal: arg.Substring(arg.IndexOf(':') + 1, arg.LastIndexOf(arg.Last()) - arg.IndexOf(':')).Trim()
                    )
                    : (
                        ArgName: arg,
                        ArgVal: null
                    );

                if (!result.TryAdd(argToken.ArgName, argToken.ArgVal))
                {
                    throw new ApplicationException($"Can't parse utility argument: '{arg}'");
                }
            }

            return result;
        }

        private static void ParseArguments(IDictionary<String, String> args)
        {
            #region Utility arguments description
            // *******************
            // * Arguments list: *
            // *******************
            // [-? | -help]         - non-obligatory argument. Should be specified as first argument,
            //                        other arguments will be ignored. Program help will be displayed,
            //                        if argument is specified
            // -input_dir | -id     - obligatory argument. Path to input directory containing
            //                        images to sort
            // [-output_dir | -od]  - non-obligatory argument. Path to directory where to put
            //                        sorted images. Images will be placed into respective sub-directories
            //                        named by next pattern 'yyyy-MM'
            // [-verbose | -v]      - non-obligatory argument. Show progress bar (file names) in console output
            #endregion

            if (args.ContainsKey("-?") || args.ContainsKey("-help"))
            {
                ShowUsage();
                return;
            }

            // Check obligatory arguments existence
            if (!args.ContainsKey("-input_dir") && !args.ContainsKey("-id"))
            {
                throw new ApplicationException("'-input_dir | -id' - obligatory utility argument is undefined");
            }

            // Parse arguments values
            foreach (var arg in args)
            {
                switch (arg.Key)
                {
                    case "-input_dir":
                    case "-id":
                        InputDir = Directory.Exists(arg.Value)
                            ? arg.Value
                            : throw new ApplicationException($"Input directory '{arg.Value}', specified" +
                                $" in the utility argument '{arg.Key}', doesn't exist");
                        break;
                    case "-output_dir":
                    case "-od":
                        OutputDir = Directory.Exists(arg.Value)
                            ? arg.Value
                            : throw new ApplicationException($"Output directory '{arg.Value}', specified" +
                                $" in the utility argument '{arg.Key}', doesn't exist");
                        break;
                    case "-verbose":
                    case "-v":
                        Verbose = true;
                        break;
                    default:
                        throw new ApplicationException($"Unknown utility argument: '{arg.Key}:{arg.Value}'");
                }
            }

            // Set arguments default values
            if (IsNullOrWhiteSpace(OutputDir))
            {
                OutputDir = Path.Combine(InputDir, "Output");
            }
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

            UInt16 chunkSize;
            String knownImageTypes;
            try
            {
                ParseArguments(GetArguments(args));

                (chunkSize, knownImageTypes) = new Configuration();
            }
            catch (Exception ex)
            {
                WriteLine($"Error! {ex.Message}{NewLine}");
                WriteLine("Program will stop");
                return;
            }

            WriteLine("(Re)сreating output directory");
            RecreateOutputDir(OutputDir);

            ExifToolWrapper.RemoveAllTmpArgFiles();

            var files = GetInputImageFiles(InputDir, knownImageTypes);

            WriteLine($"Input directory is: \"{InputDir}\"");
            WriteLine($"Output directory is: \"{OutputDir}\"");
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
                            if (Verbose)
                            {
                                progress.Report((Double) stepNum / files.Count);
                                Interlocked.Add(ref stepNum, chunkSize);
                            }

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
            var unsortedImagesDirPath = Path.Combine(OutputDir, "Unsorted");
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

                imageMetadata.CopyToDirectoryPath = Path.Combine(OutputDir, creationDate?.ToString("yyyy-MM"));
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
                foreach (var imageMetadata in imagesMetadata)
                {
                    if (Verbose)
                    {
                        progress.Report((Double) stepNum / stepsCount);
                    }
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
