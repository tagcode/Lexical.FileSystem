// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.2.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lexical.FileSystem.Utilities
{
    /// <summary>
    /// Alpha numeric string comparer. 
    /// Considers numeric character sequences as numbers which are compared with number sorters.
    /// 
    /// For example: strings "a1", "a10", "a9" would be sorted to "a1", "a9", "a10".
    /// </summary>
    public class AlphaNumericComparer : IComparer, IComparer<string>
    {
        readonly static AlphaNumericComparer instance = new AlphaNumericComparer();

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AlphaNumericComparer Default => instance;

        /// <summary>
        /// Pattern that classifies a substring either as number or text groups.
        /// </summary>
        static Regex pattern = new Regex("(-?[0-9]+)|([^0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        enum Kind { Unknown = 0, Number = 1, Text = 2 }

        /// <summary>
        /// Placeholder for making text literal comparisons.
        /// 
        /// Override this to change behaviour.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x_ix"></param>
        /// <param name="y"></param>
        /// <param name="y_ix"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected virtual int StringCompare(string x, int x_ix, string y, int y_ix, int length)
            => string.Compare(x, x_ix, y, y_ix, length, StringComparison.InvariantCulture);

        /// <summary>
        /// Compare uncasted objects. Calls ToString().
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
            => Compare(x?.ToString(), y?.ToString());

        /// <summary>
        /// Compare strings.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (y == null) return 1;
            if (x == null) return -1;

            MatchCollection x_matches = pattern.Matches(x), y_matches = pattern.Matches(y);
            int min_count = Math.Min(x_matches.Count, y_matches.Count);
            int max_count = Math.Max(x_matches.Count, y_matches.Count);
            for (int ix = 0; ix < min_count; ix++)
            {
                // Get the next match
                Match x_match = x_matches[ix], y_match = y_matches[ix];

                // Test if capture was ok
                bool x_ok = x_match.Success, y_ok = y_match.Success;
                if (!x_ok && !y_ok) return 0;
                if (!x_ok) return -1;
                if (!y_ok) return 1;

                // Put capture into enumeration
                Kind x_kind = x_match.Groups[1].Success ? Kind.Number : x_match.Groups[2].Success ? Kind.Text : Kind.Unknown;
                Kind y_kind = y_match.Groups[1].Success ? Kind.Number : y_match.Groups[2].Success ? Kind.Text : Kind.Unknown;
                if (x_kind == Kind.Unknown || y_kind == Kind.Unknown) return 0;
                if (x_kind > y_kind) return 1;
                if (x_kind < y_kind) return -1;

                // Compare strings
                if (x_kind == Kind.Text)
                {
                    // Take captured segments
                    Group x_group = x_match.Groups[2], y_group = y_match.Groups[2];
                    int len = Math.Min(x_group.Length, y_group.Length);

                    // Compare segments
                    int c = StringCompare(x, x_group.Index, y, y_group.Index, len);

                    // Text comparison was equal, compare lengths
                    if (c == 0) c = Math.Sign(x_group.Length - y_group.Length);

                    // Was there discrepancy
                    if (c != 0) return c;
                }
                else
                {
                    // Capture number groups
                    string x_num = x_match.Groups[1].Value, y_num = y_match.Groups[1].Value;

                    if (x_num.Length >= 18 || y_num.Length >= 18)
                    // decimal
                    {
                        // Parse
                        decimal x_value = decimal.Parse(x_num), y_value = decimal.Parse(y_num);

                        // Compare
                        int c = x_value.CompareTo(y_value);

                        // Discrepancy
                        if (c != 0) return c;
                    }
                    else
                    // Int64
                    {
                        // Parse
                        long x_value = long.Parse(x_num), y_value = long.Parse(y_num);

                        // Compare
                        int c = x_value.CompareTo(y_value);

                        // Discrepancy
                        if (c != 0) return c;
                    }
                }
            }

            // One of the strings still have values
            if (x_matches.Count < y_matches.Count) return -1;
            if (x_matches.Count > y_matches.Count) return 1;

            return 0;
        }
    }
}
