// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IOptionExtensions
    {

        /// <summary>
        /// Get option as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="option"></param>
        /// <returns>Option casted as <typeparamref name="T"/> or null</returns>
        public static T AsOption<T>(this IOption option) where T : IOption
        {
            if (option is T casted) return casted;
            if (option is IAdaptableOption adaptable && adaptable.GetOption(typeof(T)) is T casted_) return casted_;
            return default;
        }

        /// <summary>
        /// Get option as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="option"></param>
        /// <param name="casted"></param>
        /// <returns>true if option casted as <typeparamref name="T"/></returns>
        public static bool TryAsOption<T>(this IOption option, out T casted) where T : IOption
        {
            if (option is T _casted) { casted = _casted; return true; }
            if (option is IAdaptableOption adaptable && adaptable.GetOption(typeof(T)) is T casted_) { casted = casted_; return true; }
            casted = default;
            return default;
        }

        /// <summary>
        /// Get sub-path option.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static String SubPath(this IOption filesystemOption)
            => filesystemOption.AsOption<ISubPathOption>() is ISubPathOption mp ? mp.SubPath : null;
    }
}
