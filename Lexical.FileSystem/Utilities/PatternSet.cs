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

namespace Lexical.FileSystem.Utilities
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
        /// <returns></returns>
        public PatternSet AddWildcard(string wildcard)
        {
            patterns.Add("^" + (Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".")) + "$");
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add glob pattern, for example "**.zip/**.dll"
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorChars"></param>
        /// <returns>this</returns>
        public PatternSet AddGlobPattern(string globPattern, string directorySeparatorChars = "/")
        {
            patterns.Add("^" + GlobPatternFactory.Create(directorySeparatorChars).CreateRegexText(globPattern) + "$");
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add multiple wildcard patterns, for example "*.dll" "*.exe".
        /// 
        /// "*" denotes for anything, even paths.
        /// </summary>
        /// <param name="wildcards"></param>
        /// <returns></returns>
        public PatternSet AddWildcards(IEnumerable<string> wildcards)
        {
            foreach (var pattern in wildcards) AddWildcard(pattern);
            return this;
        }

        /// <summary>
        /// Add multiple regex patterns.
        /// </summary>
        /// <param name="regex_patterns"></param>
        /// <returns></returns>
        public PatternSet AddRegexes(IEnumerable<string> regex_patterns)
        {
            patterns.AddRange(regex_patterns);
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add glob pattern, for example "**.zip/**.dll"
        /// </summary>
        /// <param name="globPatterns"></param>
        /// <param name="directorySeparatorChars"></param>
        /// <returns>this</returns>
        public PatternSet AddGlobPatterns(IEnumerable<string> globPatterns, string directorySeparatorChars = "/")
        {
            GlobPatternFactory factory = GlobPatternFactory.Create(directorySeparatorChars);
            patterns.AddRange(globPatterns.Select(globPattern => "^" + factory.CreateRegexText(globPattern) + "$"));
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add regex pattern.
        /// </summary>
        /// <param name="regex_pattern"></param>
        /// <returns></returns>
        public PatternSet AddRegex(string regex_pattern)
        {
            patterns.Add(regex_pattern);
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add regex objects.
        /// </summary>
        /// <param name="regex_patterns"></param>
        /// <returns></returns>
        public PatternSet AddRegexes(IEnumerable<Regex> regex_patterns)
        {
            this.regex_patterns.AddRange(regex_patterns);
            matcherFunc = null;
            return this;
        }

        /// <summary>
        /// Add regex object.
        /// </summary>
        /// <param name="regex_pattern"></param>
        /// <returns></returns>
        public PatternSet AddRegex(Regex regex_pattern)
        {
            this.regex_patterns.Add(regex_pattern);
            matcherFunc = null;
            return this;
        }

    }
}
