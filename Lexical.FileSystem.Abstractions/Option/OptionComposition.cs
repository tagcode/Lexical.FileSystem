// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Implementations to <see cref="IOption"/>.
    /// </summary>
    public class OptionComposition : IAdaptableOption
    {
        /// <summary>
        /// Join operation
        /// </summary>
        public enum Op
        {
            /// <summary>
            /// Uses union of option instances
            /// </summary>
            Union,
            /// <summary>
            /// Uses intersection of option instances
            /// </summary>
            Intersection,
            /// <summary>
            /// Uses first option instance
            /// </summary>
            First,
            /// <summary>
            /// Uses last option instance
            /// </summary>
            Last
        }

        /// <summary>
        /// Options sorted by type.
        /// </summary>
        protected Dictionary<Type, IOption> byType = new Dictionary<Type, IOption>();

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="option1">option to join</param>
        /// <param name="option2">option to join</param>
        public OptionComposition(Op op, IOption option1, IOption option2)
        {
            if (option1 != null)
            {
                // IOption
                foreach (Type type in option1.GetType().GetInterfaces())
                    if (typeof(IOption).IsAssignableFrom(type) && !typeof(IOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option1);
                // IAdaptableOption
                if (option1 is IAdaptableOption adaptable)
                    foreach (KeyValuePair<Type, IOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            if (option2 != null)
            {
                // IOption
                foreach (Type type in option2.GetType().GetInterfaces())
                    if (typeof(IOption).IsAssignableFrom(type) && !typeof(IOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option2);
                // IAdaptableOption
                if (option2 is IAdaptableOption adaptable)
                    foreach (KeyValuePair<Type, IOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            Flatten();
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        /// <param name="interfaceTypesToExclude">(optional)</param>
        public OptionComposition(Op op, IEnumerable<IOption> options, Type[] interfaceTypesToExclude = null)
        {
            foreach (IOption option in options)
            {
                // Check not null
                if (option == null) continue;
                // IOption
                foreach (Type type in option.GetType().GetInterfaces())
                {
                    if (interfaceTypesToExclude != null && interfaceTypesToExclude.Contains(type)) continue;
                    if (typeof(IOption).IsAssignableFrom(type) && !typeof(IOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option);
                }
                // IAdaptableOption
                if (option is IAdaptableOption adaptable)
                    foreach (KeyValuePair<Type, IOption> line in adaptable)
                    {
                        if (interfaceTypesToExclude != null && interfaceTypesToExclude.Contains(line.Key)) continue;
                        Add(op, line.Key, line.Value);
                    }
            }
            Flatten();
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public OptionComposition(Op op, params IOption[] options) : this(op, (IEnumerable<IOption>)options) { }

        /// <summary>
        /// Add option to the composition.
        /// </summary>
        /// <param name="op">Join method</param>
        /// <param name="type">Interface type to add as</param>
        /// <param name="option">Option instance to add to composition</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileSystemExceptionOptionOperationNotSupported"></exception>
        protected virtual void Add(Op op, Type type, IOption option)
        {
            IOption prev;
            // Combine previousOption and option
            if (byType.TryGetValue(type, out prev))
            {
                if (op == Op.First) return;
                if (op == Op.Last) { byType[type] = option; return; }
                if (op == Op.Union) { byType[type] = option.OptionUnionAs(prev, type); return; };
                if (op == Op.Intersection) { byType[type] = option.OptionIntersectionAs(prev, type); return; };
                throw new ArgumentException(nameof(op));
            }
            else
            {
                // Add new entry
                byType[type] = option;
            }
        }

        /// <summary>
        /// Remove references of unknown classes.
        /// Sometimes options are instances of <see cref="IFileSystem"/>.
        /// Those are replaced with lighter option instances.
        /// </summary>
        protected virtual void Flatten()
        {
            // List where to add changes to be applied. Dictionary cannot be modified while enumerating it.
            List<(Type, IOption)> changes = null;
            // Enumerate lines
            foreach (KeyValuePair<Type, IOption> line in byType)
            {
                // Is there flattener
                if (line.Value.GetOperation(line.Key) is IOptionFlattenOperation flattener)
                {
                    // Try flattining
                    IOption flattened = flattener.Flatten(line.Value);
                    // Nothing was changed
                    if (flattened == line.Value) continue;
                    // Create list of modifications
                    if (changes == null) changes = new List<(Type, IOption)>();
                    // Add modification
                    changes.Add((line.Key, flattened));
                }
            }
            // Apply modifications
            if (changes != null) foreach ((Type, IOption) change in changes) byType[change.Item1] = change.Item2;
        }

        /// <summary>
        /// Get option by type.
        /// </summary>
        /// <param name="optionInterfaceType"></param>
        /// <returns>option or null</returns>
        public IOption GetOption(Type optionInterfaceType)
        {
            IOption result;
            if (byType.TryGetValue(optionInterfaceType, out result)) return result;
            return default;
        }

        IEnumerator<KeyValuePair<Type, IOption>> IEnumerable<KeyValuePair<Type, IOption>>.GetEnumerator() => byType.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => byType.GetEnumerator();
    }
}
