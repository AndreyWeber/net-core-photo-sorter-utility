using System;
using System.Collections.Generic;

namespace PhotoSorterUtility
{
    public static class Extensions
    {
        #region Collections/Lists extensions

        /// <summary>
        /// Converts IEnumerable source collection to IEnumerable collection of IList chunks of predifined size
        /// </summary>
        /// <param name="source">IEnumerable source</param>
        /// <param name="chunkSize" default="50">Chunk size</param>
        /// <returns>IEnumerable collection of ILists</returns>
        public static IEnumerable<IList<T>> ChunkBy<T>(this IEnumerable<T> source, UInt16 chunkSize = 50)
        {
            var sourceList = new List<T>(source);
            for (var i = 0; i < sourceList.Count; i += chunkSize)
            {
                yield return sourceList.GetRange(i, Math.Min(chunkSize, sourceList.Count - i));
            }
        }

        #endregion Collections/Lists extensions
    }
}
