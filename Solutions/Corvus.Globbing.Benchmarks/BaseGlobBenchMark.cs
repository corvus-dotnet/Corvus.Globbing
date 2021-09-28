// <copyright file="BaseGlobBenchMark.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>
// <license>
// Derived from code https://github.com/dazinator/DotNet.Glob/blob/develop/LICENSE
// </license>

namespace Corvus.Globbing.Benchmarks
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Corvus.Globbing.Benchmarks.Utils;
    using DotNet.Globbing;
    using DotNet.Globbing.Generation;
    using DotNet.Globbing.Token;

    /// <summary>
    /// Base class for glob benchmarks.
    /// </summary>
    public abstract class BaseGlobBenchMark
    {
        private readonly Dictionary<string, List<string>> testDataSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGlobBenchMark"/> class.
        /// </summary>
        protected BaseGlobBenchMark()
        {
            this.testDataSet = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Gets or sets the glob pattern for the benchmark.
        /// </summary>
        public abstract string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets the test strings for the benchmark.
        /// </summary>
        public List<string>? TestStrings { get; set; }

        /// <summary>
        /// Initializes the glob test data.
        /// </summary>
        /// <param name="globPattern">The glob pattern to use.</param>
        /// <param name="numberOfMatchingStrings">The number of matching strings to generate.</param>
        /// <param name="numberOfNonMatchingStrings">The number of non-matching strings to generate.</param>
        protected void InitialiseGlobTestData(string globPattern, int numberOfMatchingStrings, int numberOfNonMatchingStrings)
        {
            if (!this.testDataSet.ContainsKey(globPattern))
            {
                // generate test data.
                IList<IGlobToken>? tokens = new GlobTokeniser().Tokenise(globPattern);
                var generator = new GlobMatchStringGenerator(tokens);

                int total = numberOfMatchingStrings + numberOfNonMatchingStrings;
                var testData = new List<string>(total);

                for (int i = 0; i < numberOfMatchingStrings; i++)
                {
                    testData.Add(generator.GenerateRandomNonMatch());
                }

                for (int i = 0; i < numberOfNonMatchingStrings; i++)
                {
                    testData.Add(generator.GenerateRandomNonMatch());
                }

                this.testDataSet.Add(globPattern, testData);
            }

            this.TestStrings = this.testDataSet[globPattern];
        }

        /// <summary>
        /// Creates a <see cref="Regex"/> for the given glob pattern.
        /// </summary>
        /// <param name="globPattern">The glob pattern for which to create a <see cref="Regex"/>.</param>
        /// <param name="compiled">Whether to compile the <see cref="Regex"/>.</param>
        /// <returns>The <see cref="Regex"/> for the glob pattern.</returns>
        protected Regex CreateRegex(string globPattern, bool compiled)
        {
            IList<IGlobToken>? tokens = new GlobTokeniser().Tokenise(globPattern);
            return this.CreateRegex(tokens, compiled);
        }

        /// <summary>
        /// Creates a <see cref="Regex"/> for a list of <see cref="IGlobToken"/>.
        /// </summary>
        /// <param name="tokens">The glob tokens for which to create a regex.</param>
        /// <param name="compiled">Whether to compile the <see cref="Regex"/>.</param>
        /// <returns>The <see cref="Regex"/> for the glob tokens.</returns>
        protected Regex CreateRegex(IList<IGlobToken> tokens, bool compiled)
        {
            var regexFormatter = new GlobToRegexFormatter();
            string? regexString = regexFormatter.Format(tokens);
            var regex = new Regex(regexString, compiled ? RegexOptions.Compiled | RegexOptions.Singleline : RegexOptions.Singleline);
            return regex;
        }

        /// <summary>
        /// Creates a <see cref="Regex"/> for the given glob pattern.
        /// </summary>
        /// <param name="globPattern">The glob pattern for which to create a <see cref="Regex"/>.</param>
        /// <returns>The <see cref="Regex"/> for the glob pattern.</returns>
        protected string CreateRegexString(string globPattern)
        {
            IList<IGlobToken>? tokens = new GlobTokeniser().Tokenise(globPattern);
            return this.CreateRegexString(tokens);
        }

        /// <summary>
        /// Creates a <see cref="Regex"/> for a list of <see cref="IGlobToken"/>.
        /// </summary>
        /// <param name="tokens">The glob tokens for which to create a regex.</param>
        /// <returns>The <see cref="Regex"/> for the glob tokens.</returns>
        protected string CreateRegexString(IList<IGlobToken> tokens)
        {
            var regexFormatter = new GlobToRegexFormatter();
            return regexFormatter.Format(tokens);
        }
    }
}