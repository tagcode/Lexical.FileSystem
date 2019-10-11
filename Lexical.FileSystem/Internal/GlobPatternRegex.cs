// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Text.RegularExpressions;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Glob pattern uses the following notation:
    ///   "*" matches to string of characters within the same directory.
    ///   "?" matches to any character except directory separator.
    ///   "**" matches to any characters, including directory separators.
    ///   
    /// For example: "**.zip/**.dll" 
    /// </summary>
    public class GlobPatternRegex : Regex
    {
        static string MakeRegexPattern(string globPattern, string directorySeparatorCharacters)
            => "^" + GlobPatternRegexFactory.Create(directorySeparatorCharacters).CreateRegexText(globPattern ?? throw new ArgumentNullException(nameof(globPattern))) + "$";

        /// <summary>The glob pattern as a string.</summary>
        public readonly String Pattern;

        /// <summary>Directory separator characters, e.g. "/".</summary>
        public readonly String DirectorySeparatorCharacters;

        /// <summary>
        /// Create glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        public GlobPatternRegex(string globPattern) : base(MakeRegexPattern(globPattern, "/")) { this.Pattern = globPattern; this.DirectorySeparatorCharacters = "/"; }

        /// <summary>
        /// Create glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorCharacters"></param>
        public GlobPatternRegex(string globPattern, string directorySeparatorCharacters) : base(MakeRegexPattern(globPattern, directorySeparatorCharacters)) { this.Pattern = globPattern; this.DirectorySeparatorCharacters = directorySeparatorCharacters; }

        /// <summary>
        /// Create glob pattern with regexp options.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorCharacters"></param>
        /// <param name="options"></param>
        public GlobPatternRegex(string globPattern, string directorySeparatorCharacters, RegexOptions options) : base(MakeRegexPattern(globPattern, directorySeparatorCharacters), options) { this.Pattern = globPattern; this.DirectorySeparatorCharacters = directorySeparatorCharacters; }

        /// <summary>
        /// Analyses whether <paramref name="patternText"/> is a glob-pattern.
        /// </summary>
        /// <param name="patternText"></param>
        /// <returns>true if contains characters '?' or '*'</returns>
        public static bool IsGlobPattern(string patternText)
        {
            for (int i=0; i<patternText.Length; i++)
            {
                char ch = patternText[i];
                if (ch == '*' || ch == '?') return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Glob pattern factory that converts glob pattern string into regular expression.
    /// 
    /// Glob pattern uses the following notation:
    ///   "*" matches to string of characters within the same directory.
    ///   "?" matches to any character except directory separator.
    ///   "**" matches to any characters, including directory separators.
    ///   
    /// For example: "**/*.zip/**.dll" 
    /// </summary>
    public class GlobPatternRegexFactory
    {
        static GlobPatternRegexFactory singletonSlash = new GlobPatternRegexFactory("/");
        static GlobPatternRegexFactory singletonBackslash = new GlobPatternRegexFactory("\\");
        static GlobPatternRegexFactory singletonSlashAndBackslash = new GlobPatternRegexFactory("/\\");

        /// <summary>
        /// Singleton instance that assumes "/" is the directory separator.
        /// </summary>
        public static GlobPatternRegexFactory Slash => singletonSlash;

        /// <summary>
        /// Singleton instance that assumes "\" is the directory separator.
        /// </summary>
        public static GlobPatternRegexFactory Backslash => singletonBackslash;

        /// <summary>
        /// Singleton instance that assumes that both "/" and "\" are used as directory separators.
        /// </summary>
        public static GlobPatternRegexFactory SlashAndBackslash => singletonSlashAndBackslash;

        /// <summary>
        /// Get or create GlobPatternFactory
        /// </summary>
        /// <param name="directorySeparatorCharacters">separator characters, e.g. "/\\"</param>
        /// <returns>glob pattern factory</returns>
        public static GlobPatternRegexFactory Create(string directorySeparatorCharacters)
        {
            if (directorySeparatorCharacters == null) throw new ArgumentNullException(nameof(directorySeparatorCharacters));
            switch (directorySeparatorCharacters)
            {
                case "/": return GlobPatternRegexFactory.Slash;
                case "\\": return GlobPatternRegexFactory.Backslash;
                case "\\/":
                case "/\\": return GlobPatternRegexFactory.SlashAndBackslash;
                default: return new GlobPatternRegexFactory(directorySeparatorCharacters);
            }
        }

        /// <summary>
        /// Pattern that searches for "**", "*" and "?" characters
        /// </summary>
        static Regex GlobPattern = new Regex("(\\*\\*)|(\\*)|(\\?)|([^\\*\\?]*)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Parameters on how this directory is configured.
        /// </summary>
        public readonly String DirectorySeparatorChars;

        /// <summary>
        /// Replace strings for each group.
        /// </summary>
        string twoStarPattern, oneStarPattern, questionMarkPattern;

        /// <summary>
        /// Delegate used in Replace.
        /// </summary>
        MatchEvaluator matchEvaluator;

        /// <summary>
        /// Replacer for text parts that are not **, *, or ?.
        /// </summary>
        MatchEvaluator nonglobtextReplacer;

        /// <summary>
        /// Create new glob pattern.
        /// </summary>
        /// <param name="directorySeparatorChars">directory separtor characters, for example "/\\"</param>
        /// <param name="nonglobtextReplacer"></param>
        public GlobPatternRegexFactory(string directorySeparatorChars, MatchEvaluator nonglobtextReplacer = default)
        {
            this.DirectorySeparatorChars = directorySeparatorChars;
            this.nonglobtextReplacer = nonglobtextReplacer ?? (m => Regex.Escape(m.Value));
            twoStarPattern = ".*";
            oneStarPattern = "[^" + Regex.Escape(directorySeparatorChars) + "]*";
            questionMarkPattern = "[^" + Regex.Escape(directorySeparatorChars) + "]";
            matchEvaluator = MatchEvaluator;
        }

        string MatchEvaluator(Match match)
        {
            if (match.Groups[1].Success) return twoStarPattern;
            if (match.Groups[2].Success) return oneStarPattern;
            if (match.Groups[3].Success) return questionMarkPattern;
            return Regex.Escape(match.Value);
        }

        /// <summary>
        /// Create glob pattern as regular expression text.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public string CreateRegexText(string globPattern)
            => GlobPattern.Replace(globPattern, matchEvaluator);

        /// <summary>
        /// Create glob pattern as compiled regular expression instance.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <returns></returns>
        public Regex CreateRegex(string globPattern)
            => new Regex("^" + CreateRegexText(globPattern) + "$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{GetType().FullName}(DirectorySeparators={DirectorySeparatorChars})";
    }
}
