// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           26.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.FileProviders;
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// <see cref="IFileProvider"/> that implements <see cref="IDisposable"/>.
    /// Used to signal both interfaces as return value of methods.
    /// </summary>
    public interface IFileProviderDisposable : IFileProvider, IDisposable
    {
    }
}
