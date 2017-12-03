using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using static System.String;

namespace PhotoSorterUtility
{
    public sealed class ExifToolWrapper : IDisposable
    {
        private const String ImageFilesReadErrorMessage = "image files read";
        private const String ExifToolInputArgsFileName = "input.arg";
        private const String ExifToolInputTmpArgsFileName = "input.arg.tmp";

        private static readonly String baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly StringBuilder exifToolOutput = new StringBuilder();

        private Process exifToolProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(baseDirectory, "exiftool.exe"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"-@ {ExifToolInputTmpArgsFileName}"
            }
        };

        public UInt16 waitForFinishIntervalMsec { get; set; }

        public ExifToolWrapper(UInt16 waitForFinishIntervalMsec = UInt16.MaxValue)
        {
            this.waitForFinishIntervalMsec = waitForFinishIntervalMsec;

            exifToolProcess.OutputDataReceived += (Object sender, DataReceivedEventArgs e) =>
            {
                var dataChunk = e.Data?.Trim();
                if (!IsNullOrEmpty(dataChunk))
                {
                    exifToolOutput.Append(dataChunk);
                }
            };
        }

        private String RunTool()
        {
            try
            {
                exifToolOutput.Clear();

                if (!exifToolProcess.Start())
                {
                    throw new Exception("Failed to start Exiftool process");
                }

                // Start async read of standard output stream
                exifToolProcess.BeginOutputReadLine();

                var error = exifToolProcess.StandardError.ReadToEnd();
                if (!IsNullOrWhiteSpace(error) &&
                    !error.Trim().ToLower().EndsWith(ImageFilesReadErrorMessage))
                {
                    throw new Exception($"Exiftool process error: {error}");
                }

                if (exifToolProcess.WaitForExit(waitForFinishIntervalMsec))
                {
                    //var result = exifToolOutput.ToString();
                    // Stop async read of standard output stream
                    exifToolProcess.CancelOutputRead();

                    return exifToolOutput.ToString();
                }

                throw new Exception("Failed to correctly finish Exiftool process");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to run Exiftool process with the next reson: {ex.Message}", ex);
            }
        }

        private void WriteInputImageFilePathsToTmpArgsFile(IEnumerable<String> imagePaths)
        {
            if (imagePaths == null || imagePaths.All(ip => IsNullOrWhiteSpace(ip)))
            {
                throw new ArgumentException("Failed to run Exiftool process. Image path(s) not defined", nameof(imagePaths));
            }

            // Check input.arg file exists
            if (!File.Exists(Path.Combine(baseDirectory, ExifToolInputArgsFileName)))
            {
                throw new FileNotFoundException("Can't find Exiftool arguments file", ExifToolInputArgsFileName);
            }

            try
            {
                // Create temporary input args file (overwrite existing one)
                File.Copy(
                    Path.Combine(baseDirectory, ExifToolInputArgsFileName),
                    Path.Combine(baseDirectory, ExifToolInputTmpArgsFileName),
                    true);
                // Append image path(s) to the temporary input args file
                File.AppendAllLines(
                    Path.Combine(baseDirectory, ExifToolInputTmpArgsFileName),
                    imagePaths.Where(ip => !IsNullOrWhiteSpace(ip)).Select(ip => ip.Trim()),
                    Encoding.GetEncoding("UTF-8"));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create or write to tmp Exiftool input args file", ex);
            }
        }

        public String GetImagesMetadataAsJsonString(IEnumerable<String> imagePaths)
        {
            WriteInputImageFilePathsToTmpArgsFile(imagePaths);
            return RunTool();
        }

        public String GetSingleImageMetadataAsJsonString(String imagePath) =>
            GetImagesMetadataAsJsonString(new List<String> { imagePath });

        #region IDisposable members

        public void Dispose()
        {
            if (exifToolProcess != null)
            {
                exifToolProcess.Dispose();
                exifToolProcess = null;
            }
        }

        #endregion IDisposable members
    }
}
