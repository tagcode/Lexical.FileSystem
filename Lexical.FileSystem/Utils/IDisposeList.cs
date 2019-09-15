// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem.Utils
{
    /// <summary>
    /// Object that can be attached with <see cref="IDisposable"/>.
    /// They will be disposed along with the object.
    /// </summary>
    public interface IDisposeList : IDisposable
    {
        /// <summary>
        /// Add <paramref name="disposable"/> that is to be disposed along with the called object.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposable"/>.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was added to list, false if wasn't but was disposed immediately</returns>
        bool AddDisposable(object disposable);

        /// <summary>
        /// Add <paramref name="disposables"/> that are going to be disposed along with the called object.
        /// 
        /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposables"/>.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>true if were added to list, false if were disposed immediately</returns>
        bool AddDisposables(IEnumerable<object> disposables);

        /// <summary>
        /// Remove <paramref name="disposable"/> from the list. 
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveDisposable(object disposable);

        /// <summary>
        /// Remove <paramref name="disposables"/> from the list. 
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool RemoveDisposables(IEnumerable<object> disposables);
    }

}
