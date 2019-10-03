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
    /// Tool that makes path conversions of decorated filesystems.
    /// 
    /// This tool is used by <see cref="IFileSystem"/> implementations that support <see cref="IFileSystemOptionMountPath"/> option.
    /// </summary>
    public class PathDecoration
    {
        /// <summary>
        /// Expected parent path
        /// </summary>
        public readonly StringSegment ParentPathSegment;

        /// <summary>
        /// Expected parent path
        /// </summary>
        public readonly String ParentPath;

        /// <summary>
        /// Added prefix on child filesystem.
        /// </summary>
        public readonly StringSegment ChildPathSegment;

        /// <summary>
        /// Added prefix on child filesystem.
        /// </summary>
        public readonly String ChildPath;

        /// <summary>
        /// If <see cref="ParentPathSegment"/> and <see cref="ChildPathSegment"/> are equal, then 
        /// they can be passed as is with no modification.
        /// </summary>
        bool equals;

        /// <summary>
        /// Create conversion tool.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        public PathDecoration(string parentPath, string childPath)
        {
            this.ParentPathSegment = new StringSegment(parentPath);
            this.ChildPathSegment = new StringSegment(childPath);
            this.ParentPath = parentPath ?? "";
            this.ChildPath = childPath ?? "";
            equals = StringSegment.Comparer.Instance.Equals(parentPath, childPath);
        }

        /// <summary>
        /// Convert input <paramref name="parentPath"/> of parent filesystem to 
        /// suitable path of child filesystem.
        /// 
        /// This method is used in most methods in parent filesystem implementation for converting input path to suitable for child filesystem, such as Browse, GetEntry, Open, CreateDirectory, Move.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        /// <returns>true <paramref name="parentPath"/> started with expected <see cref="ParentPathSegment"/></returns>
        public bool ParentToChild(StringSegment parentPath, out StringSegment childPath)
        {
            // Pass on string as is
            if (equals) { childPath = parentPath; return true; }

            // Verify parentPath starts with expected string
            if (this.ParentPath.Length > 0)
            {
                // Not enough characters
                if (parentPath.Length < ParentPathSegment.Length) { childPath = StringSegment.Empty; return false; }
                // Compare each char
                for (int i = 0; i < this.ParentPath.Length; i++)
                    if (parentPath.String[parentPath.Start + i] != this.ParentPath[i])
                    { childPath = StringSegment.Empty; return false; }

                // Get index in parentPath start start form
                int ix = this.ParentPath.Length;

                // Move past separator '/' in parentPath
                if (ix < parentPath.Length - 1 && parentPath[ix] == '/') ix++;

                // Append childpath suffix
                if (this.ChildPath.Length > 0)
                {
                    string resultString = parentPath.Length - ix == 0 ? this.ChildPath : this.ChildPath + "/" + parentPath.String.Substring(parentPath.Start + ix, parentPath.Length - ix);
                    childPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of parentPath
                    childPath = parentPath.String.Substring(parentPath.Start + ix, parentPath.Length - ix);
                    return true;
                }
            }
            else
            // Expected parentPath is empty
            {
                // Append childpath suffix
                if (this.ChildPath.Length > 0)
                {
                    string resultString = parentPath.Length == 0 ? this.ChildPath : this.ChildPath + "/" + parentPath.String.Substring(parentPath.Start, parentPath.Length);
                    childPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of parentPath
                    childPath = parentPath.String.Substring(parentPath.Start, parentPath.Length);
                    return true;
                }
            }
        }

        /// <summary>
        /// Convert input <paramref name="parentPath"/> of parent filesystem to 
        /// suitable path of child filesystem.
        /// 
        /// This method is used in most methods in parent filesystem implementation for converting input path to suitable for child filesystem, such as Browse, GetEntry, Open, CreateDirectory, Move.
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="childPath"></param>
        /// <returns>true <paramref name="parentPath"/> started with expected <see cref="ParentPathSegment"/></returns>
        public bool ParentToChild(String parentPath, out String childPath)
        {
            // Pass on string as is
            if (equals) { childPath = parentPath; return true; }

            // Verify parentPath starts with expected string
            if (this.ParentPath.Length > 0)
            {
                // Not enough characters
                if (parentPath.Length < ParentPathSegment.Length) { childPath = ""; return false; }
                // Compare each char
                for (int i = 0; i < this.ParentPath.Length; i++)
                    if (parentPath[i] != this.ParentPath[i])
                    { childPath = StringSegment.Empty; return false; }

                // Get index in parentPath start start form
                int ix = this.ParentPath.Length;

                // Move past separator '/' in parentPath
                if (ix < parentPath.Length - 1 && parentPath[ix] == '/') ix++;

                // Append childpath suffix
                if (this.ChildPath.Length > 0)
                {
                    string resultString = parentPath.Length - ix == 0 ? this.ChildPath : this.ChildPath + "/" + parentPath.Substring(ix, parentPath.Length - ix);
                    childPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of parentPath
                    childPath = parentPath.Substring(ix, parentPath.Length - ix);
                    return true;
                }
            }
            else
            // Expected parentPath is empty
            {
                // Append childpath suffix
                if (this.ChildPath.Length > 0)
                {
                    string resultString = parentPath.Length == 0 ? this.ChildPath : this.ChildPath + "/" + parentPath.Substring(0, parentPath.Length);
                    childPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of parentPath
                    childPath = parentPath.Substring(0, parentPath.Length);
                    return true;
                }
            }
        }

        /// <summary>
        /// Converts input from an event in child filesystem to compatible path in parent filesystem.
        /// 
        /// This method is called observer decorations that convert paths from child filesystem such as events entries from GetEntry and Browse.
        /// </summary>
        /// <param name="childPath"></param>
        /// <param name="parentPath"></param>
        /// <returns>true if <paramref name="childPath"/> started with expected <see cref="ChildPathSegment"/></returns>
        public bool ChildToParent(StringSegment childPath, out StringSegment parentPath)
        {
            // Pass on string as is
            if (equals) { parentPath = childPath; return true; }

            // Verify childPath starts with expected string
            if (this.ChildPath.Length > 0)
            {
                // Not enough characters
                if (childPath.Length < ChildPathSegment.Length) { parentPath = StringSegment.Empty; return false; }
                // Compare each char
                for (int i = 0; i < this.ChildPath.Length; i++)
                    if (childPath.String[childPath.Start + i] != this.ChildPath[i])
                    { parentPath = StringSegment.Empty; return false; }

                // Get index in childPath start start form
                int ix = this.ChildPath.Length;

                // Move past separator '/' in childPath
                if (ix < childPath.Length - 1 && childPath[ix] == '/') ix++;

                // Append parentpath suffix
                if (this.ParentPath.Length > 0)
                {
                    string resultString = childPath.Length - ix == 0 ? this.ParentPath : this.ParentPath + "/" + childPath.String.Substring(childPath.Start + ix, childPath.Length - ix);
                    parentPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of childPath
                    parentPath = childPath.String.Substring(childPath.Start + ix, childPath.Length - ix);
                    return true;
                }
            }
            else
            // Expected childPath is empty
            {
                // Append parentpath suffix
                if (this.ParentPath.Length > 0)
                {
                    string resultString = childPath.Length == 0 ? this.ParentPath : this.ParentPath + "/" + childPath.String.Substring(childPath.Start, childPath.Length);
                    parentPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of childPath
                    parentPath = childPath.String.Substring(childPath.Start, childPath.Length);
                    return true;
                }
            }
        }

        /// <summary>
        /// Converts input from an event in child filesystem to compatible path in parent filesystem.
        /// 
        /// This method is called observer decorations that convert paths from child filesystem such as events entries from GetEntry and Browse.
        /// </summary>
        /// <param name="childPath"></param>
        /// <param name="parentPath"></param>
        /// <returns>true if <paramref name="childPath"/> started with expected <see cref="ChildPathSegment"/></returns>
        public bool ChildToParent(String childPath, out String parentPath)
        {
            // Pass on string as is
            if (equals) { parentPath = childPath; return true; }

            // Verify childPath starts with expected string
            if (this.ChildPath.Length > 0)
            {
                // Not enough characters
                if (childPath.Length < ChildPathSegment.Length) { parentPath = ""; return false; }
                // Compare each char
                for (int i = 0; i < this.ChildPath.Length; i++)
                    if (childPath[i] != this.ChildPath[i]) { parentPath = ""; return false; }

                // Get index in childPath start start form
                int ix = this.ChildPath.Length;

                // Move past separator '/' in childPath
                if (ix < childPath.Length - 1 && childPath[ix] == '/') ix++;

                // Append parentpath suffix
                if (this.ParentPath.Length > 0)
                {
                    string resultString = childPath.Length - ix == 0 ? this.ParentPath : this.ParentPath + "/" + childPath.Substring(ix, childPath.Length - ix);
                    parentPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of childPath
                    parentPath = childPath.Substring(ix, childPath.Length - ix);
                    return true;
                }
            }
            else
            // Expected childPath is empty
            {
                // Append parentpath suffix
                if (this.ParentPath.Length > 0)
                {
                    string resultString = childPath.Length == 0 ? this.ParentPath : this.ParentPath + "/" + childPath.Substring(0, childPath.Length);
                    parentPath = resultString;
                    return true;
                }
                else
                {
                    // Return substring of childPath
                    parentPath = childPath.Substring(0, childPath.Length);
                    return true;
                }
            }
        }

    }
}
