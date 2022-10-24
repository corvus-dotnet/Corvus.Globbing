// <copyright file="GlobTokenizer.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Implements simple glob matching.
    /// </summary>
    public static class GlobTokenizer
    {
        /// <summary>
        /// Tokenizes a glob.
        /// </summary>
        /// <param name="glob">The glob to tokenize.</param>
        /// <param name="tokenizedGlob">The tokenized glob.</param>
        /// <returns>The number of tokens created.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Tokenize(ReadOnlySpan<char> glob, in Span<GlobToken> tokenizedGlob)
        {
            int tokenIndex = 0;
            var reader = new GlobReader(glob);
            while (reader.ReadChar())
            {
                if (reader.IsBeginningOfRangeOrList)
                {
                    tokenizedGlob[tokenIndex++] = ReadRangeOrListToken(ref reader);
                }
                else if (reader.IsSingleCharacterMatch)
                {
                    tokenizedGlob[tokenIndex++] = ReadSingleCharacterMatchToken(ref reader);
                }
                else if (reader.IsWildcardCharacterMatch)
                {
                    tokenizedGlob[tokenIndex++] = ReadWildcardToken(ref reader);
                }
                else if (reader.IsPathSeparator)
                {
                    tokenizedGlob[tokenIndex++] = ReadPathSeparatorToken(ref reader);
                }
                else if (reader.IsBeginningOfDirectoryWildcard)
                {
                    if (tokenIndex > 0)
                    {
                        if (tokenizedGlob[tokenIndex - 1].Type == GlobTokenType.PathSeparator)
                        {
                            tokenizedGlob[tokenIndex - 1] = ReadDirectoryWildcardToken(ref reader, tokenizedGlob[tokenIndex - 1]);
                            continue;
                        }
                    }

                    tokenizedGlob[tokenIndex++] = ReadDirectoryWildcardToken(ref reader);
                }
                else
                {
                    tokenizedGlob[tokenIndex++] = ReadLiteralToken(ref reader);
                }
            }

            return tokenIndex;
        }

        /// <summary>
        /// Gets a value indicating whether the leading character of the glob token is a path separator.
        /// </summary>
        /// <param name="glob">The glob string.</param>
        /// <param name="token">The glob token to test.</param>
        /// <returns><see langword="true"/> if the leading character is a separator, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasLeadingPathSeparator(ReadOnlySpan<char> glob, in GlobToken token)
        {
            return IsPathSeparatorChar(glob[token.Start]);
        }

        /// <summary>
        /// Gets a value indicating whether the trailing character of the glob token is a path separator.
        /// </summary>
        /// <param name="glob">The glob string.</param>
        /// <param name="token">The glob token to test.</param>
        /// <returns><see langword="true"/> if the trailing character is a separator, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTrailingPathSeparator(ReadOnlySpan<char> glob, in GlobToken token)
        {
            return IsPathSeparatorChar(glob[token.End]);
        }

        /// <summary>
        /// Gets a value indicating whether the given character is a path separator.
        /// </summary>
        /// <param name="character">The character to test.</param>
        /// <returns><see langword="true"/> if the character is a path separator, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsPathSeparatorChar(in char character)
        {
            return character == GlobConstants.PathSeparatorWindows ||
                   character == GlobConstants.PathSeparatorUnix;
        }

        /// <summary>
        /// Gets a value indicating whether the given character is a start of token character.
        /// </summary>
        /// <param name="character">The character to test.</param>
        /// <returns><see langword="true"/> if the character is a start-of-token character, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsStartOfTokenChar(in char character)
        {
            return
                character == GlobConstants.Star ||
                character == GlobConstants.OpenSquareBracket ||
                character == GlobConstants.QuestionMark;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadRangeOrListToken(ref GlobReader reader)
        {
            bool isNegated = false;
            bool isLetterRange = false;
            bool isCharList = false;
            if (reader.PeekChar() == GlobConstants.Exclamation)
            {
                // Negate and skip over the exclamation
                isNegated = true;
                reader.ReadChar();
            }

            // There must be a next character...
            char nextChar = reader.PeekChar();

            int startIndex;
            if (char.IsLetterOrDigit(nextChar))
            {
                // So read to the next character
                reader.ReadChar();

                // And peek the character after that
                nextChar = reader.PeekChar();
                if (nextChar == GlobConstants.Dash)
                {
                    // The character was a dash, so this is a character range of either
                    isLetterRange = true;
                }
                else
                {
                    // The next character wasn't a dash, so this is a character list.
                    isCharList = true;
                }

                // Either way, the start index of this token is the first character in the list or range
                // (so we exclude the brackets, negation etc)
                startIndex = reader.CurrentIndex;
            }
            else
            {
                // The first character wasn't a letter or digit, so this is definitely a character list
                isCharList = true;
                reader.ReadChar();
                startIndex = reader.CurrentIndex;
            }

            if (isLetterRange)
            {
                // skip over the dash char
                reader.ReadChar();
            }

            while (reader.ReadChar())
            {
                if (reader.IsEndOfRangeOrList)
                {
                    // We have one close bracket, but we escape close square bracket literals
                    // with a second close square brackets, so we should peek ahead and see
                    // if we are in that situation.
                    // e.g. a]] matches a]
                    char peekChar = reader.PeekChar();

                    if (peekChar != GlobConstants.CloseSquareBracket)
                    {
                        break;
                    }
                }
            }

            // We don't include the close square bracket in our range
            if (isCharList)
            {
                return new GlobToken(startIndex, reader.CurrentIndex - 1, isNegated ? GlobTokenType.NegatedCharacterList : GlobTokenType.CharacterList);
            }

            return new GlobToken(startIndex, reader.CurrentIndex - 1, isNegated ? GlobTokenType.NegatedLetterRange : GlobTokenType.LetterRange);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadSingleCharacterMatchToken(ref GlobReader reader)
        {
            return new GlobToken(reader.CurrentIndex, reader.CurrentIndex, GlobTokenType.AnyCharacter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadLiteralToken(ref GlobReader reader)
        {
            int startIndex = reader.CurrentIndex;
            while (!reader.IsAtEnd)
            {
                char peekChar = reader.PeekChar();
                bool canAccept = !IsStartOfTokenChar(peekChar) && !IsPathSeparatorChar(peekChar);

                if (canAccept)
                {
                    if (!reader.ReadChar())
                    {
                        break;
                    }
                }
                else
                {
                    // This is either an unsupported string literal character, or the start of a string.
                    break;
                }
            }

            return new GlobToken(startIndex, reader.CurrentIndex, GlobTokenType.Literal);
        }

        /// <summary>
        /// Reads a character wildcard token.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadWildcardToken(ref GlobReader reader)
        {
            return new GlobToken(reader.CurrentIndex, reader.CurrentIndex, GlobTokenType.Wildcard);
        }

        /// <summary>
        /// Reads a directory wildcard token where we have a leading path separator token.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadDirectoryWildcardToken(ref GlobReader reader, in GlobToken leadingPathSeparatorToken)
        {
            // Read the second character in the directory wildcard
            reader.ReadChar();

            // Check to see if the next character is a path separator
            if (IsPathSeparatorChar(reader.PeekChar()))
            {
                // If so, then read past the path separator character too
                reader.ReadChar();

                // We want to construct a glob token that takes in the whole of the wildcard including the leading path separator, and the trailing path separator.
                return new GlobToken(leadingPathSeparatorToken.Start, reader.CurrentIndex, GlobTokenType.WildcardDirectory);
            }

            return new GlobToken(leadingPathSeparatorToken.Start, reader.CurrentIndex, GlobTokenType.WildcardDirectory);
        }

        /// <summary>
        /// Reads a directory wildcard token where we do not have a leading path separator token.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadDirectoryWildcardToken(ref GlobReader reader)
        {
            int startIndex = reader.CurrentIndex;

            // Read the second character in the directory wildcard
            reader.ReadChar();

            // Check to see if the next character is a path separator
            if (IsPathSeparatorChar(reader.PeekChar()))
            {
                // If so, then read the path separator character too
                reader.ReadChar();

                // We want to construct a glob token that takes in the whole of the wildcard including the leading path separator, and the trailing path separator.
                return new GlobToken(startIndex, reader.CurrentIndex, GlobTokenType.WildcardDirectory);
            }

            return new GlobToken(startIndex, reader.CurrentIndex, GlobTokenType.WildcardDirectory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GlobToken ReadPathSeparatorToken(ref GlobReader reader)
        {
            return new GlobToken(reader.CurrentIndex, reader.CurrentIndex, GlobTokenType.PathSeparator);
        }
    }
}
