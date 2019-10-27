// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           27.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexical.FileSystem.Package
{
    /// <summary>
    /// Class that creates temp files.
    /// </summary>
    public class TempFileProvider : ITempFileProvider
    {
        /// <summary>
        /// Default instance that uses the default temp folder.
        /// </summary>
        static ITempFileProvider _default = new TempFileProvider();

        /// <summary>
        /// Default instance that uses the default temp folder.
        /// 
        /// Note, that singleton cannot be disposed, as it is shared.
        /// However, you can call .Clear() at shutdown of the application.
        /// </summary>
        public static ITempFileProvider Default => _default;

        /// <summary>
        /// Signature of dispose function.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="deleted"></param>
        public delegate void HandleDisposedFunc(ITempFileHandle handle, bool deleted);

        /// <summary>
        /// Collection of handles that are not disposed.
        /// </summary>
        protected readonly HashSet<ITempFileHandle> handles = new HashSet<ITempFileHandle>();

        /// <summary>
        /// Collection of files that should be deleted.
        /// </summary>
        protected readonly HashSet<string> ToDelete = new HashSet<string>();

        /// <summary>
        /// Dispose state
        /// </summary>
        protected bool isDisposed, isDisposing;

        /// <summary>
        /// Internal lock object
        /// </summary>
        protected readonly Object m_lock = new Object();

        /// <summary>
        /// On disposed callback delegate.
        /// </summary>
        protected readonly HandleDisposedFunc handleDisposedFunc;

        /// <summary>
        /// Delegate that creates a new the temp file of size 0.
        /// </summary>
        /// <returns>full path to file name</returns>
        protected Func<String> createTempFile;

        string directory, prefix, suffix;

        /// <summary>
        /// Create temp file provider.
        /// </summary>
        /// <param name="options">(optional) options</param>
        public TempFileProvider(TempFileProviderOptions options = null)
        {
            this.handleDisposedFunc = HandleDisposed;

            if (options?.Directory == null && options?.Prefix == null && options?.Suffix == null)
            {
                // Use default function.
                directory = Path.GetTempPath();
                createTempFile = Path.GetTempFileName;
            }
            else
            {
                // Read dir
                directory = options?.Directory;

                if (directory == null)
                {
                    // Get the default dir
                    directory = Path.GetTempPath();
                }
                else
                {
                    // Expand environment variables
                    if (directory != null) directory = Environment.ExpandEnvironmentVariables(directory);

                    // Expand tmp if the line above didn't do that
                    if (directory != null && directory.Contains("%tmp%")) directory = directory.Replace("%tmp%", Path.GetTempPath());

                    // Test if there are still unwrapped environment variables.
                    if (directory != null)
                    {
                        Match environmentVariables = EnvironmentVariablePattern.Match(directory);
                        if (environmentVariables.Success) throw new ArgumentException($"{nameof(TempFileProviderOptions)}: could not expand all environment variables \"{environmentVariables.Groups[1].Value}\".");
                    }
                }

                // Create function that creates files using GUIDs
                prefix = options?.Prefix ?? "";
                suffix = options?.Suffix ?? "";
                createTempFile = () =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Guid guid = Guid.NewGuid();
                        string filename = Path.Combine(directory, prefix + guid.ToString() + suffix);
                        if (File.Exists(filename)) continue;

                        try
                        {
                            // Try create file
                            using (var fs = new FileStream(filename, FileMode.CreateNew)) { }
                            return filename;
                        }
                        catch (IOException)
                        {
                            // File existed, or create failed
                            continue;
                        }
                    }
                    throw new IOException($"Failed to create temp file in {directory}");
                };
            }
        }

        // Pattern that extracts environment variables, such as "%tmp%/blabla".
        static Regex EnvironmentVariablePattern = new Regex("(%[a-zA-Z_]*%)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Create new random temp file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">if file creation failed</exception>
        /// <exception cref="ObjectDisposedException">new files cannot be created after provider is disposed</exception>
        /// <returns>handle with a filename for the caller to use. Caller must dispose after use, that will delete the file if it exists.</returns>
        public ITempFileHandle CreateTempFile()
        {
            // Check disposing
            if (isDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Check again
            lock (m_lock) if (isDisposing) throw new ObjectDisposedException(GetType().FullName);

            // Try to create temp file
            string tempFile = createTempFile();

            // Make handle
            TempFileHandle handle = new TempFileHandle(tempFile, handleDisposedFunc);

            // Add to collection
            lock (m_lock)
            {
                // We create file while provider was disposed.
                if (isDisposing)
                {
                    // Delete the file
                    try
                    {
                        File.Delete(handle.Filename);
                        // File was deleted, now throw the exception
                        throw new ObjectDisposedException(GetType().FullName);
                    }
                    catch (Exception)
                    {
                        // For unexpected reason, the deletion failed. Virus scan perhaps.
                        ToDelete.Add(handle.Filename);
                    }
                }
                else
                {
                    // Add to collection
                    handles.Add(handle);
                }
            }

            // Return handle
            return handle;
        }

        /// <summary>
        /// Handle callback from handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="deleted"></param>
        protected void HandleDisposed(ITempFileHandle handle, bool deleted)
        {
            // Remove from collection if delete succeeded.
            lock (m_lock)
            {
                handles.Remove(handle);
                // Delete failed, so add to to-delete list.
                if (!deleted) ToDelete.Add(handle.Filename);
            }
        }

        /// <summary>
        /// Delete all handles, even non-disposed ones.
        /// </summary>
        public void Clear()
        {
            // Move open handles to ToDelete set
            lock (m_lock)
            {
                foreach (var handle in handles)
                    ToDelete.Add(handle.Filename);
                handles.Clear();
            }

            // Delete files
            DeleteMarked();
        }

        /// <summary>
        /// Delete any disposed files that were locked when the handle was disposed.
        /// </summary>
        public void DeleteMarked()
        {
            // Get snapshot
            List<string> toDelete = null;
            lock (m_lock) if (ToDelete.Count > 0) toDelete = new List<string>(ToDelete);

            if (toDelete == null) return;
            List<string> deleted = new List<string>(ToDelete.Count);
            foreach (string filename in toDelete)
            {
                // Try delete
                try { File.Delete(filename); } catch (IOException) { }
                // File has disappeared
                if (!File.Exists(filename)) { deleted.Add(filename); continue; }
            }

            // Remove all files that were now deleted
            lock (m_lock) foreach (string filename in deleted) ToDelete.Remove(filename);
        }

        /// <summary>
        /// Dispose temp files provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose temp files provider.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Singleton instance is not disposed. 
            if (this == _default) { return; }

            if (disposing)
            {
                lock (m_lock) isDisposing = true;
                try
                {
                    Clear();
                }
                finally
                {
                    isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{GetType().Name}(Directory={this.directory}, Prefix={this.prefix}, Suffix={this.suffix})";
    }



    class TempFileHandle : ITempFileHandle
    {
        /// <summary>
        /// File path to temp file.
        /// </summary>
        public string Filename { get; internal set; }

        /// <summary>
        /// is disposed
        /// </summary>
        long disposed;

        /// <summary>
        /// Callback to parent that manages handles.
        /// </summary>
        TempFileProvider.HandleDisposedFunc deleteAction;

        /// <summary>
        /// Create new temp file handle.
        /// </summary>
        /// <param name="filename">full file path to temp file</param>
        /// <param name="deleteAction">on delete callback delegate</param>
        public TempFileHandle(string filename, TempFileProvider.HandleDisposedFunc deleteAction)
        {
            this.Filename = filename ?? throw new ArgumentNullException(nameof(filename));
            this.deleteAction = deleteAction ?? throw new ArgumentNullException(nameof(deleteAction));
        }

        public void Dispose()
        {
            // Only one thread can dispose and once
            if (Interlocked.CompareExchange(ref disposed, 1L, 0L) != 0L) return;

            // Try deleting the file.
            // If file is locked it will throw exception
            try
            {
                if (File.Exists(Filename)) File.Delete(Filename);
                deleteAction(this, true);
            }
            catch (Exception) when (NotifyDisposeFailed())
            {
                // Let exception fly
            }
        }

        bool NotifyDisposeFailed()
        {
            deleteAction(this, false);
            return false;
        }

        public override string ToString()
            => Filename;
    }
}
