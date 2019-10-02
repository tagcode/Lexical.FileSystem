// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.10.2019
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
            /// (optional) FileSystem to use in decorated events and mountpoints.
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent">(optional) filesystem to use on decorated events and mountpoints</param>
            /// <param name="parentPath">(optional) expected path in <paramref name="parent"/>. Used with path conversions</param>
            /// <param name="child">the decorated filesystem</param>
            /// <param name="childPath">(optional) expected path in <paramref name="child"/>. Used with path conversions</param>
            /// <param name="decorationOptions">(optional) options that reduce permissions of <paramref name="child"/></param>
            public Setup(IFileSystem parent, string parentPath, IFileSystem child, string childPath, IFileSystemOption decorationOptions)
            {
                Parent = parent;
                ParentPath = parentPath;
                Child = child;
                ChildPath = childPath;
                DecorationOptions = decorationOptions;
            }
        }

        /// <inheritdoc/>
        public override bool CanObserve => throw new NotImplementedException();

        /// <summary>
        /// Create decoration of one decorated filesystem.
        /// </summary>
        /// <param name="fs">filesystem that is decorated</param>
        /// <param name="option">(optional) decorating options</param>
        public FileSystemDecoration(IFileSystem fs, IFileSystemOption option) : this(new Setup[] { new Setup(null, null, fs, null, option) }) { }

        /// <summary>
        /// Create decoration of one decorated filesystem.
        /// </summary>
        /// <param name="setups">filesystems to be decorated</param>
        public FileSystemDecoration(params (IFileSystem fs, IFileSystemOption option)[] setups) : this(setups.Select(s=>new Setup(null, null, s.fs, null, s.option)).ToArray()) { }

        /// <summary>
        /// Create decoration of one decorated filesystem.
        /// </summary>
        /// <param name="setup"></param>
        public FileSystemDecoration(Setup setup) : this(new Setup[] { setup }) { }

        /// <summary>
        /// Create decoration of multiple decorated filesystems.
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