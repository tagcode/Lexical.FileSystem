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
    /// Extension methods for <see cref="IOptionOperation"/>.
    /// </summary>
    public static partial class OptionOperationExtensions
    {
        /// <summary>
        /// Flatten <paramref name="option"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IOption"/></param>
        /// <returns>Either <paramref name="option"/> or flattened version as <paramref name="optionType"/></returns>
        public static IOption OptionFlattenAs(this IOption option, Type optionType)
            => option.Operation<IOptionFlattenOperation>(optionType).Flatten(option);

        /// <summary>
        /// Take union of <paramref name="option"/> and <paramref name="anotherOption"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="anotherOption"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IOption"/></param>
        /// <returns>flattened instance of <paramref name="optionType"/></returns>
        public static IOption OptionUnionAs(this IOption option, IOption anotherOption, Type optionType)
            => option.Operation<IOptionUnionOperation>(optionType).Union(option, anotherOption);

        /// <summary>
        /// Take intersection of <paramref name="option"/> and <paramref name="anotherOption"/> as <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="anotherOption"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IOption"/></param>
        /// <returns>flattened instance of <paramref name="optionType"/></returns>
        public static IOption OptionIntersectionAs(this IOption option, IOption anotherOption, Type optionType)
            => option.Operation<IOptionIntersectionOperation>(optionType).Intersection(option, anotherOption);

        /// <summary>
        /// Enumerate all the supported <see cref="IOption"/> Types.
        /// </summary>
        /// <param name="option"></param>
        /// <returns>types</returns>
        public static IEnumerable<Type> OperationTypes(this IOption option)
        {
            foreach (Type type in option.GetType().GetInterfaces())
                if (typeof(IOption).IsAssignableFrom(type) && !typeof(IOption).Equals(type)) yield return type;
            if (option is IAdaptableOption adaptable)
                foreach (KeyValuePair<Type, IOption> line in adaptable)
                    yield return line.Key;
        }

        /// <summary>
        /// Get first operation instance for <paramref name="option"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IOption"/></param>
        /// <returns>operation instance or null</returns>
        public static IOptionOperation GetOperation(this IOption option, Type optionType)
        {
            foreach (object attrib in option.GetType().GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    //if (!optionType.Equals(opAttrib.OperationsClass)) continue;
                    return (IOptionOperation)Activator.CreateInstance(opAttrib.OperationsClass);
                }
            }
            foreach (object attrib in optionType.GetCustomAttributes(typeof(OperationsAttribute), true))
            {
                if (attrib is OperationsAttribute opAttrib)
                {
                    return (IOptionOperation)Activator.CreateInstance(opAttrib.OperationsClass);
                }
            }
            return null;
        }

        /// <summary>
        /// Get operation instance that implements operation <typeparamref name="T"/> for option interface type <paramref name="optionType"/>.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionType">option interface type, a subtype of <see cref="IOption"/></param>
        /// <returns>operation instance</returns>
        /// <exception cref="FileSystemExceptionOptionOperationNotSupported">If operation is not supported.</exception>
        public static T Operation<T>(this IOption option, Type optionType) where T : IOptionOperation
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
