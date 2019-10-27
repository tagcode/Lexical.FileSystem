// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           27.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Package
{
    // <interfaces>
    /// <summary>
    /// Temporary file provider.
    /// </summary>
    public interface ITempFileProvider : IDisposable
    {
        /// <summary>
        /// Create a new unique 0-bytes temp file that is not locked.
        /// </summary>
        /// <exception cref="IOException">if file creation failed</exception>
        /// <exception cref="ObjectDisposedException">if provider is disposed</exception>
        /// <returns>handle with a filename. Caller must dispose after use, which will delete the file if it still exists.</returns>
        ITempFileHandle CreateTempFile();
    }

    /// <summary>
    /// A handle to a temp file name. 
    /// 
    /// Dispose the handle to delete it.
    /// 
    /// If temp file is locked, Dispose() throws an <see cref="IOException"/>.
    /// 
    /// Failed deletion will still be marked as to-be-deleted.
    /// There is another delete attempt when the parent <see cref="ITempFileProvider"/> is disposed.
    /// </summary>
    public interface ITempFileHandle : IDisposable
    {
        /// <summary>
        /// Filename to 0 bytes temp file.
        /// </summary>
        String Filename { get; }
    }
    // </interfaces>

    /// <summary>
    /// Options for configuring <see cref="ITempFileProvider"/>.
    /// </summary>
    public class TempFileProviderOptions : ICloneable
    {
        /// <summary>
        /// Directory to use temp files. Use slash '/' as directory separator for maximum compability.
        /// 
        /// If directory contains environment variables, such as "%tmp%", then they will be opened.
        /// If %tmp% environment variable is not found, then the <see cref="ITempFileProvider"/> implemntation 
        /// will use replace %tmp% with value from <see cref="Path.GetTempPath"/>.
        /// 
        /// If value is null, then the <see cref="ITempFileProvider"/> implemntation will use <see cref="Path.GetTempPath"/>.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Prefix to use to append before file names.
        /// 
        /// If null then "" is used.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Suffix to use to append after file names. For example ".tmp"
        /// 
        /// If null then "" is used.
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// Copy settings from <paramref name="src"/>.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public TempFileProviderOptions ReadFrom(TempFileProviderOptions src)
        {
            this.Directory = src.Directory;
            this.Prefix = src.Prefix;
            this.Suffix = src.Suffix;
            return this;
        }

        /// <summary>
        /// Calculate hashcode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
            => (Directory == null ? 0 : Directory.GetHashCode() * 7) + (Prefix == null ? 0 : Prefix.GetHashCode() * 3) + (Suffix == null ? 0 : Suffix.GetHashCode() * 5);

        /// <summary>
        /// Compare equal contents.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
            => obj is TempFileProviderOptions options ? Compare(Directory, options?.Directory) && Compare(Prefix, options?.Prefix) && Compare(Suffix, options?.Suffix) : false;

        /// <summary>
        /// Equal content comparison that approves nulls and considers two nulls are equal.
        /// </summary>
        /// <param name="a">(optional)</param>
        /// <param name="b">(optional)</param>
        /// <returns></returns>
        static bool Compare(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Create a copy
        /// </summary>
        /// <returns></returns>
        public object Clone()
            => new TempFileProviderOptions().ReadFrom(this);

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{GetType().Name}(Directory={Directory}, Prefix={Prefix}, Suffix={Suffix})";
    }

    /// <summary>
    /// <see cref="ITempFileHandle"/> extension methods.
    /// </summary>
    public static class TempFileProviderExtensions
    {
        /// <summary>
        /// Alternative Dispose() that suppresses <see cref="IOException"/>.
        /// </summary>
        /// <param name="handle"></param>
        public static void DisposeSuppressIOException(this ITempFileHandle handle)
        {
            try
            {
                handle.Dispose();
            }
            catch (IOException)
            {
            }
        }
    }
}
