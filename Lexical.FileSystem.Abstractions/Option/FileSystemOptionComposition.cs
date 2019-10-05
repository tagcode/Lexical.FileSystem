// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Implementations to <see cref="IFileSystemOption"/>.
    /// </summary>
    public class FileSystemOptionComposition : IFileSystemOptionAdaptable
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
        protected Dictionary<Type, IFileSystemOption> byType = new Dictionary<Type, IFileSystemOption>();

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="option1">option to join</param>
        /// <param name="option2">option to join</param>
        public FileSystemOptionComposition(Op op, IFileSystemOption option1, IFileSystemOption option2)
        {
            if (option1 != null)
            {
                // IFileSystemOption
                foreach (Type type in option1.GetType().GetInterfaces())
                    if (typeof(IFileSystemOption).IsAssignableFrom(type) && !typeof(IFileSystemOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option1);
                // IFileSystemOptionAdaptable
                if (option1 is IFileSystemOptionAdaptable adaptable)
                    foreach (KeyValuePair<Type, IFileSystemOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            if (option2 != null)
            {
                // IFileSystemOption
                foreach (Type type in option2.GetType().GetInterfaces())
                    if (typeof(IFileSystemOption).IsAssignableFrom(type) && !typeof(IFileSystemOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option2);
                // IFileSystemOptionAdaptable
                if (option2 is IFileSystemOptionAdaptable adaptable)
                    foreach (KeyValuePair<Type, IFileSystemOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            Flatten();
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public FileSystemOptionComposition(Op op, IEnumerable<IFileSystemOption> options)
        {
            foreach (IFileSystemOption option in options)
            {
                // Check not null
                if (option == null) continue;
                // IFileSystemOption
                foreach (Type type in option.GetType().GetInterfaces())
                    if (typeof(IFileSystemOption).IsAssignableFrom(type) && !typeof(IFileSystemOption).Equals(type) && !typeof(IFileSystem).IsAssignableFrom(type))
                        Add(op, type, option);
                // IFileSystemOptionAdaptable
                if (option is IFileSystemOptionAdaptable adaptable)
                    foreach (KeyValuePair<Type, IFileSystemOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            Flatten();
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public FileSystemOptionComposition(Op op, params IFileSystemOption[] options) : this(op, (IEnumerable<FileSystemOption>)options) { }

        /// <summary>
        /// Add option to the composition.
        /// </summary>
        /// <param name="op">Join method</param>
        /// <param name="type">Interface type to add as</param>
        /// <param name="option">Option instance to add to composition</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileSystemExceptionOptionOperationNotSupported"></exception>
        protected virtual void Add(Op op, Type type, IFileSystemOption option)
        {
            IFileSystemOption prev;
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
            List<(Type, IFileSystemOption)> changes = null;
            // Enumerate lines
            foreach (KeyValuePair<Type, IFileSystemOption> line in byType)
            {
                // Is there flattener
                if (line.Value.GetOperation(line.Key) is IFileSystemOptionOperationFlatten flattener)
                {
                    // Try flattining
                    IFileSystemOption flattened = flattener.Flatten(line.Value);
                    // Nothing was changed
                    if (flattened == line.Value) continue;
                    // Create list of modifications
                    if (changes == null) changes = new List<(Type, IFileSystemOption)>();
                    // Add modification
                    changes.Add((line.Key, flattened));
                }
            }
            // Apply modifications
            if (changes != null) foreach ((Type, IFileSystemOption) change in changes) byType[change.Item1] = change.Item2;
        }

        /// <summary>
        /// Get option by type.
        /// </summary>
        /// <param name="optionInterfaceType"></param>
        /// <returns>option or null</returns>
        public IFileSystemOption GetOption(Type optionInterfaceType)
        {
            IFileSystemOption result;
            if (byType.TryGetValue(optionInterfaceType, out result)) return result;
            return default;
        }

        IEnumerator<KeyValuePair<Type, IFileSystemOption>> IEnumerable<KeyValuePair<Type, IFileSystemOption>>.GetEnumerator() => byType.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => byType.GetEnumerator();
    }
}
