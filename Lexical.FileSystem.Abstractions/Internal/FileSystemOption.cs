// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Implementation to <see cref="IFileSystemOption"/>.
    /// </summary>
    public class FileSystemOptionNone : IFileSystemOption
    {
        /// <summary>No options</summary>
        static IFileSystemOption noOptions = new FileSystemOptionNone();

        /// <summary>No options</summary>
        public static IFileSystemOption NoOptions => noOptions;
    }

}
