using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PhotoSorterUtility
{
    [JsonConverter(typeof (JsonImageMetadataConverter))]
    public sealed class ImageMetadata
    {
        public String SourceFile { get; set; } = String.Empty;
        public IDictionary<String, ExifTag> ExifTags { get; set; } = new Dictionary<String, ExifTag>();
    }
}