﻿// <copyright file="Program.cs" company="Endjin Limited">
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
        public static void Main()
        {
            BenchmarkRunner.Run<BaselineRegexGlobCompileBenchmarks>();
            BenchmarkRunner.Run<BaselineRegexCompileAndMatchFalseBenchmarks>();
            BenchmarkRunner.Run<BaselineRegexCompileAndMatchTrueBenchmarks>();
            BenchmarkRunner.Run<BaselineRegexIsMatchFalseBenchmarks>();
            BenchmarkRunner.Run<BaselineRegexIsMatchTrueBenchmarks>();
        }
    }
}
