// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// <see cref="IOption"/> interface type specific operations handling.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IOptionUnionOperation"/></item>
    ///     <item><see cref="IOptionIntersectionOperation"/></item>
    ///     <item><see cref="IOptionFlattenOperation"/></item>
    /// </list>
    /// 
    /// </summary>
    public interface IOptionOperation
    {
        /// <summary>
        /// The subinterface of <see cref="IOption"/> that this class manages.
        /// </summary>
        Type OptionType { get; }
    }

    /// <summary>
    /// <see cref="IOption"/> interface type specific operations handling.
    /// </summary>
    public interface IOptionUnionOperation : IOptionOperation
    {
        /// <summary>
        /// Join two instances of the option type.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        IOption Union(IOption o1, IOption o2);
    }

    /// <summary>
    /// <see cref="IOption"/> interface type specific operations handling.
    /// </summary>
    public interface IOptionIntersectionOperation : IOptionOperation
    {

        /// <summary>
        /// Join two instances of the option type.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        IOption Intersection(IOption o1, IOption o2);
    }

    /// <summary>
    /// <see cref="IOption"/> interface type specific operations handling.
    /// </summary>
    public interface IOptionFlattenOperation : IOptionOperation
    {
        /// <summary>
        /// Creates more simplified instance of <paramref name="o"/>.
        /// May return a singleton.
        /// </summary>
        /// <param name="o"></param>
        /// <returns>Effectively same content than <paramref name="o"/>, but may be reference to a lighter object</returns>
        IOption Flatten(IOption o);
    }

    /// <summary>
    /// Attribute for <see cref="IOption"/> interfaces to expose a class that manages operations for that interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class OperationsAttribute : Attribute
    {
        /// <summary>
        /// A class that implements <see cref="IOptionOperation"/>.
        /// </summary>
        public readonly Type OperationsClass;

        /// <summary>
        /// Crate attribute that gives reference to a class that manages an operations for an option interface or class.
        /// </summary>
        /// <param name="operationsClass">A class that implements <see cref="IOptionOperation"/>.</param>
        public OperationsAttribute(Type operationsClass)
        {
            OperationsClass = operationsClass ?? throw new ArgumentNullException(nameof(OperationsClass));
        }
    }
}
