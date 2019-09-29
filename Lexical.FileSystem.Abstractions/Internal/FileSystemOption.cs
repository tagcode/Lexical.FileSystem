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
    public class FileSystemOption : IFileSystemOption
    {
        /// <summary>No options</summary>
        static IFileSystemOption noOptions = new FileSystemOption();

        /// <summary>No options</summary>
        public static IFileSystemOption NoOptions => noOptions;
    }

}
