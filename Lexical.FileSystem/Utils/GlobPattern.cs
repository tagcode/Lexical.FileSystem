// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Text.RegularExpressions;

namespace Lexical.FileSystem.Utils
{
    /// <summary>
    /// Glob pattern uses the following notation:
    ///   "*" matches to string of characters within the same directory.
    ///   "?" matches to any character except directory separator.
    ///   "**" matches to any characters, including directory separators.
    ///   
    /// For example: "**.zip/**.dll" 
    /// </summary>
    public class GlobPattern : Regex
    {
        static string MakeRegexPattern(string globPattern, string directorySeparatorCharacters)
            => "^" + GlobPatternFactory.Create(directorySeparatorCharacters).CreateRegexText(globPattern ?? throw new ArgumentNullException(nameof(globPattern))) + "$";

        /// <summary>
        /// Create glob pattern.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorCharacters"></param>
        public GlobPattern(string globPattern, string directorySeparatorCharacters) : base(MakeRegexPattern(globPattern, directorySeparatorCharacters)) { }

        /// <summary>
        /// Create glob pattern with regexp options.
        /// </summary>
        /// <param name="globPattern"></param>
        /// <param name="directorySeparatorCharacters"></param>
        /// <param name="options"></param>
        public GlobPattern(string globPattern, string directorySeparatorCharacters, RegexOptions options) : base(MakeRegexPattern(globPattern, directorySeparatorCharacters), options) { }
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
    public class GlobPatternFactory
    {
        static GlobPatternFactory singletonSlash = new GlobPatternFactory("/");
        static GlobPatternFactory singletonBackslash = new GlobPatternFactory("\\");
        static GlobPatternFactory singletonSlashAndBackslash = new GlobPatternFactory("/\\");

        /// <summary>
        /// Singleton instance that assumes "/" is the directory separator.
        /// </summary>
        public static GlobPatternFactory Slash => singletonSlash;

        /// <summary>
        /// Singleton instance that assumes "\" is the directory separator.
        /// </summary>
        public static GlobPatternFactory Backslash => singletonBackslash;

        /// <summary>
        /// Singleton instance that assumes that both "/" and "\" are used as directory separators.
        /// </summary>
        public static GlobPatternFactory SlashAndBackslash => singletonSlashAndBackslash;

        /// <summary>
        /// Get or create GlobPatternFactory
        /// </summary>
        /// <param name="directorySeparatorCharacters">separator characters, e.g. "/\\"</param>
        /// <returns>glob pattern factory</returns>
        public static GlobPatternFactory Create(string directorySeparatorCharacters)
        {
            if (directorySeparatorCharacters == null) throw new ArgumentNullException(nameof(directorySeparatorCharacters));
            switch (directorySeparatorCharacters)
            {
                case "/": return GlobPatternFactory.Slash;
                case "\\": return GlobPatternFactory.Backslash;
                case "\\/":
                case "/\\": return GlobPatternFactory.SlashAndBackslash;
                default: return new GlobPatternFactory(directorySeparatorCharacters);
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
        public GlobPatternFactory(string directorySeparatorChars, MatchEvaluator nonglobtextReplacer = default)
        {
            this.DirectorySeparatorChars = directorySeparatorChars;
            this.nonglobtextReplacer = nonglobtextReplacer ?? (m => Regex.Escape(m.Value));
            twoStarPattern = ".*";
            oneStarPattern = "[^" + Regex.Escape(directorySeparatorChars) + "]*";
            questionMarkPattern = "[^" + Regex.Escape(directorySeparatorChars) + "]+";
            matchEvaluator = MatchEvaluator;
        }

        string MatchEvaluator(Match match)
        {
            if (match.Groups[1].Success) return twoStarPattern;
            if (match.Groups[2].Success) return oneStarPattern;
            if (match.Groups[3].Success) return questionMarkPattern;
            return Regex.Escape(match.Value);
        }

        public string CreateRegexText(string globPattern)
            => GlobPattern.Replace(globPattern, matchEvaluator);

        public Regex CreateRegex(string globPattern)
            => new Regex("^" + CreateRegexText(globPattern) + "$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public override string ToString()
            => $"{GetType().FullName}(DirectorySeparators={DirectorySeparatorChars})";
    }
}
