// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Tool that makes path conversions.
    /// 
    /// This tool is used by <see cref="IFileSystem"/> implementations that support <see cref="IFileSystemOptionMountPath"/> option.
    /// </summary>
    public class PathConversionTool
    {
        /// <summary>
        /// Expected parent path
        /// </summary>
        StringSegment ParentPath;

        /// <summary>
        /// Added prefix on child filesystem.
        /// </summary>
        StringSegment ChildPath;

        /// <summary>
        /// Create conversion tool.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        public PathConversionTool(string parentPath, string childPath)
        {
            this.ParentPath = new StringSegment(parentPath);
            this.ChildPath = new StringSegment(childPath);
        }

        /// <summary>
        /// Convert input <paramref name="parentPath"/> of parent filesystem to 
        /// suitable path of child filesystem.
        /// 
        /// This method is used in most methods in parent filesystem implementation for converting input path to suitable for child filesystem, such as Browse, GetEntry, Open, CreateDirectory, Move.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        /// <returns>true <paramref name="parentPath"/> started with expected <see cref="ParentPath"/></returns>
        public bool ParentToChild(StringSegment parentPath, out StringSegment childPath)
        {
            childPath = default;
            return false;
        }

        /// <summary>
        /// Converts input from an event in child filesystem to compatible path in parent filesystem.
        /// 
        /// This method is called observer decorations that convert paths from child filesystem such as events entries from GetEntry and Browse.
        /// </summary>
        /// <param name="childPath"></param>
        /// <param name="parentPath"></param>
        /// <returns>true if <paramref name="childPath"/> started with expected <see cref="ChildPath"/></returns>
        public bool ChildToParent(StringSegment childPath, out StringSegment parentPath)
        {
            parentPath = default;
            return false;
        }
    }
}
