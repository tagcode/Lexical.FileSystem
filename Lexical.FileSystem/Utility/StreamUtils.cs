// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           3.11.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

            // Assert fits to byte[]
            if (length > int.MaxValue) throw new IOException("Stream length over 2GB");

            // Create buffer
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

        /// <summary>
        /// Read bytes from <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadFullyAsync(Stream s)
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
                // Copy to memory stream
                await s.CopyToAsync(ms);
                // Return contents
                return ms.ToArray();
            }

            // Assert fits to byte[]
            if (length > int.MaxValue) throw new IOException("Stream length over 2GB");

            // Create buffer
            int _len = (int)length;
            byte[] data = new byte[_len];

            // Read chunks
            int ix = 0;
            while (ix < _len)
            {
                int count = await s.ReadAsync(data, ix, _len - ix);

                // "returns zero (0) if the end of the stream has been reached."
                if (count == 0) break;

                ix += count;
            }
            if (ix == _len) return data;
            throw new IOException("Failed to read stream fully");
        }

        /// <summary>
        /// Read text from <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static async Task<string> ReadTextFullyAsync(Stream s)
        {
            // No stream, no text
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
                // Copy to memory stream
                await s.CopyToAsync(ms);
                // Assert fits to byte[]
                if (ms.Length > int.MaxValue) throw new IOException("Stream length over 2GB");
                // Return contents
                return UTF8Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            }

            // Assert fits to byte[]
            if (length > int.MaxValue) throw new IOException("Stream length over 2GB");

            // Create buffer
            int _len = (int)length;
            byte[] data = new byte[_len];

            // Read chunks
            int ix = 0;
            while (ix < _len)
            {
                int count = await s.ReadAsync(data, ix, _len - ix);

                // "returns zero (0) if the end of the stream has been reached."
                if (count == 0) break;

                ix += count;
            }
            if (ix == _len) return UTF8Encoding.UTF8.GetString(data);
            throw new IOException("Failed to read stream fully");
        }


    }
}
