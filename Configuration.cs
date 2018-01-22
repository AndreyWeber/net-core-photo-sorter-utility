using System;
using System.Configuration;

using static System.String;

namespace PhotoSorterUtility
{
    public sealed class Configuration
    {
        public UInt16 ChunkSize
        {
            get => UInt16.TryParse(ConfigurationManager.AppSettings["chunkSize"], out UInt16 chunkSize) && chunkSize > 0
                ? chunkSize
                : throw new ConfigurationErrorsException("Invalid 'chunkSize' configuration parameter");
        }

        public String KnownImageTypes
        {
            get => IsNullOrWhiteSpace(ConfigurationManager.AppSettings["knownImageTypes"])
                ? throw new ConfigurationErrorsException("Invalid 'knownImageTypes' configuration parameter")
                : ConfigurationManager.AppSettings["knownImageTypes"];
        }

        public void Deconstruct(out UInt16 chunkSize, out String knownImageTypes)
        {
            chunkSize = ChunkSize;
            knownImageTypes = KnownImageTypes;
        }
    }
}
