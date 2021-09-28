// <copyright file="GlobToken.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    /// <summary>
    /// A token in a glob.
    /// </summary>
    public readonly struct GlobToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobToken"/> struct.
        /// </summary>
        /// <param name="start">The start index of the token.</param>
        /// <param name="end">The end index of the token.</param>
        /// <param name="type">The type of the token.</param>
        public GlobToken(int start, int end, GlobTokenType type)
        {
            this.Start = start;
            this.End = end;
            this.Type = type;
        }

        /// <summary>
        /// Gets the index of the start of the token.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the index of the end of the token.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the length of the token.
        /// </summary>
        public int Length => (this.End - this.Start) + 1;

        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        public GlobTokenType Type { get; }
    }
}
