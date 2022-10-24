// <copyright file="BaselineRegexIsMatchFalseBenchmarks.cs" company="Endjin Limited">
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
    /// Match failures.
    /// </summary>
    [MemoryDiagnoser]
    public class BaselineRegexIsMatchFalseBenchmarks : BaseGlobBenchMark
    {
        private const int MaxResults = 10000;

        private string? pattern;
        private Regex? compiledRegex;
        private DotNet.Globbing.Glob? dotnetGlob;
        private int? patternLength;
        private GlobToken[]? corvusGlob;

        /// <summary>
        /// Gets or sets the number of matches to perform.
        /// </summary>
        [Params(MaxResults)]
        public int NumberOfMatches { get; set; }

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
                this.patternLength = value!.Length;
                this.dotnetGlob = DotNet.Globbing.Glob.Parse(this.pattern);
                this.compiledRegex = this.CreateRegex(this.dotnetGlob.Tokens, true);

                Span<Corvus.Globbing.GlobToken> tokenizedGlob = stackalloc Corvus.Globbing.GlobToken[this.patternLength!.Value];
                int tokenCount = Corvus.Globbing.GlobTokenizer.Tokenize(this.Pattern, tokenizedGlob);
                this.corvusGlob = tokenizedGlob[..tokenCount].ToArray();
                this.InitialiseGlobTestData(value!, 0, MaxResults);
            }
        }

        /// <summary>
        /// Benchmark against a pre-compiled RegEx.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark]
        public bool Compiled_Regex_IsMatch()
        {
            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= this.compiledRegex!.IsMatch(testString);
            }

            return result;
        }

        /// <summary>
        /// Benchmark against a pre-compiled dotnet glob.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark]
        public bool DotNetGlob_IsMatch()
        {
            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= this.dotnetGlob!.IsMatch(testString);
            }

            return result;
        }

        /// <summary>
        /// Benchmark against a Corvus.Globbing glob.
        /// </summary>
        /// <returns>True if it is a match.</returns>
        [Benchmark(Baseline = true)]
        public bool CorvusGlob_IsMatch()
        {
            bool result = false;
            for (int i = 0; i < this.NumberOfMatches; i++)
            {
                string? testString = this.TestStrings![i];
                result ^= Corvus.Globbing.Glob.Match(this.Pattern, this.corvusGlob, testString!);
            }

            return result;
        }
    }
}
