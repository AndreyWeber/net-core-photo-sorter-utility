using System;
using System.Collections.Generic;

namespace PhotoSorterUtility
{
    public static class Extensions
    {
        #region Collections/Lists extensions

        public static IEnumerable<IList<T>> ChunkBy<T>(this List<T> sourceList, UInt16 chunkSize = 50)
        {
            for (var i = 0; i < sourceList.Count; i += chunkSize)
            {
                yield return sourceList.GetRange(i, Math.Min(chunkSize, sourceList.Count - i));
            }
        }

        #endregion Collections/Lists extensions
    }
}