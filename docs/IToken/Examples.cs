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
    public class IToken_Examples
    {
        public static void Main(string[] args)
        {
            {
                // <Snippet_1>
                // Authorization header
                AuthenticationHeaderValue authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("webuser:webpassword")));
                // Tokenize
                IToken token = new Token(authorization, typeof(AuthenticationHeaderValue).FullName);
                // </Snippet_1>
            }
            {
                // <Snippet_2>
                // Token 1
                AuthenticationHeaderValue authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("webuser:webpassword")));
                IToken token1 = new Token(authorization, typeof(AuthenticationHeaderValue).FullName);

                // Token 2
                List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
                headers.Add(new KeyValuePair<string, IEnumerable<string>>("User-Agent", new String[] { "MyUserAgent" }));
                IToken token2 = new Token(headers, typeof(HttpHeaders).FullName);

                // Token 3
                CancellationTokenSource cancelSrc = new CancellationTokenSource();
                IToken token3 = new Token(cancelSrc.Token, typeof(CancellationToken).FullName);

                // Unify tokens
                IToken token = token1.Concat(token2, token3);
                // </Snippet_2>

                // <Snippet_3>
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> _headers;
                token.TryGetToken(null, key: "System.Net.Http.Headers.HttpHeaders", out _headers);
                // </Snippet_3>

                // <Snippet_4>
                AuthenticationHeaderValue _authorization;
                token.TryGetToken<AuthenticationHeaderValue>(path: null, out _authorization);
                // </Snippet_4>
            }
            {
                // <Snippet_5>
                // </Snippet_5>
            }
            {
                // <Snippet_6>
                // </Snippet_6>
            }
            {
                // <Snippet_7>
                // </Snippet_7>
            }
            {
                // <Snippet_8>
                // </Snippet_8>
            }

        }
    }

}
