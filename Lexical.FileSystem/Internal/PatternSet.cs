// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// A builder where multiple patterns are added and then united into one unifying pattern.
    /// </summary>
    public class PatternSet
    {
        /// <summary>
        /// Regex patterns in string format. These will all be compiled into one pattern.
        /// </summary>
        List<string> patterns = new List<string>();

        /// <summary>
        /// Regex based patterns.
        /// </summary>
        List<Regex> regex_patterns = new List<Regex>();

        /// <summary>
        /// A cache for matcher function.
        /// </summary>
        Func<string, Match> matcherFunc;

        /// <summary>
        /// A function that matches all the added patterns.
        /// </summary>
        public Func<string, Match> MatcherFunc => matcherFunc ?? (matcherFunc = BuildMatcherFunc());

        /// <summary>
        /// Required scan depth.
        /// </summary>
        public int scanDepth = 0;

        /// <summary>
        /// Build a function that matches all the wildcards and regular expressions.
        /// </summary>
        /// <returns></returns>
        Func<string, Match> BuildMatcherFunc()
        {
            List<Regex> rexes = new List<Regex>(regex_patterns);
            if (patterns.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string pattern in patterns)
                {
                    if (sb.Length > 0) sb.Append('|');
                    sb.Append(pattern);
                }
                rexes.Add(new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            if (rexes.Count == 0) return filepath => null;
            if (rexes.Count == 1)
            {
                var rex = rexes[0];
                return filepath => rex.Match(filepath);
            }
            return filepath =>
            {
                foreach (var rex in rexes)
                {
                    Match m = rex.Match(filepath);
                    if (m.Success) return m;
                }
                return null;
            };
        }


        /// <summary>
        /// Add wildcard pattern, for example "*.dll"
        /// </summary>
        /// <param name="wildcard"></param>
        /// <param name="scanDepth">required scan depth</param>
        /// <returns></returns>
        public PatternSet AddWildcard(string wildcard, int scanDepth = int.MaxValue)
        {
            patterns.Add("^" + (Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".")) + "$");
            matcherFunc = null;
            this.scanDepth = Math.Max(this.scanDepth, scanDepth);
            return this;
        }

        /// <summary>
        /// Add glob pattern, for example "**.zip/**.dll"
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorChars"></param>
        /// <param name="scanDepth">required scan depth</param>
        /// <returns>this</returns>
        public PatternSet AddGlobPattern(string globPattern, int scanDepth = int.MaxValue, string directorySeparatorChars = "/")
        {
            patterns.Add("^" + GlobPatternFactory.Create(directorySeparatorChars).CreateRegexText(globPattern) + "$");
            matcherFunc = null;
            this.scanDepth = Math.Max(this.scanDepth, scanDepth);
            return this;
        }

        /// <summary>
        /// Add regex pattern.
        /// </summary>
        /// <param name="regex_pattern"></param>
        /// <param name="scanDepth">required scan depth</param>
        /// <returns></returns>
        public PatternSet AddRegex(string regex_pattern, int scanDepth = int.MaxValue)
        {
            patterns.Add(regex_pattern);
            matcherFunc = null;
            this.scanDepth = Math.Max(this.scanDepth, scanDepth);
            return this;
        }

        /// <summary>
        /// Add regex object.
        /// </summary>
        /// <param name="regex_pattern"></param>
        /// <param name="scanDepth">required scan depth</param>
        /// <returns></returns>
        public PatternSet AddRegex(Regex regex_pattern, int scanDepth = int.MaxValue)
        {
            this.regex_patterns.Add(regex_pattern);
            matcherFunc = null;
            this.scanDepth = Math.Max(this.scanDepth, scanDepth);
            return this;
        }

    }
}
