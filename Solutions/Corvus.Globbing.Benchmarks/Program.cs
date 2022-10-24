// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing.Benchmarks
{
    using BenchmarkDotNet.Running;

    /// <summary>
    /// Dotnet benchmark runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Program arguments.</param>
        public static void Main(string[] args)
        {
            ////BenchmarkRunner.Run<BaselineRegexGlobCompileBenchmarks>();
            ////BenchmarkRunner.Run<BaselineRegexCompileAndMatchTrueBenchmarks>();
            ////BenchmarkRunner.Run<BaselineRegexCompileAndMatchFalseBenchmarks>();
            ////BenchmarkRunner.Run<BaselineRegexIsMatchTrueBenchmarks>`();
            BenchmarkRunner.Run<BaselineRegexIsMatchFalseBenchmarks>();
        }
    }
}
