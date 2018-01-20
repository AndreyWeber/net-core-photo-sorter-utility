using System;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

using static System.String;

namespace PhotoSorterUtility
{
  public class JsonImageMetadataConverter : JsonConverter
  {
    public override Boolean CanWrite => false;

    public override bool CanConvert(Type objectType) => typeof (ImageMetadata).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        try
        {
            var contract = (JsonObjectContract) serializer.ContractResolver.ResolveContract(objectType);
            if (existingValue == null)
            {
                existingValue = contract.DefaultCreator();
            }

            var jObject = JObject.Load(reader);

            var jProp = jObject.Properties().FirstOrDefault(prop => prop.Name.Equals("SourceFile", StringComparison.OrdinalIgnoreCase));
            if (jProp == null)
            {
                throw new Exception("SourceFile Exif tag missed in image metadata JSON");
            }

            var result = (ImageMetadata) existingValue;

            result.SourceFilePath = jProp.Value.ToObject<String>(serializer);
            result.ExifTags = jObject.Properties().Skip(1).Select(prop => new ExifTag
            {
                Id = prop.Value["id"].ToObject<Int64>(),
                Name = prop.Name,
                Value = prop.Value["val"].ToObject<String>()
            }).ToDictionary(t => t.Name, t => t);

            return existingValue;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Wrap any exceptions encountered in a JsonSerializationException
            throw new JsonSerializationException($"Error deserializing type {objectType} at path {reader.Path}", ex);
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotImplementedException($"{nameof(JsonImageMetadataConverter)} is intended only to convert from Json");
  }
}
