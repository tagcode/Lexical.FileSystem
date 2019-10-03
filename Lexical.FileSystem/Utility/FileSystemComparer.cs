// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           6.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.FileProvider;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Comparer that compares known filesystems for equality of referred system.
    /// 
    /// For instance, two different instances of <see cref="EmbeddedFileSystem"/> that
    /// refer to same <see cref="Assembly"/> are considered hash-equal.
    /// </summary>
    public class FileSystemComparerComposition : IEqualityComparer<IFileSystem>
    {
        /// <summary>
        /// Default instance that supports:
        /// 
        /// <list type="table">
        ///     <item><see cref="EmbeddedFileSystem"/></item>
        ///     <item><see cref="FileSystem"/></item>
        ///     <item><see cref="FileProviderSystem"/></item>
        ///     <item><see cref="FileSystemDecoration"/></item>
        ///     <item><see cref="MemoryFileSystem"/></item>
        /// </list>
        /// </summary>
        private static FileSystemComparerComposition instance = new FileSystemComparerComposition(
            typeof(EmbeddedFileSystemComparer), typeof(FileSystemComparer), typeof(FileProviderSystemComparer), typeof(FileSystemCompositionComparer)
        );

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FileSystemComparerComposition Instance => instance;

        /// <summary>
        /// Comparers for each <see cref="IFileSystem"/> type.
        /// </summary>
        ConcurrentDictionary<Type, IEqualityComparer<IFileSystem>> comparers = new ConcurrentDictionary<Type, IEqualityComparer<IFileSystem>>();

        /// <summary>
        /// Supported types.
        /// </summary>
        HashSet<Type> supportedTypes = new HashSet<Type>();

        /// <summary>
        /// Comparer factory delegate.
        /// </summary>
        Func<Type, IEqualityComparer<IFileSystem>> comparerFactory;

        /// <summary>
        /// Construct filesystem comparer.
        /// </summary>
        /// <param name="comparerTypes">supported comparer types that implement <see cref="IEqualityComparer{T}"/></param>
        public FileSystemComparerComposition(params Type[] comparerTypes)
        {
            foreach (Type comparerType in comparerTypes)
            {
                supportedTypes.Add(comparerType);
            }
            comparerFactory = CreateComparer;
        }

        /// <summary>
        /// Method that creates comparer.
        /// </summary>
        IEqualityComparer<IFileSystem> CreateComparer(Type filesystemType)
        {
            // Assert that type is supported
            if (!supportedTypes.Contains(filesystemType))
            {
                // Revert to reference comparer
                return EqualityComparer<IFileSystem>.Default;
            }

            // Construct comparer
            return (IEqualityComparer<IFileSystem>)Activator.CreateInstance(filesystemType);
        }

        /// <summary>
        /// Compare two filesystems. 
        /// </summary>
        /// <param name="x">(optional) </param>
        /// <param name="y">(optional) </param>
        /// <returns>true if filesystems are equal</returns>
        public bool Equals(IFileSystem x, IFileSystem y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            // Get comparers
            IEqualityComparer<IFileSystem> xComparer = comparers.GetOrAdd(x.GetType(), comparerFactory), yComparer = comparers.GetOrAdd(y.GetType(), comparerFactory);

            // Same comparer
            if (xComparer == yComparer) return xComparer.Equals(x, y);

            // Compare both ways
            return xComparer.Equals(x, y) && yComparer.Equals(y, x);
        }

        /// <summary>
        /// Calculate filesystem hashcode.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystem filesystem)
        {
            // Null
            if (filesystem == null) return 0;
            // Get comparer
            IEqualityComparer<IFileSystem> comparer = comparers.GetOrAdd(filesystem.GetType(), comparerFactory);
            // Calc
            return comparer.GetHashCode(filesystem);
        }
    }

    /// <summary>
    /// Compares <see cref="EmbeddedFileSystem"/> instances.
    /// </summary>
    public class EmbeddedFileSystemComparer : IEqualityComparer<IFileSystem>
    {
        /// <summary>
        /// Compare for <see cref="Assembly"/> equality.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IFileSystem x, IFileSystem y)
        {
            // Handle null
            if (x == null && y == null) return true;
            // Compare
            if (x is EmbeddedFileSystem xFileSystem && y is EmbeddedFileSystem yFileSystem)
                return xFileSystem.Assembly.Equals(yFileSystem.Assembly);
            // Did not apply
            return false;
        }

        /// <summary>
        /// Get hashcode
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystem filesystem)
        {
            // Handle null
            if (filesystem == null) return 0;
            // Use Assembly as hashcode
            if (filesystem is EmbeddedFileSystem fs) return fs.Assembly.GetHashCode() ^ 0x234234;
            // Did not apply
            return filesystem.GetHashCode();
        }
    }

    /// <summary>
    /// Compares <see cref="FileSystem"/> instances.
    /// </summary>
    public class FileSystemComparer : IEqualityComparer<IFileSystem>
    {
        /// <summary>
        /// Compare for <see cref="FileSystem.Path"/> equality.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IFileSystem x, IFileSystem y)
        {
            // Handle null
            if (x == null && y == null) return true;
            // Compare
            if (x is FileSystem xFileSystem && y is FileSystem yFileSystem)
                return xFileSystem.Path.Equals(yFileSystem.Path);
            // Did not apply
            return false;
        }

        /// <summary>
        /// Get hashcode
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystem filesystem)
        {
            // Handle null
            if (filesystem == null) return 0;
            // Use Assembly as hashcode
            if (filesystem is FileSystem fs) return fs.Path.GetHashCode() ^ 0x234234;
            // Did not apply
            return filesystem.GetHashCode();
        }
    }

    /// <summary>
    /// Compares <see cref="FileProviderSystem"/> instances.
    /// </summary>
    public class FileProviderSystemComparer : IEqualityComparer<IFileSystem>
    {
        /// <summary>
        /// Compare for <see cref="FileSystem.Path"/> equality.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IFileSystem x, IFileSystem y)
        {
            // Handle null
            if (x == null && y == null) return true;
            // Compare
            if (x is FileProviderSystem xFileSystem && y is FileProviderSystem yFileSystem)
            {
                IFileProvider xFileProvider = xFileSystem.FileProvider, yFileProvider = yFileSystem.FileProvider;

                // Handle null
                if (xFileProvider == null && yFileProvider == null) return true;
                // Handle on enull
                if (xFileProvider == null || yFileProvider == null) return false;
                // Compare providers
                return xFileProvider.Equals(yFileProvider);
            }
            // Did not apply
            return false;
        }

        /// <summary>
        /// Get hashcode
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystem filesystem)
        {
            // Handle null
            if (filesystem == null) return 0;
            // Use Assembly as hashcode
            if (filesystem is FileProviderSystem fs)
            {
                // Get fileprovider
                IFileProvider fp = fs.FileProvider;
                // Handle null
                if (fp == null) return 0;
                // Get hashcode
                return fp.GetHashCode();
            }
            // Did not apply
            return filesystem.GetHashCode();
        }
    }

    /// <summary>
    /// Compares <see cref="FileSystemDecoration"/> instances for equal filesystems in equal order.
    /// </summary>
    public class FileSystemCompositionComparer : IEqualityComparer<IFileSystem>
    {
        IEqualityComparer<IFileSystem> componentComparer;

        /// <summary>
        /// 
        /// </summary>
        public FileSystemCompositionComparer()
        {
            componentComparer = FileSystemComparerComposition.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="componentComparer"></param>
        public FileSystemCompositionComparer(IEqualityComparer<IFileSystem> componentComparer)
        {
            this.componentComparer = componentComparer ?? throw new ArgumentNullException(nameof(componentComparer));
        }

        /// <summary>
        /// Compare for equal filesystems in equal order.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IFileSystem x, IFileSystem y)
        {
            // Handle null
            if (x == null && y == null) return true;
            // Compare
            if (x is FileSystemDecoration xFileSystem && y is FileSystemDecoration yFileSystem)
            {
                // Get filesystems
                IFileSystem[] xFileSystems = xFileSystem.FileSystems, yFileSystems = yFileSystem.FileSystems;
                // Compare lengths
                if (xFileSystems.Length != yFileSystems.Length) return false;
                // Compare elements
                for (int i = 0; i < xFileSystems.Length; i++)
                    if (!componentComparer.Equals(xFileSystems[i], yFileSystems[i])) return false;
                return true;
            }
            // Did not apply
            return false;
        }

        /// <summary>
        /// Get hashcode
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystem filesystem)
        {
            // Handle null
            if (filesystem == null) return 0;
            // Use Assembly as hashcode
            if (filesystem is FileSystemDecoration fs)
            {
                int hash = unchecked((int)2166136261);
                // Apply elements
                foreach (IFileSystem element in fs.FileSystems)
                {
                    hash ^= componentComparer.GetHashCode(element);
                    hash *= 16777619;
                }
                return hash;
            }
            // Did not apply
            return filesystem.GetHashCode();
        }
    }

}
