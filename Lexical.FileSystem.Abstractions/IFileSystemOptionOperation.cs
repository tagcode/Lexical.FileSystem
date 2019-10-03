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

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Flatten <paramref name="option"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>Either <paramref name="option"/> or flattened version as <paramref name="optionType"/></returns>
        public static IFileSystemOption FlattenAs(this IFileSystemOption option, Type optionType)
            => option.Operation<IFileSystemOptionOperationFlatten>(optionType).Flatten(option);

        /// <summary>
        /// Take union of <paramref name="option"/> and <paramref name="anotherOption"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="anotherOption"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>flattened instance of <paramref name="optionType"/></returns>
        public static IFileSystemOption UnionAs(this IFileSystemOption option, IFileSystemOption anotherOption, Type optionType)
            => option.Operation<IFileSystemOptionOperationUnion>(optionType).Union(option, anotherOption);

        /// <summary>
        /// Take intersection of <paramref name="option"/> and <paramref name="anotherOption"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="anotherOption"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>flattened instance of <paramref name="optionType"/></returns>
        public static IFileSystemOption IntersectionAs(this IFileSystemOption option, IFileSystemOption anotherOption, Type optionType)
            => option.Operation<IFileSystemOptionOperationIntersection>(optionType).Intersection(option, anotherOption);

        /// <summary>
        /// Enumerate all the supported <see cref="IFileSystemOption"/> Types.
        /// </summary>
        /// <param name="option"></param>
        /// <returns>types</returns>
        public static IEnumerable<Type> OperationTypes(this IFileSystemOption option)
        {
            foreach (Type type in option.GetType().GetInterfaces())
                if (typeof(IFileSystemOption).IsAssignableFrom(type) && !typeof(IFileSystemOption).Equals(type)) yield return type;
            if (option is IFileSystemOptionAdaptable adaptable)
                foreach (KeyValuePair<Type, IFileSystemOption> line in adaptable)
                    yield return line.Key;
        }

        /// <summary>
        /// Get first operation instance for <paramref name="option"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>operation instance or null</returns>
        public static IFileSystemOptionOperation GetOperation(this IFileSystemOption option, Type optionType)
        {
            foreach (object attrib in option.GetType().GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    //if (!optionType.Equals(opAttrib.OperationsClass)) continue;
                    return (IFileSystemOptionOperation)Activator.CreateInstance(opAttrib.OperationsClass);
                }
            }
            foreach (object attrib in optionType.GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    return (IFileSystemOptionOperation)Activator.CreateInstance(opAttrib.OperationsClass);
                }
            }
            return null;
        }

        /// <summary>
        /// Get operation instance that implements operation <typeparamref name="T"/> for option interface type <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>operation instance</returns>
        /// <exception cref="FileSystemExceptionOptionOperationNotSupported">If operation is not supported.</exception>
        public static T Operation<T>(this IFileSystemOption option, Type optionType) where T : IFileSystemOptionOperation
        {
            foreach (object attrib in option.GetType().GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    //if (!optionType.Equals(opAttrib.OperationsClass)) continue;
                    object op = Activator.CreateInstance(opAttrib.OperationsClass);
                    if (op is T casted) return casted;
                }
            }
            foreach (object attrib in optionType.GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    object op = Activator.CreateInstance(opAttrib.OperationsClass);
                    if (op is T casted) return casted;
                }
            }
            throw new FileSystemExceptionOptionOperationNotSupported(null, null, option, optionType, typeof(T));
        }

    }
}
