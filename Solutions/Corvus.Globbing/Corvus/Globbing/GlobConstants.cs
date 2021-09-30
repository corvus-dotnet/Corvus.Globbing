// <copyright file="GlobConstants.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    /// <summary>
    /// Constant values for a glob.
    /// </summary>
    internal static class GlobConstants
    {
        /// <summary>
        /// The null character.
        /// </summary>
        public const char NullCharacter = (char)0;

        /// <summary>
        /// An exclamation mark.
        /// </summary>
        public const char Exclamation = '!';

        /// <summary>
        /// A star.
        /// </summary>
        public const char Star = '*';

        /// <summary>
        /// Open bracket.
        /// </summary>
        public const char OpenSquareBracket = '[';

        /// <summary>
        /// Close sequare bracket.
        /// </summary>
        public const char CloseSquareBracket = ']';

        /// <summary>
        /// Dash.
        /// </summary>
        public const char Dash = '-';

        /// <summary>
        /// Question mark.
        /// </summary>
        public const char QuestionMark = '?';

        /// <summary>
        /// Path separator (Windows).
        /// </summary>
        public const char PathSeparatorWindows = '\\';

        /// <summary>
        /// Path separator (Unix).
        /// </summary>
        public const char PathSeparatorUnix = '/';
    }
}
