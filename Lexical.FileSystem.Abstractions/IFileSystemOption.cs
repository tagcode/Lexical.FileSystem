// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// Interface for filesystem options. 
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOptionAdaptable"/></item>
    ///     <item><see cref="IFileSystemOptionOpen"/></item>
    ///     <item><see cref="IFileSystemOptionObserve"/></item>
    ///     <item><see cref="IFileSystemOptionMove"/></item>
    ///     <item><see cref="IFileSystemOptionBrowse"/></item>
    ///     <item><see cref="IFileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemOptionDelete"/></item>
    ///     <item><see cref="IFileSystemOptionMount"/></item>
    ///     <item><see cref="IFileSystemOptionMountPath"/></item>
    ///     <item><see cref="IFileSystemOptionPath"/></item>
    ///     <item><see cref="IFileSystemOptionPackageLoader"/></item>
    /// </list>
    /// 
    /// The options properties must be immutable in the implementing classes.
    /// </summary>
    public interface IFileSystemOption
    {
    }

    /// <summary>
    /// Interface for option classes that adapt to option types at runtime.
    /// Also enumerates supported <see cref="IFileSystemOption"/> option type interfaces.
    /// </summary>
    public interface IFileSystemOptionAdaptable : IFileSystemOption, IEnumerable<KeyValuePair<Type, IFileSystemOption>>
    {
        /// <summary>
        /// Get option with type interface.
        /// </summary>
        /// <param name="optionInterfaceType">Subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>Option or null</returns>
        IFileSystemOption GetOption(Type optionInterfaceType);
    }
    // </doc>
}
