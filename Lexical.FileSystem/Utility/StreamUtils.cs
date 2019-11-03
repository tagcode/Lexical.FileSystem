// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           3.11.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Stream utils
    /// </summary>
    public static class StreamUtils
    {
        /// <summary>
        /// Read bytes from <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ReadFully(Stream s)
        {
            if (s == null) return null;

            // Get length
            long length;
            try
            {
                length = s.Length;
            }
            catch (NotSupportedException)
            {
                // Cannot get length
                MemoryStream ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }

            if (length > int.MaxValue) throw new IOException("File size over 2GB");

            int _len = (int)length;
            byte[] data = new byte[_len];

            // Read chunks
            int ix = 0;
            while (ix < _len)
            {
                int count = s.Read(data, ix, _len - ix);

                // "returns zero (0) if the end of the stream has been reached."
                if (count == 0) break;

                ix += count;
            }
            if (ix == _len) return data;
            throw new IOException("Failed to read stream fully");
        }
    }
}
