// <copyright file="GlobTokenType.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    /// <summary>
    /// The given glob token type.
    /// </summary>
    public enum GlobTokenType
    {
        /// <summary>
        /// Any character (?).
        /// </summary>
        AnyCharacter,

        /// <summary>
        /// Any specific set of characters.
        /// </summary>
        CharacterList,

        /// <summary>
        /// Not any specific characters.
        /// </summary>
        NegatedCharacterList,

        /// <summary>
        /// Any in a range of alphabetical characters [a-Z].
        /// </summary>
        LetterRange,

        /// <summary>
        /// Not any in a range of alphabetical characters.
        /// </summary>
        NegatedLetterRange,

        /// <summary>
        /// A specific sequence of characters.
        /// </summary>
        Literal,

        /// <summary>
        /// Any in a range of numeric characters [0-9].
        /// </summary>
        NumberRange,

        /// <summary>
        /// Not any in a range of numeric characters [0-9].
        /// </summary>
        NegatedNumberRange,

        /// <summary>
        /// A path separator token (/ or \).
        /// </summary>
        PathSeparator,

        /// <summary>
        /// A directory list wildcard (**).
        /// </summary>
        WildcardDirectory,

        /// <summary>
        /// A character list wildcard (*).
        /// </summary>
        Wildcard,
    }
}
