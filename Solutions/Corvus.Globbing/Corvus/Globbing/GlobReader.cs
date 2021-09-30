// <copyright file="GlobReader.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    using System;

    /// <summary>
    /// A character reader for a glob string.
    /// </summary>
    internal ref struct GlobReader
    {
        private readonly ReadOnlySpan<char> text;
        private int currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobReader"/> struct.
        /// </summary>
        /// <param name="text">The glob text.</param>
        public GlobReader(ReadOnlySpan<char> text)
        {
            this.text = text;
            this.currentIndex = -1;
        }

        /// <summary>
        /// Gets the index of the current character.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return this.currentIndex;
            }
        }

        /// <summary>
        /// Gets the current character.
        /// </summary>
        public char CurrentChar => this.text[this.currentIndex];

        /// <summary>
        /// Gets a value indicating whether we have reached the end of the string.
        /// </summary>
        public bool IsAtEnd
        {
            get { return this.currentIndex == this.text.Length - 1; }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is the beginning of a range or list.
        /// </summary>
        public bool IsBeginningOfRangeOrList
        {
            get { return this.CurrentChar == GlobConstants.OpenSquareBracket; }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is the end of a range or list.
        /// </summary>
        public bool IsEndOfRangeOrList
        {
            get { return this.CurrentChar == GlobConstants.CloseSquareBracket; }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is a path separator.
        /// </summary>
        public bool IsPathSeparator
        {
            get
            {
                return GlobTokenizer.IsPathSeparatorChar(this.CurrentChar);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is a single-character match.
        /// </summary>
        public bool IsSingleCharacterMatch
        {
            get { return this.CurrentChar == GlobConstants.QuestionMark; }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is a character wildcard match.
        /// </summary>
        public bool IsWildcardCharacterMatch
        {
            get { return this.CurrentChar == GlobConstants.Star && (this.IsAtEnd || this.PeekChar() != GlobConstants.Star); }
        }

        /// <summary>
        /// Gets a value indicating whether the current character is a the start of a directory wildcard match.
        /// </summary>
        public bool IsBeginningOfDirectoryWildcard
        {
            get { return this.CurrentChar == GlobConstants.Star && !this.IsAtEnd && this.PeekChar() == GlobConstants.Star; }
        }

        /// <summary>
        /// Read a character.
        /// </summary>
        /// <returns>True if the character was read successfully, otherwise false.</returns>
        public bool ReadChar()
        {
            if (!this.IsAtEnd)
            {
                this.currentIndex++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Peek the next character.
        /// </summary>
        /// <returns>The next character.</returns>
        public char PeekChar()
        {
            return this.IsAtEnd ? GlobConstants.NullCharacter : this.text[this.currentIndex + 1];
        }
    }
}
