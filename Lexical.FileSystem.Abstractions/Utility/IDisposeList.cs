// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem.Utility
{
    // <doc>
    /// <summary>
    /// Object that can be attached with <see cref="IDisposable"/>.
    /// They will be disposed along with the object.
    /// </summary>
    public interface IDisposeList : IDisposable
    {
        /// <summary>
        /// Has Dispose() been called, has dispose started, or has dispose completed.
        /// </summary>
        bool IsDisposeCalled { get; }

        /// <summary>
        /// Has dispose started or completed.
        /// </summary>
        bool IsDisposing { get; }

        /// <summary>
        /// Has dispose completed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Add <paramref name="disposableObject"/> that is to be disposed along with the called object.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposableObject"/>.
        /// </summary>
        /// <param name="disposableObject"></param>
        /// <returns>true if was added to list, false if wasn't but was disposed immediately</returns>
        bool AddDisposable(object disposableObject);

        /// <summary>
        /// Add <paramref name="disposableObjects"/> that are going to be disposed along with the called object.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposableObjects"/>.
        /// </summary>
        /// <param name="disposableObjects"></param>
        /// <returns>true if were added to list, false if were disposed immediately</returns>
        bool AddDisposables(IEnumerable<object> disposableObjects);

        /// <summary>
        /// Remove <paramref name="disposableObject"/> from the list. 
        /// </summary>
        /// <param name="disposableObject"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveDisposable(object disposableObject);

        /// <summary>
        /// Remove <paramref name="disposableObjects"/> from the list. 
        /// </summary>
        /// <param name="disposableObjects"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveDisposables(IEnumerable<object> disposableObjects);

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the <paramref name="disposeAction"/> is executed immediately.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <param name="state"></param>
        /// <returns>true if was added to list, false if was disposed right away</returns>
        bool AddDisposeAction(Action<object> disposeAction, object state);
    }
    // </doc>

}
