// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <IOption>
    /// <summary>
    /// Interface for filesystem options. 
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IAdaptableOption"/></item>
    ///     <item><see cref="ISubPathOption"/></item>
    ///     <item><see cref="IPathInfo"/></item>
    ///     <item><see cref="IAutoMountOption"/></item>
    ///     <item><see cref="IToken"/></item>
    ///     <item><see cref="IOpenOption"/></item>
    ///     <item><see cref="IObserveOption"/></item>
    ///     <item><see cref="IMoveOption"/></item>
    ///     <item><see cref="IBrowseOption"/></item>
    ///     <item><see cref="ICreateDirectoryOption"/></item>
    ///     <item><see cref="IDeleteOption"/></item>
    ///     <item><see cref="IMountOption"/></item>
    /// </list>
    /// 
    /// The options properties must be immutable in the implementing classes.
    /// </summary>
    public interface IOption
    {
    }
    // </IOption>

    // <IAdaptableOption>
    /// <summary>
    /// Interface for option classes that adapt to option types at runtime.
    /// Also enumerates supported <see cref="IOption"/> option type interfaces.
    /// </summary>
    public interface IAdaptableOption : IOption, IEnumerable<KeyValuePair<Type, IOption>>
    {
        /// <summary>
        /// Get option with type interface.
        /// </summary>
        /// <param name="optionInterfaceType">Subtype of <see cref="IOption"/></param>
        /// <returns>Option or null</returns>
        IOption GetOption(Type optionInterfaceType);
    }
    // </IAdaptableOption>

    /// <summary>Option for mount path. Use with decorator.</summary>
    [Operations(typeof(SubPathOptionOperations))]
    // <ISubPathOption>
    public interface ISubPathOption : IOption
    {
        /// <summary>Sub-path.</summary>
        String SubPath { get; }
    }
    // </ISubPathOption>


    /// <summary>Path related options</summary>
    [Operations(typeof(FileSystemOptionOperationPath))]
    // <IPathInfo>
    public interface IPathInfo : IOption
    {
        /// <summary>Case sensitivity</summary>
        FileSystemCaseSensitivity CaseSensitivity { get; }
        /// <summary>Filesystem allows empty string "" directory names. The value of this property excludes the default empty "" root path.</summary>
        bool EmptyDirectoryName { get; }
    }
    // </IPathInfo>

    /// <summary>Knolwedge about path name case sensitivity</summary>
    [Flags]
    public enum FileSystemCaseSensitivity
    {
        /// <summary>Unknown</summary>
        Unknown = 0,
        /// <summary>Path names are case-sensitive</summary>
        CaseSensitive = 1,
        /// <summary>Path names are case-insensitive</summary>
        CaseInsensitive = 2,
        /// <summary>Some parts are sensitive, some insensitive</summary>
        Inconsistent = 3
    }
}
