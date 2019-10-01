// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Decoration of <see cref="IFileSystem"/>(s).
    /// </summary>
    public class FileSystemDecoration : FileSystemBase
    {
        /// <summary>
        /// Decoration setup.
        /// </summary>
        public class Setup
        {
            /// <summary>
            /// (optional) Parent file system to use as reference for adapted observers and mountpoint handles.
            /// If given as null, then returns the instance of <see cref="FileSystemDecoration"/>
            /// </summary>
            public IFileSystem Parent { get; internal protected set; }

            /// <summary>
            /// Expented path stem on the <see cref="Parent"/> filesystem.
            /// </summary>
            public string ParentPath { get; internal protected set; }

            /// <summary>
            /// Child file system.
            /// </summary>
            public IFileSystem Child { get; internal protected set; }

            /// <summary>
            /// A suffix of path that is appended, before path is compiled.
            /// </summary>
            public String ChildPath { get; internal protected set; }

            /// <summary>
            /// (optional) Option that was given as an argument.
            /// </summary>
            public IFileSystemOption DecorationOptions { get; internal protected set; }

            /// <summary>
            /// Effective option that is intersection of <see cref="DecorationOptions"/>, options of <see cref="Child"/>, and options of <see cref="Parent"/>.
            /// </summary>
            public IFileSystemOption EffectiveOptions { get; internal protected set; }

            /// <summary>
            /// Makes path conversions and path assertions between parent and child filesystems.
            /// </summary>
            PathConversionTool PathUtil;
        }

        /// <inheritdoc/>
        public override bool CanObserve => throw new NotImplementedException();

        /// <summary>
        /// Create decoration of one child filesystem.
        /// </summary>
        /// <param name="setup"></param>
        public FileSystemDecoration(Setup setup) : base()
        {

        }

        /// <summary>
        /// Create decoration of multiple filesystems.
        /// </summary>
        /// <param name="setups"></param>
        public FileSystemDecoration(params Setup[] setups) : base()
        {

        }

        /// <inheritdoc/>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            throw new NotImplementedException();
        }
    }

}