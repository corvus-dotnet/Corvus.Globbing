// <copyright file="BaselineRegexGlobCompileBenchmarks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>
// <license>
// Derived from code https://github.com/dazinator/DotNet.Glob/blob/develop/LICENSE
// </license>

namespace Corvus.Globbing.Benchmarks
{
    using System;
    using System.Text.RegularExpressions;
    using BenchmarkDotNet.Attributes;
    using Corvus.Globbing.Benchmarks.Utils;
    using DotNet.Globbing;

    /// <summary>
    /// Regex compilation benchmarks.
    /// </summary>
    [MemoryDiagnoser]
    public class BaselineRegexGlobCompileBenchmarks : BaseGlobBenchMark
    {
        private int? patternLength;
        private string? pattern;
        private string? regexString;

        /// <inheritdoc/>
        [Params(
            "p?th/**/a[bcd]b[e-g]a[1-4]*[!wxyz][!a-c][!1-3].txt",
            "p?th/a[e-g].txt",
            "p?th/a[bcd]b[e-g].txt",
            "p?th/a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].txt")]
        public override string? Pattern
        {
            get
            {
                return this.pattern;
            }

            set
            {
                this.pattern = value;
                this.patternLength = value?.Length;
                System.Collections.Generic.IList<DotNet.Globbing.Token.IGlobToken>? tokens = new GlobTokeniser().Tokenise(this.pattern);
                this.regexString = new GlobToRegexFormatter().Format(tokens);
            }
        }

        /// <summary>
        /// Compile a regex.
        /// </summary>
        /// <returns>The generated <see cref="Regex"/>.</returns>
        [Benchmark(Baseline = true)]
        public Regex New_Compiled_Regex_Glob()
        {
            var result = new Regex(this.regexString!, RegexOptions.Compiled | RegexOptions.Singleline);
            return result;
        }

        /// <summary>
        /// Compile a Dotnet.Glob glob.
        /// </summary>
        /// <returns>The globbing glob.</returns>
        [Benchmark]
        public DotNet.Globbing.Glob New_DotNet_Glob()
        {
            var result = DotNet.Globbing.Glob.Parse(this.Pattern);
            return result;
        }

        /// <summary>
        /// Parse a Corvus.Globbing glob.
        /// </summary>
        /// <returns>The number of tokens generated.</returns>
        [Benchmark]
        public int New_Corvus_Glob()
        {
            Span<Corvus.Globbing.GlobToken> tokenizedGlob = stackalloc Corvus.Globbing.GlobToken[this.patternLength!.Value];
            return Corvus.Globbing.GlobTokenizer.Tokenize(this.Pattern, ref tokenizedGlob);
        }
    }
}
