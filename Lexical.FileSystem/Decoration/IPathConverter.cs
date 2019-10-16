// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// Interface for classes that make path conversions between two filesystems "Parent" and "Child".
    /// </summary>
    public interface IPathConverter
    {
        /// <summary>Path stem on parent filesystem</summary>
        StringSegment ParentPathSegment { get; }
        /// <summary>Path stem on parent filesystem</summary>
        String ParentPath { get; }
        /// <summary>Path stem on child filesystem</summary>
        StringSegment ChildPathSegment { get; }
        /// <summary>Path stem on child filesystem</summary>
        String ChildPath { get; }

        /// <summary>
        /// Convert input <paramref name="parentPath"/> of parent filesystem to path of child filesystem.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        /// <returns>true <paramref name="parentPath"/> started with expected <see cref="ParentPathSegment"/></returns>
        bool ParentToChild(StringSegment parentPath, out StringSegment childPath);

        /// <summary>
        /// Convert <paramref name="parentPath"/> of parent filesystem to path of child filesystem.
        /// 
        /// If <paramref name="childPath"/> is null then <paramref name="parentPath"/> is placed null as well.
        /// </summary>
        /// <param name="parentPath">(optional) path in parent filesystem's format</param>
        /// <param name="childPath">(optional) <paramref name="parentPath"/> converted to child filesystem's path notation</param>
        /// <returns>true <paramref name="parentPath"/> started with expected <see cref="ParentPathSegment"/></returns>
        bool ParentToChild(String parentPath, out String childPath);

        /// <summary>
        /// Converts path in child filesystem to path in parent filesystem.
        /// </summary>
        /// <param name="childPath"></param>
        /// <param name="parentPath"></param>
        /// <returns>true if <paramref name="childPath"/> started with expected <see cref="ChildPathSegment"/></returns>
        bool ChildToParent(StringSegment childPath, out StringSegment parentPath);

        /// <summary>
        /// Converts path from child filesystem to path in parent filesystem.
        /// 
        /// If <paramref name="childPath"/> is null then <paramref name="parentPath"/> is placed null as well.
        /// </summary>
        /// <param name="childPath">(optional) child filesystem path to be converted</param>
        /// <param name="parentPath">(optional) path in parent filesystem format</param>
        /// <returns>true if <paramref name="childPath"/> started with expected <see cref="ChildPathSegment"/></returns>
        bool ChildToParent(String childPath, out String parentPath);
    }
}
