// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    /// <summary>
    /// <see cref="IFileSystemOption"/> interface type specific operations handling.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOptionOperationUnion"/></item>
    ///     <item><see cref="IFileSystemOptionOperationIntersection"/></item>
    ///     <item><see cref="IFileSystemOptionOperationFlatten"/></item>
    /// </list>
    /// 
    /// </summary>
    public interface IFileSystemOptionOperation
    {
        /// <summary>
        /// The subinterface of <see cref="IFileSystemOption"/> that this class manages.
        /// </summary>
        Type OptionType { get; }
    }

    /// <summary>
    /// <see cref="IFileSystemOption"/> interface type specific operations handling.
    /// </summary>
    public interface IFileSystemOptionOperationUnion : IFileSystemOptionOperation
    {
        /// <summary>
        /// Join two instances of the option type.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2);
    }

    /// <summary>
    /// <see cref="IFileSystemOption"/> interface type specific operations handling.
    /// </summary>
    public interface IFileSystemOptionOperationIntersection : IFileSystemOptionOperation
    {

        /// <summary>
        /// Join two instances of the option type.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2);
    }

    /// <summary>
    /// <see cref="IFileSystemOption"/> interface type specific operations handling.
    /// </summary>
    public interface IFileSystemOptionOperationFlatten : IFileSystemOptionOperation
    {
        /// <summary>
        /// Creates more simplified instance of <paramref name="o"/>.
        /// May return a singleton.
        /// </summary>
        /// <param name="o"></param>
        /// <returns>Effectively same content than <paramref name="o"/>, but may be reference to a lighter object</returns>
        IFileSystemOption Flatten(IFileSystemOption o);
    }

    /// <summary>
    /// Attribute for <see cref="IFileSystemOption"/> interfaces for class that manages that option type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class OperationsAttribute : Attribute
    {
        /// <summary>
        /// A class that implements <see cref="IFileSystemOptionOperation"/>.
        /// </summary>
        public readonly Type OperationsClass;

        /// <summary>
        /// Crate attribute that gives reference to a class that manages an operations for an option interface or class.
        /// </summary>
        /// <param name="operationsClass">A class that implements <see cref="IFileSystemOptionOperation"/>.</param>
        public OperationsAttribute(Type operationsClass)
        {
            OperationsClass = operationsClass ?? throw new ArgumentNullException(nameof(OperationsClass));
        }
    }
}
