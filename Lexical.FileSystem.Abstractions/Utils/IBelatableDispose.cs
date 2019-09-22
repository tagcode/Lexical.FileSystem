// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Utils
{
    /// <summary>
    /// Interface for objects whose dispose can be belated. 
    /// </summary>
    public interface IBelatableDispose : IDisposable
    {
        /// <summary>
        /// Post-pone dispose. 
        /// 
        /// Creates a handle that postpones the dispose of the object until all the belate-handles have been disposed.
        /// </summary>
        /// <returns>belating handle that must be diposed</returns>
        /// <exception cref="ObjectDisposedException">thrown if object has already been disposed</exception>
        IDisposable BelateDispose();
    }
}
