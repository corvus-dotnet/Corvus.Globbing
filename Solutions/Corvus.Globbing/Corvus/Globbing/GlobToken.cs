// <copyright file="GlobToken.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing
{
    using System;

    /// <summary>
    /// A token in a glob.
    /// </summary>
    public readonly struct GlobToken
    {
        private readonly uint value;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobToken"/> struct.
        /// </summary>
        /// <param name="start">The 12-bit start index of the token.</param>
        /// <param name="end">The 12-bit end index of the token.</param>
        /// <param name="type">The type of the token.</param>
        public GlobToken(int start, int end, GlobTokenType type)
        {
            if ((start & 0xFFFFF000) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if ((end & 0xFFFFF000) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            this.value = (uint)(((start & 0x0FFF) << 20) | ((end & 0x0FFF) << 8) | (int)type);
        }

        /// <summary>
        /// Gets the index of the start of the token.
        /// </summary>
        public int Start => unchecked((int)(this.value >> 20)); // Top 12 bits of our packed value

        /// <summary>
        /// Gets the index of the end of the token.
        /// </summary>
        public int End => unchecked((ushort)this.value >> 8); // bottom 12 bits of our packed value

        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        public GlobTokenType Type => (GlobTokenType)unchecked((byte)this.value);
    }
}
