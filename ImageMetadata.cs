using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using static System.String;

namespace PhotoSorterUtility
{
    [JsonConverter(typeof (JsonImageMetadataConverter))]
    public sealed class ImageMetadata
    {
        private const String DateTimeFormat = "yyyy:MM:dd HH:mm:ss";

        public String SourceFile { get; set; } = String.Empty;
        public IDictionary<String, ExifTag> ExifTags { get; set; } = new Dictionary<String, ExifTag>();

        public DateTime? ExtractImageCreationDate()
        {
            if (ExifTags == null || !ExifTags.Any())
            {
                return null;
            }

            var dateTimeString = Empty;
            if (ExifTags.ContainsKey("DateTimeOriginal"))
            {
                dateTimeString = ExifTags["DateTimeOriginal"].Value;
            }
            else if (ExifTags.ContainsKey("CreateDate"))
            {
                dateTimeString = ExifTags["CreateDate"].Value;
            }

            // TODO: Add image file creation date from file?

            DateTime creationDateTime;
            var result = DateTime.TryParseExact(dateTimeString, DateTimeFormat, CultureInfo.InvariantCulture,
                                                DateTimeStyles.None, out creationDateTime)
                ? (DateTime?) creationDateTime
                : (DateTime?) null;

            if (result == null)
            {
                Console.WriteLine($"Can't parse '{dateTimeString}' date time string");
            }

            return result;
        }
    }
}
