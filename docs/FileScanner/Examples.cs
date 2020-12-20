using Lexical.FileSystem;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace docs
{
    public class FileScanner_Examples
    {
        public static void Main(string[] args)
        {/*
            {
                #region Snippet_1
                // Get filesystem
                IFileSystem filesystem = FileSystem.OS;

                // Create scanner
                FileScanner filescanner = new FileScanner(filesystem)
                    .AddWildcard("c:/Windows/System32/**.dll")
                    .SetReturnDirectories(true)
                    .SetDirectoryEvaluator(dir => true);

                // Scan files
                foreach (var entry in filescanner)
                {
                    Console.WriteLine(entry.Path);
                }
                #endregion Snippet_1
            }
            */
            {
                #region Snippet_1a
                IFileSystem fs = new MemoryFileSystem();
                fs.CreateDirectory("myfile.zip/folder");
                fs.CreateFile("myfile.zip/folder/somefile.txt");

                FileScanner filescanner = new FileScanner(fs);
                #endregion Snippet_1a

                #region Snippet_1b
                filescanner.AddWildcard("*.zip");
                #endregion Snippet_1b

                #region Snippet_1c
                filescanner.AddRegex(path: "", pattern: new Regex(@".*\.zip"));
                #endregion Snippet_1c

                #region Snippet_1d
                filescanner.AddGlobPattern("**.zip/**.txt");
                #endregion Snippet_1d

                #region Snippet_1d2
                filescanner.AddGlobPattern("myfile.zip/**.txt");
                #endregion Snippet_1d2

                #region Snippet_1e
                foreach (IEntry entry in filescanner)
                {
                    Console.WriteLine(entry.Path);
                }
                #endregion Snippet_1e

                #region Snippet_1f
                // Collect errors
                filescanner.errors = new ConcurrentBag<Exception>();
                // Run scan
                IEntry[] entries = filescanner.ToArray();
                // View errors
                foreach (Exception e in filescanner.errors) Console.WriteLine(e);
                #endregion Snippet_1f

                #region Snippet_1g
                filescanner.ReturnDirectories = true;
                #endregion Snippet_1g

                #region Snippet_1h
                filescanner.SetDirectoryEvaluator(e => e.Name != "tmp");
                #endregion Snippet_1h
            }


            {
                #region Snippet_10a
                Regex globPattern = new GlobPatternRegex("**/*.dll/**.resources", "/");
                #endregion Snippet_10a

                #region Snippet_10b
                string[] files = new[]
                {
                    "somefile.zip",
                    "somefile.zip/somefile.ext",
                    "somefile.zip/somefile.ext/someresource",
                    "somefile.zip/somelib.dll",
                    "somefile.zip/somelib.dll/someresource.resources",
                    "somelib.dll",
                    "somelib.dll/someresource.resources",
                };

                foreach (string filename in files.Where(fn => globPattern.IsMatch(fn)))
                    Console.WriteLine(filename);
                #endregion Snippet_10b
            }
        }

    }
}
