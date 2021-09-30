// <copyright file="BaselineRegexCompileAndMatchFalseBenchmarks.cs" company="Endjin Limited">
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

    /// <summary>
    /// Match successes.
    /// </summary>
    [MemoryDiagnoser]
    public class BaselineRegexCompileAndMatchFalseBenchmarks : BaseGlobBenchMark
    {
        private const int MaxResults = 10000;

        private string? pattern;
        private int? patternLength;
        private string? regexString;

        /// <summary>
        /// Gets or sets the number of matches to apply.
        /// </summary>
        [Params(1, MaxResults)]
        public int NumberOfMatches { get; set; }

        /// <inheritdoc/>
        [Params(
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
                this.regexString = this.CreateRegexString(this.pattern!);
                this.InitialiseGlobTestData(value!, 0, MaxResults);
            }
        }

        /// <summary>
        /// Benchmark against the compiled RegEx.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark(Baseline = true)]
        public bool Compiled_Regex_IsMatch()
        {
            Regex? compiledRegex = this.CreateRegex(this.regexString!, true);

            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= compiledRegex!.IsMatch(testString);
            }

            return result;
        }

        /// <summary>
        /// Benchmark against the Dotnet.Glob.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark]
        public bool DotNetGlob_IsMatch()
        {
            var dotnetGlob = DotNet.Globbing.Glob.Parse(this.pattern);

            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= dotnetGlob!.IsMatch(testString);
            }

            return result;
        }

        /// <summary>
        /// Benchmark against an on-the-fly parsed Corvus.Globbing glob.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark]
        public bool CorvusGlob_IsMatch()
        {
            Span<Corvus.Globbing.GlobToken> tokenizedGlob = stackalloc Corvus.Globbing.GlobToken[this.patternLength!.Value];
            int tokenCount = Corvus.Globbing.GlobTokenizer.Tokenize(this.Pattern, ref tokenizedGlob);
            ReadOnlySpan<GlobToken> glob = tokenizedGlob.Slice(0, tokenCount);

            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= Corvus.Globbing.Glob.Match(this.Pattern, glob, testString!);
            }

            return result;
        }
    }
}
