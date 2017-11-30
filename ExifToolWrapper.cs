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
        #region Exiftool command prompt args examples
        //* .\exiftool.exe -exif:all -s2 -n -H -x ThumbnailImage 1.jpg
        //* .\exiftool.exe -exif:all -j -n -D -x ThumbnailImage 1.jpg
        #endregion Exiftool command prompt args examples

        private const String ImageFilesReadErrorMessage = "image files read";
        private const String ArgsToGetImageExifTagsAsJsonString = "-exif:all -j -n -D -x ThumbnailImage";

        private Process exifToolProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\exiftool.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        public UInt16 waitForFinishIntervalMsec { get; set; }

        public ExifToolWrapper(UInt16 waitForFinishIntervalMsec = 10000)
        {
            this.waitForFinishIntervalMsec = waitForFinishIntervalMsec;
        }

        private String RunTool(String imagesArg)
        {
            if (IsNullOrWhiteSpace(imagesArg))
            {
                return Empty;
            }

            try
            {
                exifToolProcess.StartInfo.Arguments = $"{ArgsToGetImageExifTagsAsJsonString} {imagesArg}";

                var output = new StringBuilder();
                exifToolProcess.OutputDataReceived += (Object sender, DataReceivedEventArgs e) =>
                {
                    var dataChunk = e.Data?.Trim();
                    if (!IsNullOrEmpty(dataChunk))
                    {
                        output.Append(dataChunk);
                    }
                };

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
                    // Stop async read of standard output stream
                    exifToolProcess.CancelOutputRead();
                    return output.ToString();
                }

                throw new Exception("Failed to correctly finish Exiftool process");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to run Exiftool process with the next reson: {ex.Message}", ex);
            }
        }

        public String GetSingleImageMetadataAsJsonString(String imagePath)
        {
            var path = imagePath?.Trim();
            if (IsNullOrEmpty(path))
            {
                return Empty;
            }

            return RunTool($"\"{path}\"");
        }

        public String GetImagesMetadataAsJsonString(IEnumerable<String> imagesPaths)
        {
            if (imagesPaths == null || !imagesPaths.Any())
            {
                return Empty;
            }

            var paths = new StringBuilder();
            paths.AppendJoin(" ", imagesPaths.Select(ip => $"\"{ip.Trim()}\""));

            return RunTool(paths.ToString());
        }

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