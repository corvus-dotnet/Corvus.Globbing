// <copyright file="Glob.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Implements simple glob matching.
    /// </summary>
    public static class Glob
    {
        private const int MaxGlobTokenArrayLength = 1024;

        /// <summary>
        /// Matches a value against a glob.
        /// </summary>
        /// <param name="glob">The glob to match.</param>
        /// <param name="value">The value to match.</param>
        /// <param name="comparisonType">The string comparison type. It defaults to <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>True if the given value matches the glob.</returns>
        public static bool Match(ReadOnlySpan<char> glob, ReadOnlySpan<char> value, StringComparison comparisonType = StringComparison.Ordinal)
        {
            // There can't be more tokens than there are characters the glob.
            GlobToken[] globTokenArray = Array.Empty<GlobToken>();
            Span<GlobToken> globTokens = stackalloc GlobToken[0];

            if (glob.Length > MaxGlobTokenArrayLength)
            {
                globTokenArray = ArrayPool<GlobToken>.Shared.Rent(glob.Length);
                globTokens = globTokenArray.AsSpan();
            }
            else
            {
                globTokens = stackalloc GlobToken[glob.Length];
            }

            try
            {
                int tokenCount = GlobTokenizer.Tokenize(glob, ref globTokens);
                ReadOnlySpan<GlobToken> tokenizedGlob = globTokens.Slice(0, tokenCount);
                return Match(glob, tokenizedGlob, value, comparisonType);
            }
            finally
            {
                if (glob.Length > MaxGlobTokenArrayLength)
                {
                    ArrayPool<GlobToken>.Shared.Return(globTokenArray);
                }
            }
        }

        /// <summary>
        /// Matches a value against a tokenized glob.
        /// </summary>
        /// <param name="glob">The original glob.</param>
        /// <param name="tokenizedGlob">The tokenized glob.</param>
        /// <param name="value">The value to match.</param>
        /// <param name="comparisonType">The string comparison type. It defaults to <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>True if the given value matches the tokenized glob.</returns>
        public static bool Match(ReadOnlySpan<char> glob, ReadOnlySpan<GlobToken> tokenizedGlob, ReadOnlySpan<char> value, StringComparison comparisonType = StringComparison.Ordinal)
        {
            bool isMatched = Match(glob, tokenizedGlob, value, comparisonType, out int charactersMatched, out int tokensMatched);
            return isMatched && charactersMatched == value.Length && tokensMatched == tokenizedGlob.Length;
        }

        /// <summary>
        /// Matches a value against a tokenized glob.
        /// </summary>
        /// <param name="glob">The original glob.</param>
        /// <param name="tokenizedGlob">The tokenized glob.</param>
        /// <param name="value">The value to match.</param>
        /// <param name="comparisonType">The string comparison type. It defaults to <see cref="StringComparison.Ordinal"/>.</param>
        /// <param name="charactersMatched">The number of characters matched.</param>
        /// <param name="tokensConsumed">The number of tokens consumed.</param>
        /// <returns>True if the given value matches the tokenized glob.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Match(ReadOnlySpan<char> glob, ReadOnlySpan<GlobToken> tokenizedGlob, ReadOnlySpan<char> value, StringComparison comparisonType, out int charactersMatched, out int tokensConsumed)
        {
            int valueCharsRead = 0;
            int tokenIndex = 0;

            while (tokenIndex < tokenizedGlob.Length)
            {
                if (!Match(glob, tokenizedGlob, tokenIndex, value[valueCharsRead..], comparisonType, out int internalCharactersMatched, out int internalTokensConsumed))
                {
                    charactersMatched = 0;
                    tokensConsumed = 0;
                    return false;
                }

                valueCharsRead += internalCharactersMatched;
                tokenIndex += internalTokensConsumed;
            }

            charactersMatched = valueCharsRead;
            tokensConsumed = tokenIndex;
            return true;
        }

        /// <summary>
        /// Matches a particular token against an input span.
        /// </summary>
        /// <param name="glob">The glob to match.</param>
        /// <param name="tokenizedGlob">The tokenized glob to match.</param>
        /// <param name="tokenIndex">The current token index.</param>
        /// <param name="value">The span of characters against which to match the token.</param>
        /// <param name="comparisonType">The string comparison type.</param>
        /// <param name="charactersMatched">The number of characters matched the span.</param>
        /// <param name="tokensConsumed">The number of tokens consumed.</param>
        /// <returns><see langword="true"/> if the glob matched any number of characters from the start of the input value, otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Match(ReadOnlySpan<char> glob, ReadOnlySpan<GlobToken> tokenizedGlob, int tokenIndex, ReadOnlySpan<char> value, StringComparison comparisonType, out int charactersMatched, out int tokensConsumed)
        {
            GlobToken currentToken = tokenizedGlob[tokenIndex];
            switch (currentToken.Type)
            {
                case GlobTokenType.Literal:
                    return MatchLiteral(glob, currentToken, value, comparisonType, out charactersMatched, out tokensConsumed);
                case GlobTokenType.PathSeparator:
                    return MatchPathSeparator(value, out charactersMatched, out tokensConsumed);
                case GlobTokenType.Wildcard:
                    return MatchWildcard(glob, tokenizedGlob, tokenIndex, value, comparisonType, out charactersMatched, out tokensConsumed);
                case GlobTokenType.WildcardDirectory:
                    return MatchWildcardDirectory(glob, tokenizedGlob, tokenIndex, value, comparisonType, out charactersMatched, out tokensConsumed);
                case GlobTokenType.AnyCharacter:
                    return MatchAnyCharacter(value, out charactersMatched, out tokensConsumed);
                case GlobTokenType.CharacterList:
                    return MatchCharacterList(glob, currentToken, value, comparisonType, false, out charactersMatched, out tokensConsumed);
                case GlobTokenType.LetterRange:
                    return MatchLetterRange(glob, currentToken, value, comparisonType, false, out charactersMatched, out tokensConsumed);
                case GlobTokenType.NegatedCharacterList:
                    return MatchCharacterList(glob, currentToken, value, comparisonType, true, out charactersMatched, out tokensConsumed);
                case GlobTokenType.NegatedLetterRange:
                    return MatchLetterRange(glob, currentToken, value, comparisonType, true, out charactersMatched, out tokensConsumed);
                case GlobTokenType.NegatedNumberRange:
                    return MatchNumberRange(glob, currentToken, value, comparisonType, true, out charactersMatched, out tokensConsumed);
                case GlobTokenType.NumberRange:
                    return MatchNumberRange(glob, currentToken, value, comparisonType, false, out charactersMatched, out tokensConsumed);
                default:
                    charactersMatched = 0;
                    tokensConsumed = 0;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchWildcardDirectory(ReadOnlySpan<char> glob, ReadOnlySpan<GlobToken> tokenizedGlob, int tokenIndex, ReadOnlySpan<char> value, StringComparison comparisonType, out int charactersMatched, out int tokensConsumed)
        {
            // If we are matching a directory, there must be at least one path separator the remaining string.
            if (value.Length == 0)
            {
                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            int currentPosition = 0;

            // First, check to see if we require a leading separator
            char currentChar = value[0];
            if (GlobTokenizer.HasLeadingPathSeparator(glob, tokenizedGlob[tokenIndex]))
            {
                if (!GlobTokenizer.IsPathSeparatorChar(currentChar))
                {
                    charactersMatched = 0;
                    tokensConsumed = 0;
                    return false;
                }

                currentPosition++;
            }
            else
            {
                // There is no explicit leading separator the glob (i.e. we have specified ** not /**)
                // However, the input string may or may not support a leading path separator - it is optional.
                // (so either /foo/bar or foo/bar will match)
                if (GlobTokenizer.IsPathSeparatorChar(currentChar))
                {
                    // Eat the leading path separator
                    currentPosition++;
                }
            }

            // If we have no other tokens to consume, we know we match
            if (tokenizedGlob.Length <= tokenIndex + 1)
            {
                charactersMatched = value.Length;
                tokensConsumed = 1;
                return true;
            }

            // If the remaining tokens are optional (i.e. consume a minimum of 0 tokens) then we can also match.
            ReadOnlySpan<GlobToken> remainingPattern = tokenizedGlob[(tokenIndex + 1) ..];
            int remainingMinLength = remainingPattern.Length == 0 ? 0 : SumMatchesMinLength(remainingPattern);
            if (remainingMinLength == 0)
            {
                charactersMatched = value.Length;
                tokensConsumed = tokenizedGlob.Length - tokenIndex;
                return true;
            }

            // We know that we have additional tokens to evaluate, so our wildcard can consume *at most*
            // the difference between the minimum number of characters that the additional tokens consume, and the total
            // length of the characters available.
            int maxPos = value.Length - remainingMinLength;

            // If there are not enough characters to provide a match, then we can fail fast.
            if (currentPosition > maxPos)
            {
                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            // If our remaining tokens consume a fixed number of characters, we can short-cut the match
            if (MatchesFixedLength(remainingPattern))
            {
                // Our directory wildcard is only allowed to match full segments, so, the previous character must be a separator.
                if (maxPos > 0)
                {
                    char lastCharacterInDirectoryWildcard = value[maxPos - 1];
                    if (!GlobTokenizer.IsPathSeparatorChar(lastCharacterInDirectoryWildcard))
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }
                }

                // We can skip ahead to "maxPos" because we can match anything whatsoever between those directory separators.
                // And then match the remaining tokens to that segment.
                if (Match(glob, remainingPattern, value[maxPos..], comparisonType, out int internalCharactersMatched, out int internalTokensConsumed))
                {
                    charactersMatched = maxPos + internalCharactersMatched;
                    tokensConsumed = internalTokensConsumed + 1;
                    return true;
                }

                // We didn't match the remaining tokens.
                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }
            else
            {
                // The remaining tokens match a variable length of the remaining value.
                // We iterate the substring starting at the minimum position.
                bool isMatch;

                // If the ** token was parsed with a trailing slash - i.e "**/", then we can read past the
                // token if it is a directory separator
                if (GlobTokenizer.HasTrailingPathSeparator(glob, tokenizedGlob[tokenIndex]))
                {
                    if (GlobTokenizer.IsPathSeparatorChar(value[currentPosition]))
                    {
                        // consume the separator.
                        currentPosition += 1;
                    }
                }

                // Keep track of whether we have seen a separator; we must see at least one separator to be a whole segment
                bool matchedSeparator = false;

                // Iterate throught the string until we reach the
                // maximum possible substring
                while (currentPosition <= maxPos)
                {
                    if (currentPosition == maxPos)
                    {
                        // We must have seen at least one separator
                        if (!matchedSeparator)
                        {
                            charactersMatched = 0;
                            tokensConsumed = 0;
                            return false;
                        }
                    }

                    isMatch = Match(glob, remainingPattern, value[currentPosition..], comparisonType, out int internalCharactersMatched, out int internalTokensConsumed);
                    if (isMatch)
                    {
                        charactersMatched = currentPosition + internalCharactersMatched;
                        tokensConsumed = internalTokensConsumed + 1;
                        return true;
                    }

                    // If it wasn't a match, and we've advanced as far as maxPos, we have failed to match.
                    if (currentPosition == maxPos)
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }

                    // Iterate until we hit the next separator or maxPos.
                    matchedSeparator = false;
                    while (currentPosition < maxPos)
                    {
                        currentPosition++;
                        currentChar = value[currentPosition];

                        if (GlobTokenizer.IsPathSeparatorChar(currentChar))
                        {
                            // consume the separator.
                            matchedSeparator = true;
                            currentPosition++;
                            break;
                        }
                    }
                }
            }

            // We failed to match
            charactersMatched = 0;
            tokensConsumed = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchWildcard(ReadOnlySpan<char> glob, ReadOnlySpan<GlobToken> tokenizedGlob, int tokenIndex, ReadOnlySpan<char> value, StringComparison comparisonType, out int charactersMatched, out int tokensConsumed)
        {
            // First, check to see if we are the last token the glob
            if (tokenIndex == tokenizedGlob.Length - 1)
            {
                // We are the last token the glob
                // If we have reached the end of the value, then we match, because it is valid for * to match 0 characters.
                if (value.Length == 0)
                {
                    charactersMatched = 0;
                    tokensConsumed = 1;
                    return true;
                }

                // if we have any more separators, then we don't match
                foreach (char character in value)
                {
                    if (GlobTokenizer.IsPathSeparatorChar(character))
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }
                }

                // We have matched the entire length of the value
                charactersMatched = value.Length;
                tokensConsumed = 1;
                return true;
            }

            // We are not the last token the glob, and so we need to ensure that however much we consume, the remaining tokens also match.
            // First, we ask: does the rest of pattern match a fixed length, or variable length?
            ReadOnlySpan<GlobToken> remainingPattern = tokenizedGlob[(tokenIndex + 1) ..];
            if (MatchesFixedLength(remainingPattern))
            {
                // The remaining pattern is a fixed length, so, regardless of whether that matches or not,
                // our wild card must match whatever characters remain.
                int requiredMatchPosition = value.Length - SumMatchesMinLength(remainingPattern);

                if (requiredMatchPosition < 0)
                {
                    tokensConsumed = 0;
                    charactersMatched = 0;
                    return false;
                }

                for (int i = 0; i < requiredMatchPosition; i++)
                {
                    char currentChar = value[i];
                    if (GlobTokenizer.IsPathSeparatorChar(currentChar))
                    {
                        tokensConsumed = 0;
                        charactersMatched = 0;
                        return false;
                    }
                }

                if (Match(glob, remainingPattern, value[requiredMatchPosition..], comparisonType, out int remainingMatched, out int remainingConsumed))
                {
                    charactersMatched = requiredMatchPosition + remainingMatched;
                    tokensConsumed = remainingConsumed + 1;
                    return true;
                }

                tokensConsumed = 0;
                charactersMatched = 0;
                return false;
            }

            // If we entered the previous "if" statement, we have returned from the method, so this is effectively
            // the else clause. However, StyleCop wants us to simplify the else away; so I'm adding this comment
            // where a couple of curly brackets would have done :)

            // If we get here, we are matching a variable number of characters
            // There are a couple of constraints on this:
            // 1. After we've matched our characters, there must be *at least* the mininimum number of characters left to match by they remaining evaluator tokens
            // 2. This is not a directory wildcard, so we cannot match past a path separator.
            int maxPos = value.Length - 1;
            int remainingMatchesMinLength = SumMatchesMinLength(remainingPattern);
            if (remainingMatchesMinLength > 0)
            {
                maxPos = maxPos - remainingMatchesMinLength + 1;
            }

            // Run through the remaining characters until we find either a path separator character (which case we are no longer valid)
            // (or we get a complete match for the remaining parts of the glob pattern
            for (int i = 0; i <= maxPos; i++)
            {
                bool isMatch = Match(glob, remainingPattern, value[i..], comparisonType, out int remainingMatched, out int remainingTokensConsumed);
                if (isMatch)
                {
                    charactersMatched = i + remainingMatched;
                    tokensConsumed = remainingTokensConsumed + 1;
                    return true;
                }

                char currentChar = value[i];
                if (GlobTokenizer.IsPathSeparatorChar(value[i]))
                {
                    charactersMatched = 0;
                    tokensConsumed = 0;
                    return false;
                }
            }

            // We have reached the end of the value without hitting a path separator,
            // but we didn't match the remaining glob pattern either. This is OK
            // if the remaining pattern is allowed to consume zero characters.
            if (remainingMatchesMinLength == 0)
            {
                charactersMatched = value.Length;
                tokensConsumed = remainingPattern.Length + 1;
                return true;
            }

            charactersMatched = 0;
            tokensConsumed = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchPathSeparator(ReadOnlySpan<char> value, out int charactersMatched, out int tokensConsumed)
        {
            if (GlobTokenizer.IsPathSeparatorChar(value[0]))
            {
                charactersMatched = 1;
                tokensConsumed = 1;
                return true;
            }

            charactersMatched = 0;
            tokensConsumed = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchNumberRange(ReadOnlySpan<char> glob, GlobToken currentToken, ReadOnlySpan<char> value, StringComparison comparisonType, bool isNegated, out int charactersMatched, out int tokensConsumed)
        {
            return MatchLetterRange(glob, currentToken, value, comparisonType, isNegated, out charactersMatched, out tokensConsumed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchLiteral(ReadOnlySpan<char> glob, GlobToken currentToken, ReadOnlySpan<char> value, StringComparison comparisonType, out int charactersMatched, out int tokensConsumed)
        {
            int length = currentToken.Length;
            if (value.Length < currentToken.Length)
            {
                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            if (comparisonType == StringComparison.Ordinal)
            {
                for (int i = currentToken.Start, j = 0; i <= currentToken.End; ++i, ++j)
                {
                    if (value[j] != glob[i])
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }
                }
            }
            else
            {
                ReadOnlySpan<char> literalToMatch = glob.Slice(currentToken.Start, length);
                if (!value.StartsWith(literalToMatch, comparisonType))
                {
                    charactersMatched = 0;
                    tokensConsumed = 0;
                    return false;
                }
            }

            charactersMatched = length;
            tokensConsumed = 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchLetterRange(ReadOnlySpan<char> glob, GlobToken currentToken, ReadOnlySpan<char> value, StringComparison comparisonType, bool isNegated, out int charactersMatched, out int tokensConsumed)
        {
            if (value.Length == 0)
            {
                if (isNegated)
                {
                    charactersMatched = 1;
                    tokensConsumed = 1;
                    return true;
                }

                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            if (comparisonType == StringComparison.Ordinal)
            {
                char currentChar = value[0];
                if (currentChar >= glob[currentToken.Start] && currentChar <= glob[currentToken.End])
                {
                    if (isNegated)
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }

                    charactersMatched = 1;
                    tokensConsumed = 1;
                    return true;
                }
            }
            else
            {
                ReadOnlySpan<char> currentValue = value.Slice(0, 1);
                ReadOnlySpan<char> startCharacter = glob.Slice(currentToken.Start, 1);
                ReadOnlySpan<char> endCharacter = glob.Slice(currentToken.End, 1);

                if (currentValue.CompareTo(startCharacter, comparisonType) >= 0 && currentValue.CompareTo(endCharacter, comparisonType) <= 0)
                {
                    if (isNegated)
                    {
                        charactersMatched = 0;
                        tokensConsumed = 0;
                        return false;
                    }

                    charactersMatched = 1;
                    tokensConsumed = 1;
                    return true;
                }
            }

            if (isNegated)
            {
                charactersMatched = 1;
                tokensConsumed = 1;
                return true;
            }

            charactersMatched = 0;
            tokensConsumed = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchCharacterList(ReadOnlySpan<char> glob, GlobToken currentToken, ReadOnlySpan<char> value, StringComparison comparisonType, bool isNegated, out int charactersMatched, out int tokensConsumed)
        {
            if (value.Length == 0)
            {
                if (isNegated)
                {
                    charactersMatched = 1;
                    tokensConsumed = 1;
                    return true;
                }

                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            if (comparisonType == StringComparison.Ordinal)
            {
                char currentValue = value[0];
                for (int i = currentToken.Start; i <= currentToken.End; ++i)
                {
                    if (currentValue == glob[i])
                    {
                        if (isNegated)
                        {
                            charactersMatched = 0;
                            tokensConsumed = 0;
                            return false;
                        }

                        charactersMatched = 1;
                        tokensConsumed = 1;
                        return true;
                    }
                }
            }
            else
            {
                ReadOnlySpan<char> currentValue = value.Slice(0, 1);
                for (int i = currentToken.Start; i <= currentToken.End; ++i)
                {
                    if (currentValue.Equals(glob.Slice(i, 1), comparisonType))
                    {
                        if (isNegated)
                        {
                            charactersMatched = 0;
                            tokensConsumed = 0;
                            return false;
                        }

                        charactersMatched = 1;
                        tokensConsumed = 1;
                        return true;
                    }
                }
            }

            if (isNegated)
            {
                charactersMatched = 1;
                tokensConsumed = 1;
                return true;
            }

            charactersMatched = 0;
            tokensConsumed = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchAnyCharacter(ReadOnlySpan<char> value, out int charactersMatched, out int tokensConsumed)
        {
            // This actually matches any character except a path separator.
            if (GlobTokenizer.IsPathSeparatorChar(value[0]))
            {
                charactersMatched = 0;
                tokensConsumed = 0;
                return false;
            }

            charactersMatched = 1;
            tokensConsumed = 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FixedLengthFor(GlobToken token)
        {
            return token.Type switch
            {
                GlobTokenType.AnyCharacter => 1,
                GlobTokenType.CharacterList => 1,
                GlobTokenType.LetterRange => 1,
                GlobTokenType.Literal => token.Length,
                GlobTokenType.NegatedCharacterList => 1,
                GlobTokenType.NegatedLetterRange => 1,
                GlobTokenType.NegatedNumberRange => 1,
                GlobTokenType.NumberRange => 1,
                GlobTokenType.PathSeparator => 1,
                _ => 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SumMatchesMinLength(ReadOnlySpan<GlobToken> tokenizedGlob)
        {
            int accumulator = 0;
            int startIndex = 0;

            // Work from the start point through the remaining tokens
            while (startIndex < tokenizedGlob.Length)
            {
                GlobToken token = tokenizedGlob[startIndex];

                // If this is a fixed length token...
                if (MatchesFixedLength(token))
                {
                    // ...just add the length of the token, and advance to the next token.
                    accumulator += FixedLengthFor(token);
                    ++startIndex;
                }
                else
                {
                    // otherwise, perform a sum over the remaining elements, and then drop out
                    // (as we will have visited all the elements by the end of this calculation).
                    accumulator += SumMatchesMinLength(tokenizedGlob[(startIndex + 1) ..]);
                    break;
                }
            }

            return accumulator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesFixedLength(ReadOnlySpan<GlobToken> tokenizedGlob)
        {
            foreach (GlobToken token in tokenizedGlob)
            {
                if (!MatchesFixedLength(token))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesFixedLength(GlobToken token)
        {
            return token.Type switch
            {
                GlobTokenType.AnyCharacter => true,
                GlobTokenType.CharacterList => true,
                GlobTokenType.LetterRange => true,
                GlobTokenType.Literal => true,
                GlobTokenType.NegatedCharacterList => true,
                GlobTokenType.NegatedLetterRange => true,
                GlobTokenType.NegatedNumberRange => true,
                GlobTokenType.NumberRange => true,
                GlobTokenType.PathSeparator => true,
                GlobTokenType.Wildcard => false,
                GlobTokenType.WildcardDirectory => false,
                _ => false,
            };
        }
    }
}
