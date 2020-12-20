using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace docs
{
    public class HttpFileSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                // <Snippet_1>
                IFileSystem fs = new HttpFileSystem(httpClient: default, option: default);
                // </Snippet_1>
            }
            {
                // <Snippet_2>
                IFileSystem fs = HttpFileSystem.Instance;
                // </Snippet_2>
            }
            {
                // <Snippet_3>
                using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    byte[] data = StreamUtils.ReadFully(s);
                    String str = UTF8Encoding.UTF8.GetString(data);
                    Console.WriteLine(str);
                }
                // </Snippet_3>
            }
            {
                // <Snippet_4>
                MemoryFileSystem ram = new MemoryFileSystem();
                HttpFileSystem.Instance.CopyFile("http://lexical.fi", ram, "document.txt");
                ram.PrintTo(Console.Out);
                // </Snippet_4>
            }
            {
                try
                {
                    // <Snippet_5>
                    byte[] data = new byte[1024];
                    using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Create, FileAccess.Write, FileShare.None))
                        s.Write(data);
                    // </Snippet_5>
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            {
                // <Snippet_6>
                // Create options
                List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
                headers.Add(new KeyValuePair<string, IEnumerable<string>>("User-Agent", new String[] { "MyUserAgent" }));
                IOption option1 = new Token(headers, typeof(System.Net.Http.Headers.HttpHeaders).FullName);
                IOption option2 = Option.SubPath("http://lexical.fi/");
                IOption options = Option.Union(option1, option2);

                // Create FileSystem
                IFileSystem fs = new HttpFileSystem(option: options);

                // Read resource
                using (var s = fs.Open("index.html", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    byte[] data = StreamUtils.ReadFully(s);
                    String str = UTF8Encoding.UTF8.GetString(data);
                    Console.WriteLine(str);
                }
                // </Snippet_6>
            }
            {
                // <Snippet_7>
                AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));
                IToken token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName);
                using (var s = HttpFileSystem.Instance.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None, token))
                {
                    byte[] data = new byte[4096];
                    int c = s.Read(data, 0, 1024);
                    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
                    Console.WriteLine(str);
                }
                // </Snippet_7>
            }
            {
                // <Snippet_8>
                // Authentication header
                AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

                // Token
                IToken token = new Token(
                    authentication, 
                    typeof(AuthenticationHeaderValue).FullName, 
                    "https://lexical.fi/FileSystem/private/**",
                    "https://www.lexical.fi/FileSystem/private/**"
                );

                // Create FileSystem
                IFileSystem fs = new HttpFileSystem(default, token);

                // Open
                using (var s = fs.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    byte[] data = new byte[4096];
                    int c = s.Read(data, 0, 1024);
                    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
                    Console.WriteLine(str);
                }
                // </Snippet_8>
            }
            {
                // <Snippet_9>
                // Authentication header
                AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

                // Create token
                IToken token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

                // Pass token into decorator
                IFileSystem decoration = HttpFileSystem.Instance.Decorate(token);

                // Open
                using (var s = decoration.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    byte[] data = new byte[4096];
                    int c = s.Read(data, 0, 1024);
                    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
                    Console.WriteLine(str);
                }
                // </Snippet_9>
            }
            {
                try
                {
                    // <Snippet_10>
                    HttpFileSystem.Instance.Delete("https://lexical.fi/FileSystem/private/document.txt");
                    // </Snippet_10>
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            {
                // <Snippet_11a>
                var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
                var authentication = new AuthenticationHeaderValue("Basic", authBlob);
                var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

                IEnumerable<IEntry> entries = HttpFileSystem.Instance.Browse("https://lexical.fi/FileSystem/private/", token);
                // </Snippet_11a>
            }
            {
                // <Snippet_11b>
                var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
                var authentication = new AuthenticationHeaderValue("Basic", authBlob);
                var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

                IEntry entry = HttpFileSystem.Instance.GetEntry("https://lexical.fi/FileSystem/private/document.txt", token);
                // </Snippet_11b>
            }
            {
                // <Snippet_12>
                var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
                var authentication = new AuthenticationHeaderValue("Basic", authBlob);
                var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

                HttpFileSystem.Instance.PrintTo(Console.Out, "https://lexical.fi/FileSystem/private/", option: token);
                // </Snippet_12>
            }
            {
                // <Snippet_13>
                HttpFileSystem.Instance.PrintTo(Console.Out, "https://github.com/tagcode/Lexical.FileSystem/tree/master/");
                // </Snippet_13>
            }
            {
                try
                {
                    // <Snippet_14>
                    // Cancel token
                    CancellationTokenSource cancelSrc = new CancellationTokenSource();
                    IToken token = new Token(cancelSrc.Token, typeof(CancellationToken).FullName);

                    // Set canceled
                    cancelSrc.Cancel();

                    // Read
                    HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None, token);
                    // </Snippet_14>
                } catch (Exception)
                {
                }
            }
            {
                // <Snippet_15>
                // </Snippet_15>
            }
            {
                // <Snippet_16>
                // </Snippet_16>
            }

        }
    }

}
