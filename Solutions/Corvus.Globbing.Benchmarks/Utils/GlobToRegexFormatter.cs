// <copyright file="GlobToRegexFormatter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>
// <license>
// Derived from code https://github.com/dazinator/DotNet.Glob/blob/develop/LICENSE
// </license>

namespace Corvus.Globbing.Benchmarks.Utils
{
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using DotNet.Globbing;
    using DotNet.Globbing.Token;

    /// <summary>
    /// Formats a glob as a Regular expression string.
    /// </summary>
    public class GlobToRegexFormatter : IGlobTokenVisitor
    {
        private readonly StringBuilder stringBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobToRegexFormatter"/> class.
        /// </summary>
        public GlobToRegexFormatter()
        {
            this.stringBuilder = new StringBuilder();
        }

        /// <summary>
        /// Formats the given glob tokens as a string.
        /// </summary>
        /// <param name="tokens">The glob tokens from which to build the string.</param>
        /// <returns>The glob string representing the glob tokens.</returns>
        public string Format(IEnumerable<IGlobToken> tokens)
        {
            this.stringBuilder.Clear();
            this.stringBuilder.Append('^');
            foreach (IGlobToken? token in tokens)
            {
                token.Accept(this);
            }

            this.stringBuilder.Append('$');
            return this.stringBuilder.ToString();
        }

        /// <inheritdoc />
        public void Visit(WildcardDirectoryToken wildcardDirectoryToken)
        {
            this.stringBuilder.Append(".*");
            if (wildcardDirectoryToken.TrailingPathSeparator != null)
            {
                this.stringBuilder.Append(@"[/\\]");
            }
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(CharacterListToken token)
        {
            this.stringBuilder.Append('[');
            if (token.IsNegated)
            {
                this.stringBuilder.Append('^');
            }

            this.stringBuilder.Append(Regex.Escape(new string(token.Characters)));
            this.stringBuilder.Append(']');
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(PathSeparatorToken token)
        {
            this.stringBuilder.Append(@"[/\\]");
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(LiteralToken token)
        {
            this.stringBuilder.Append('(');
            this.stringBuilder.Append(Regex.Escape(token.Value));
            this.stringBuilder.Append(')');
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(LetterRangeToken token)
        {
            this.stringBuilder.Append('[');
            if (token.IsNegated)
            {
                this.stringBuilder.Append('^');
            }

            this.stringBuilder.Append(Regex.Escape(token.Start.ToString()));
            this.stringBuilder.Append('-');
            this.stringBuilder.Append(Regex.Escape(token.End.ToString()));
            this.stringBuilder.Append(']');
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(NumberRangeToken token)
        {
            this.stringBuilder.Append('[');
            if (token.IsNegated)
            {
                this.stringBuilder.Append('^');
            }

            this.stringBuilder.Append(Regex.Escape(token.Start.ToString()));
            this.stringBuilder.Append('-');
            this.stringBuilder.Append(Regex.Escape(token.End.ToString()));
            this.stringBuilder.Append(']');
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(AnyCharacterToken token)
        {
            this.stringBuilder.Append(@"[^/\\]{1}");
        }

        /// <inheritdoc />
        void IGlobTokenVisitor.Visit(WildcardToken token)
        {
            this.stringBuilder.Append(@"[^/\\]*");
        }
    }
}
