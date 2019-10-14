// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// This class scans directories and searches for files that match a wildcard and regex patterns.
    /// 
    /// The class itself is IEnumerable, it will start a new scan for each IEnumerator that is requested.
    /// 
    /// It uses concurrent threads for scanning. Tasks are spawned with Task.StartNew. 
    /// If TaskFactory is congested, the scanning may not start immediately. 
    /// Caller may provide customized <see cref="TaskFactory"/> to avoid issues.
    /// 
    /// The FileScanner is programmed so that it's internal separator is '/', and results use '/' as separator.
    /// For instance, to scan a network drive with RootFileSystem, use '/' separator after volume 
    /// <code>new FileScanner(root).AddWildcard(@"\\192.168.8.1\shared/*")</code>.
    /// </summary>
    public class FileScanner : IEnumerable<IFileSystemEntry>
    {
        /// <summary>
        /// Patterns by start path. 
        /// </summary>
        Dictionary<string, PatternSet> patterns = new Dictionary<string, PatternSet>();

        /// <summary>
        /// The factory that will be used for creating scanning threads.
        /// </summary>
        public TaskFactory TaskFactory = Task.Factory;

        /// <summary>
        /// A place to put errors. Caller must place value here before starting a scan.
        /// </summary>
        public IProducerConsumerCollection<Exception> errors;

        /// <summary>
        /// Root file provider
        /// </summary>
        public readonly IFileSystem FileSystem;

        /// <summary>
        /// Prefix to add to each file entries before they are matched.
        /// 
        /// For instance if "/" is used as prefix, then glob pattern "**/*.dll" can be used
        /// to match against all .dll files _including_ root, which would be "/" with prefix.
        /// </summary>
        public string RootPrefix { get; set; } = "";

        /// <summary>
        /// Function that tests whether to enter a directory. 
        /// </summary>
        public Func<IFileSystemEntry, bool> DirectoryEvaluator = DefaultDirectoryEvaluator;

        /// <summary>
        /// Default evaluator.
        /// </summary>
        static Func<IFileSystemEntry, bool> DefaultDirectoryEvaluator = dirEntry => true;

        /// <summary>
        /// Should file scanner return directories.
        /// </summary>
        public bool ReturnDirectories { get; set; } = false;

        /// <summary>
        /// Should file scanner return files.
        /// </summary>
        public bool ReturnFiles { get; set; } = true;

        /// <summary>
        /// Create new file scanner.
        /// </summary>
        /// <param name="FileSystem"></param>
        public FileScanner(IFileSystem FileSystem)
        {
            this.FileSystem = FileSystem ?? throw new ArgumentNullException(nameof(FileSystem));
        }

        /// <summary>
        /// Add a filename pattern, a pattern with path and wildcard, for example "*.dll", "folder/*.dll"
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns>self</returns>
        public FileScanner AddWildcard(string pattern)
        {
            if (pattern == null) return this;

            GlobPatternInfo info = new GlobPatternInfo(pattern);

            // Add to pattern set
            PatternSet set;
            if (!patterns.TryGetValue(info.Prefix, out set)) patterns[info.Prefix] = set = new PatternSet();
            set.AddWildcard(info, info.SuffixDepth);

            return this;
        }

        /// <summary>
        /// Adds glob pattern. 
        ///   "**" Matched to for any string of characters including directory separator.
        ///   "*" Matched for any string of characters within the same directory.
        ///   "?" Matched for one character excluding directory separator.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public FileScanner AddGlobPattern(string pattern)
        {
            if (pattern == null) return this;

            GlobPatternInfo info = new GlobPatternInfo(pattern);

            // Add to pattern set
            PatternSet set;
            if (!patterns.TryGetValue(info.Prefix, out set)) patterns[info.Prefix] = set = new PatternSet();
            set.AddGlobPattern(info, info.SuffixDepth);

            return this;
        }

        /// <summary>
        /// Add regular expression pattern to scanner match patterns.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public FileScanner AddRegex(string path, Regex pattern)
        {
            PatternSet set;
            if (!patterns.TryGetValue(path, out set)) patterns[path] = set = new PatternSet();
            set.AddRegex(pattern, int.MaxValue);
            return this;
        }

        /// <summary>
        /// Add custom <paramref name="taskFactory"/> that constructs the browsing sub-tasks.
        /// </summary>
        /// <param name="taskFactory"></param>
        /// <returns></returns>
        public FileScanner SetTaskFactory(TaskFactory taskFactory)
        {
            this.TaskFactory = taskFactory;
            return this;
        }

        /// <summary>
        /// Prefix to add to each file entries before they are matched.
        /// 
        /// For instance if "/" is used as prefix, then glob pattern "**/*.dll" can be used
        /// to match against all .dll files _including_ root, which would be "/" with prefix.
        /// </summary>
        public FileScanner SetPathPrefix(string pathPrefix)
        {
            this.RootPrefix = pathPrefix ?? "";
            return this;
        }

        /// <summary>
        /// Set collection where errors are written to.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public FileScanner SetErrorTarget(IProducerConsumerCollection<Exception> errors)
        {
            this.errors = errors;
            return this;
        }

        /// <summary>
        /// Add a custom directory qualifier <paramref name="func"/> that approves or disapprovies whether to 
        /// continue scan into a directory.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public FileScanner SetDirectoryEvaluator(Func<IFileSystemEntry, bool> func)
        {
            this.DirectoryEvaluator = func;
            return this;
        }

        /// <summary>
        /// Should file scanner return directories.
        /// <paramref name="returnDirectories"/>
        /// </summary>
        public FileScanner SetReturnDirectories(bool returnDirectories)
        {
            this.ReturnDirectories = returnDirectories;
            return this;
        }

        /// <summary>
        /// Should file scanner return directories.
        /// <paramref name="returnFiles"/>
        /// </summary>
        public FileScanner SetReturnFiles(bool returnFiles)
        {
            this.ReturnFiles = returnFiles;
            return this;
        }

        /// <summary>
        /// Start multi-threaded scan operation.
        /// </summary>
        /// <returns>FileScannerEnumerator</returns>
        IEnumerator<IFileSystemEntry> IEnumerable<IFileSystemEntry>.GetEnumerator()
            => new PatternScanner(FileSystem, RootPrefix, patterns.Select(kv=>(kv.Key, kv.Value, kv.Value.scanDepth)), TaskFactory, errors, DirectoryEvaluator, ReturnDirectories, ReturnFiles);

        /// <summary>
        /// Start multi-threaded scan operation.
        /// </summary>
        /// <returns>FileScannerEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => new PatternScanner(FileSystem, RootPrefix, patterns.Select(kv => (kv.Key, kv.Value, kv.Value.scanDepth)), TaskFactory, errors, DirectoryEvaluator, ReturnDirectories, ReturnFiles);
    }

    /// <summary>
    /// Resettable scan enumerator.
    /// </summary>
    public class PatternScanner : IEnumerator<IFileSystemEntry>
    {
        /// <summary>
        /// Collection or errors are placed here.
        /// </summary>
        public IProducerConsumerCollection<Exception> errors;
        IFileSystem FileSystem;
        List<(string, PatternSet, int)> paths;
        ScanJob job;
        TaskFactory taskFactory;
        Func<IFileSystemEntry, bool> directoryEvaluator;
        string rootPrefix;
        bool returnDirectories;
        bool returnFiles;

        /// <summary>
        /// Create scanner.
        /// </summary>
        /// <param name="FileSystem"></param>
        /// <param name="rootPrefix"></param>
        /// <param name="patterns"></param>
        /// <param name="taskFactory"></param>
        /// <param name="errors"></param>
        /// <param name="directoryEvaluator"></param>
        /// <param name="returnDirectories"></param>
        /// <param name="returnFiles"></param>
        public PatternScanner(IFileSystem FileSystem, string rootPrefix, IEnumerable<(string, PatternSet, int)> patterns, TaskFactory taskFactory, IProducerConsumerCollection<Exception> errors, Func<IFileSystemEntry, bool> directoryEvaluator, bool returnDirectories, bool returnFiles)
        {
            this.FileSystem = FileSystem;
            this.rootPrefix = rootPrefix;
            this.directoryEvaluator = directoryEvaluator;
            this.paths = new List<(string, PatternSet, int)>(patterns);
            this.job = new ScanJob(FileSystem, rootPrefix, paths, errors, taskFactory, directoryEvaluator, returnDirectories, returnFiles);
            this.taskFactory = taskFactory;
            this.returnDirectories = returnDirectories;
            this.returnFiles = returnFiles;
            int count = Math.Max(1, paths.Count);
            for (int i = 0; i < count; i++)
                taskFactory.StartNew(this.job.Scan);
        }

        /// <summary>
        /// Get current path.
        /// </summary>
        public IFileSystemEntry Current => job?.Current;

        object IEnumerator.Current => job?.Current;

        /// <summary>
        /// Dispose scanner
        /// </summary>
        public void Dispose()
        {
            var j = job;
            j?.Dispose();
            job = null;
        }

        /// <summary>
        /// Move to next element
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            var j = job;
            if (j == null) return false; else return j.MoveNext();
        }

        /// <summary>
        /// Reset scanner
        /// </summary>
        public void Reset()
        {
            var j = job;
            if (j == null) throw new ObjectDisposedException(nameof(PatternScanner));
            ScanJob newJob = new ScanJob(FileSystem, rootPrefix, paths, errors, taskFactory, directoryEvaluator, returnDirectories, returnFiles);
            j = Interlocked.Exchange(ref job, newJob);
            int count = Math.Max(1, paths.Count);
            for (int i = 0; i < count; i++)
                taskFactory.StartNew(this.job.Scan);
            j.Dispose();
        }
    }

    /// <summary>
    /// A single scan job. 
    /// </summary>
    class ScanJob : IEnumerator<IFileSystemEntry>
    {
        IFileSystem FileSystem;
        string rootPrefix;
        IProducerConsumerCollection<Exception> errors;
        List<(string, PatternSet, int)> paths;
        BlockingCollection<IFileSystemEntry> resultQueue = new BlockingCollection<IFileSystemEntry>();
        CancellationTokenSource cancelSource = new CancellationTokenSource();
        Func<IFileSystemEntry, bool> directoryEvaluator;
        bool returnDirectories;
        bool returnFiles;
        TaskFactory taskFactory;
        object monitor = new object();
        IFileSystemEntry current;
        int activeThreads, threads;

        public ScanJob(IFileSystem FileSystem, string rootPrefix, IEnumerable<(string, PatternSet, int)> patterns, IProducerConsumerCollection<Exception> errors, TaskFactory taskFactory, Func<IFileSystemEntry, bool> directoryEvaluator, bool returnDirectories, bool returnFiles)
        {
            this.paths = new List<(string, PatternSet, int)>(patterns);
            this.rootPrefix = rootPrefix;
            this.errors = errors;
            this.taskFactory = taskFactory;
            this.FileSystem = FileSystem;
            this.directoryEvaluator = directoryEvaluator;
            this.returnDirectories = returnDirectories;
            this.returnFiles = returnFiles;
        }

        public IFileSystemEntry Current => current;
        object IEnumerator.Current => current;

        public void Dispose()
        {
            cancelSource.Cancel();
            Monitor.Enter(monitor);
            Monitor.PulseAll(monitor);
            Monitor.Exit(monitor);
            cancelSource.Dispose();
        }

        void ProcessPath((string path, PatternSet pattern, int depth) line, List<IFileSystemEntry> threadLocalList)
        {
            // Directory entries
            threadLocalList.Clear();

            // Files
            try
            {
                foreach (var entry in FileSystem.Browse(line.path))
                {
                    if (entry.IsDirectory())
                    {
                        if (line.depth>1 && directoryEvaluator(entry)) threadLocalList.Add(entry);

                        // Add directory to result, if it matches filter
                        if (returnDirectories)
                        {
                            Match match = line.pattern.MatcherFunc(entry.Path);
                            bool isMatch = match != null && match.Success;
                            if (!isMatch)
                            {
                                match = line.pattern.MatcherFunc(/*"/"+*/entry.Path);
                                isMatch = match != null && match.Success;
                            }

                            if (isMatch) resultQueue.Add(entry);
                        }
                    }

                    if (entry.IsFile())
                    {
                        if (returnFiles)
                        {
                            // Add file to result, if it matches filter
                            Match match = line.pattern.MatcherFunc(entry.Path);
                            bool isMatch = match != null && match.Success;
                            if (!isMatch)
                            {
                                match = line.pattern.MatcherFunc(/*"/" + */entry.Path);
                                isMatch = match != null && match.Success;
                            }

                            if (isMatch) resultQueue.Add(entry);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errors?.TryAdd(e);
            }

            Monitor.Enter(monitor);
            try
            {
                // Add to paths                
                paths.AddRange(threadLocalList.Select(dir => (dir.Path, line.pattern, line.depth-1)));
                threadLocalList.Clear();

                // Wakeup threads, after this monitor block
                if (activeThreads < threads) Monitor.PulseAll(monitor);

                // Start more threads
                int needThreads = Math.Min(paths.Count, Environment.ProcessorCount) - threads;
                while (needThreads-- > 0) taskFactory.StartNew(Scan);
            }
            finally
            {
                Monitor.Exit(monitor);
            }
        }

        /// <summary>
        /// Call this from every thread that participates in scanning job.
        /// </summary>
        public void Scan()
        {
            Monitor.Enter(monitor);
            threads++;
            Monitor.Exit(monitor);
            try
            {
                List<IFileSystemEntry> threadLocalList = new List<IFileSystemEntry>();
                bool isActive = false;

                while (!cancelSource.Token.IsCancellationRequested)
                {
                    (string path, PatternSet pattern, int depth) line = (null, null, 0);

                    // Get next path
                    Monitor.Enter(monitor);
                    try
                    {
                        if (paths.Count > 0)
                        {
                            int ix = paths.Count - 1;
                            line = paths[ix];
                            paths.RemoveAt(ix);
                        }

                        // Add to active threads
                        if (line.path != null && !isActive)
                        {
                            isActive = true;
                            activeThreads++;
                        }
                        else if (line.path == null && isActive)
                        {
                            isActive = false;
                            activeThreads--;
                            // Nothing in the paths and no thread works on anything.
                            if (activeThreads == 0)
                            {
                                cancelSource.Cancel();
                                Monitor.PulseAll(monitor);
                                break;
                            }
                        }
                        else if (line.path == null)
                        {
                            if (activeThreads == 0)
                            {
                                cancelSource.Cancel();
                                Monitor.PulseAll(monitor);
                            }
                            break;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(monitor);
                    }

                    // Process path
                    if (line.path != null) ProcessPath(line, threadLocalList);
                    // Wait until next path has been processed.
                    else
                    {
                        Monitor.Enter(monitor);
                        Monitor.Wait(monitor);
                        Monitor.Exit(monitor);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                errors?.TryAdd(e);
            }
            finally
            {
                Monitor.Enter(monitor);
                threads--;
                Monitor.Exit(monitor);
            }
        }

        public bool MoveNext()
        {
            // Try take
            bool _canceled = cancelSource.IsCancellationRequested;
            if (resultQueue.TryTake(out current)) return true;
            if (_canceled) return false;
            try
            {
                current = resultQueue.Take(cancelSource.Token);
                return current != null;
            }
            catch (OperationCanceledException)
            {
                if (resultQueue.TryTake(out current)) return true;
                current = null;
                return false;
            }
        }

        public void Reset()
            => throw new NotImplementedException();
    }
}
