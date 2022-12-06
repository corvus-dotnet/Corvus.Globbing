# Corvus.Globbing
[![Build Status](https://dev.azure.com/endjin-labs/Corvus.Globbing/_apis/build/status/corvus-dotnet.Corvus.Globbing?branchName=main)](https://dev.azure.com/endjin-labs/Corvus.Globbing/_build/latest?definitionId=4&branchName=main)
[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/corvus-dotnet/Corvus.Globbing/main/LICENSE)
[![IMM](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)

A zero allocation globbing library.

## Repository Structure 

- `Solutions` - Contains source code files, benchmarks and specs.
- `Documentation` - Contains documentation, including polyglot notebooks containing code examples inside the `Examples` subfolder.

## Getting Started 

`Corvus.Globbing` is available on [NuGet](https://www.nuget.org/packages/Corvus.Extensions). To add a reference to the package in your project, run the following command
```
dotnet add package Corvus.Globbing
```

Use the --version option to specify a [version](https://www.nuget.org/packages/Corvus.Globbing#versions-tab) to install.
```
dotnet add package Corvus.Globbing --version 0.1.0
```

## Purpose

We built this to provide a zero-allocation globbing library with performance comparable to (or better than) https://github.com/dazinator/DotNet.Glob and raw Regular Expressions, when running under net6.0.

## Use cases

The particular use case we have optimized for is a case-sensitive glob that will be matched against a large number of paths (100+), but will not necessarily be cachable.

We want a very high-performance parse of that glob, and then a performant application of the glob to a large number of candidate paths.

Our motivation for this came when "link stripping" documents to be returned from an HTTP request, to remove links that the requesting identity is not permitted to see
(perhaps for security, feature enablement, or just local context).

We also want to minimize allocations on the hot-path of a request handler. 

### Performance targets
We offer better raw matching performance (~50-80%) against a pre-tokenized glob pattern, for the `StringComparison.Ordinal` (case sensitive) default, than [Dotnet.Glob](https://github.com/dazinator/DotNet.Glob).

Our tokenization is also ~10x faster than [Dotnet.Glob](https://github.com/dazinator/DotNet.Glob) so it is significantly faster in the single use/throwaway case.

## Usage

The simplest case just requires you to pass the glob and the candidate path to match. Note that it actually takes a `ReadonlySpan<char>` - to which strings are implicitly converted. 

```csharp
bool isMatch = Glob.Match("path/*atstand", "path/fooatstand"); 
```

We support all the standard string-comparison types. The default is `StringComparison.Ordinal` (i.e. case sensitive), but you could do a case insensitive match:

```csharp
bool isMatch = Glob.Match("path/*atstand", "PATH/fooatstand", StringComparison.OrdinalIgnoreCase); 
```

If you want to hold on to the tokenized glob and match against a number of paths, you can stack allocate a tokenized glob array, and reuse it:

```csharp
string pattern = "path/*atstand";

// There can't be more tokens than there are characters in the glob pattern, so we allocate an array at least that long.
Span<GlobToken> tokenizedGlob = stackalloc GlobToken[pattern.Length];
int tokenCount = Corvus.Globbing.GlobTokenizer.Tokenize(pattern, ref tokenizedGlob);
// And then slice off the number of tokens we actually used
ReadOnlySpan<GlobToken> glob = tokenizedGlob[..tokenCount];

bool firstMatch = Glob.Match(pattern, glob, "path/fooatstand"); // Returns: true
bool secondMatch = Glob.Match(pattern, glob, "badpath/fooatstand"); // Returns: false
```

For very long potential globs, you could fall back to the `ArrayPool` allocation technique:

```csharp
    // Pick a token array length threshold
    int MaxGlobTokenArrayLength = 1024;

    string pattern = "path/*atstand";

    // There can't be more tokens than there are characters the glob.
    GlobToken[]? globTokenArray = null;
    Span<GlobToken> globTokens = pattern.Length > MaxGlobTokenArrayLength ? stackalloc GlobToken[0] : stackalloc GlobToken[pattern.Length];

    if (pattern.Length > MaxGlobTokenArrayLength)
    {
        globTokenArray = ArrayPool<GlobToken>.Shared.Rent(pattern.Length);
        globTokens = globTokenArray.AsSpan();
    }

    try
    {
        int tokenCount = GlobTokenizer.Tokenize(pattern, ref globTokens);
        ReadOnlySpan<GlobToken> tokenizedGlob = globTokens[..tokenCount];

        // Do your matching here...
        bool firstMatch = Glob.Match(pattern, tokenizedGlob, "path/fooatstand"); // Returns: true

        bool secondMatch = Glob.Match(pattern, tokenizedGlob, "badpath/fooatstand"); // Returns: false
    }
    finally
    {
        if (pattern.Length > MaxGlobTokenArrayLength)
        {
            ArrayPool<GlobToken>.Shared.Return(globTokenArray);
        }
    }
}
```

## Supported patterns

(Derived from [Wikipedia](https://en.wikipedia.org/wiki/Glob_(programming)#Syntax))

| Wildcard | Description                                                                  | Example     | Matches                                 | Does not match              |
|----------|------------------------------------------------------------------------------|-------------|-----------------------------------------|-----------------------------|
| *        | matches any number of any characters including none                          | Law*        | Law, Laws, or Lawyer                    | GrokLaw, La, Law/foo or aw          |
|          |                                                                              | \*Law\*     | Law, GrokLaw, or Lawyer.                | La, or aw                   |
| **       | matches any number of path segments                                          | \*\*/Law*   | foo/bar/Law, bar/baz/bat/Law, or Law    | Law/foo                     |
| ?        | matches any single character                                                 | ?at         | Cat, cat, Bat or bat                    | at                          |
| [abc]    | matches one character given in the bracket                                   | [CB]at      | Cat or Bat                              | cat, bat or CBat            |
| [a-z]    | matches one character from the (locale-dependent) range given in the bracket | Letter[0-9] | Letter0, Letter1, Letter2 up to Letter9 | Letters, Letter or Letter10 |
| [!abc]   | matches one character that is not given in the bracket                | [!C]at       | Bat, bat, or cat                                         | Cat                                   |
| [!a-z]   | matches one character that is not from the range given in the bracket | Letter[!3-5] | Letter1, Letter2, Letter6 up to Letter9 and Letterx etc. | Letter3, Letter4, Letter5 or Letterxx |

### Escaping special characters using `[]`

The special characters `*`, `?`, `\`, `/`, `[` can be escaped by using the `[]` 'match one character given in the bracket' list - e.g. `[[]` matches the literal `[` and `[*]` matches the literal `*`.

## Benchmarks

We have used Benchmark Dotnet to compare the performance with raw RegEx and DotNet.Glob. This represents a current example run.

_(note that we have re-baselined on the Corvus implementation for ease of comparison)_

### Compile
|                  Method |              Pattern |         Mean |      Error |       StdDev |       Median |  Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-------------:|-----------:|-------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [50]** | **22,711.52 ns** | **312.245 ns** |   **292.075 ns** | **22,722.18 ns** | **116.87** |    **6.76** | **5.0354** | **0.0305** |   **21168 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [50] |  1,714.77 ns |  32.992 ns |    27.550 ns |  1,706.28 ns |   8.72 |    0.48 | 0.6733 |      - |    2816 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [50] |    181.49 ns |   3.243 ns |     8.372 ns |    178.24 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |              |              |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** | **13,542.85 ns** | **268.211 ns** |   **263.420 ns** | **13,516.94 ns** | **149.33** |    **2.71** | **3.3875** |      **-** |   **14280 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |  1,091.12 ns |  15.724 ns |    14.708 ns |  1,093.35 ns |  12.04 |    0.29 | 0.4539 |      - |    1904 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [21] |     90.61 ns |   1.652 ns |     1.464 ns |     90.99 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |              |              |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **16,944.90 ns** | **204.460 ns** |   **170.733 ns** | **16,985.44 ns** |  **53.14** |    **1.90** | **4.1199** |      **-** |   **17328 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,648.61 ns |  29.527 ns |    27.620 ns |  1,639.03 ns |   5.16 |    0.19 | 0.5665 |      - |    2376 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    289.22 ns |   8.345 ns |    24.605 ns |    282.93 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |              |              |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** | **20,880.66 ns** | **417.662 ns** | **1,016.647 ns** | **20,812.23 ns** | **193.99** |   **11.20** | **3.1128** |      **-** |   **13064 B** |          **NA** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |  1,264.50 ns |  24.177 ns |    63.692 ns |  1,258.52 ns |  11.66 |    0.68 | 0.3319 |      - |    1392 B |          NA |
|         New_Corvus_Glob |      p?th/a[e-g].txt |    108.09 ns |   2.200 ns |     4.290 ns |    108.22 ns |   1.00 |    0.00 |      - |      - |         - |          NA |


### Compile and match false
|                 Method | NumberOfMatches |              Pattern |          Mean |        Error |        StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |--------------:|-------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **139,752.46 ns** | **6,424.810 ns** | **18,015.855 ns** | **1,223.35** |   **81.14** | **5.1270** | **2.4414** |   **22286 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |   1,253.16 ns |    20.810 ns |     42.976 ns |    10.37 |    0.46 | 0.4539 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     121.35 ns |     2.445 ns |      3.807 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |               |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **152,524.28 ns** | **3,047.930 ns** |  **7,702.507 ns** |   **679.27** |   **46.46** | **6.8359** | **3.4180** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   2,004.83 ns |    39.688 ns |     89.582 ns |     9.06 |    0.53 | 0.5646 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     222.17 ns |     4.466 ns |      7.823 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |               |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** | **130,492.62 ns** | **2,378.685 ns** |  **5,834.954 ns** | **1,385.44** |   **86.96** | **4.6387** | **2.4414** |   **19480 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     986.72 ns |    19.257 ns |     26.995 ns |    10.41 |    0.30 | 0.3319 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |      95.01 ns |     1.835 ns |      2.320 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |               |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **421,902.04 ns** | **7,706.025 ns** | **17,076.020 ns** |     **5.31** |    **0.30** | **4.8828** | **1.9531** |   **22276 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 148,875.12 ns | 2,911.091 ns |  3,681.598 ns |     1.88 |    0.07 | 0.2441 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  79,654.45 ns | 1,580.265 ns |  3,082.185 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |               |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **422,679.26 ns** | **8,259.183 ns** | **13,570.074 ns** |     **4.94** |    **0.23** | **6.8359** | **3.9063** |   **29168 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 147,765.51 ns | 2,905.293 ns |  3,345.739 ns |     1.73 |    0.08 | 0.4883 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  85,605.19 ns | 1,698.802 ns |  3,106.356 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |               |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **413,981.57 ns** | **8,076.480 ns** | **10,214.161 ns** |     **5.21** |    **0.19** | **4.3945** | **1.9531** |   **19469 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 156,811.84 ns | 3,094.066 ns |  4,437.417 ns |     1.98 |    0.08 | 0.2441 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  79,624.19 ns | 1,567.974 ns |  2,441.147 ns |     1.00 |    0.00 |      - |      - |         - |          NA |


### Compile and match true
|                 Method | NumberOfMatches |              Pattern |         Mean |        Error |       StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |-------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **170,338.6 ns** |  **4,021.46 ns** | **11,210.21 ns** | **1,081.97** |   **98.92** | **4.8828** | **2.4414** |   **22284 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |   1,694.4 ns |     33.54 ns |     80.36 ns |    10.61 |    0.72 | 0.4539 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     157.7 ns |      3.25 ns |      9.59 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |              |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **173,928.9 ns** |  **3,148.74 ns** |  **4,712.89 ns** |   **684.37** |   **33.45** | **6.8359** | **3.4180** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   2,299.5 ns |     45.63 ns |     87.91 ns |     9.08 |    0.47 | 0.5646 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     253.4 ns |      5.11 ns |      9.59 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |              |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** | **152,687.1 ns** |  **2,541.88 ns** |  **3,393.34 ns** | **1,345.74** |   **37.74** | **4.6387** | **2.4414** |   **19480 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |   1,174.1 ns |     23.38 ns |     50.83 ns |    10.36 |    0.52 | 0.3319 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     113.2 ns |      2.25 ns |      3.37 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |              |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **507,652.0 ns** | **11,313.63 ns** | **33,358.49 ns** |     **5.54** |    **0.51** | **4.8828** | **1.9531** |   **22284 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 171,689.9 ns |  3,229.09 ns |  6,596.17 ns |     1.83 |    0.12 | 0.2441 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  92,780.8 ns |  1,852.50 ns |  4,976.62 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |              |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **480,558.4 ns** |  **9,395.33 ns** | **18,101.60 ns** |     **5.35** |    **0.34** | **6.8359** | **3.9063** |   **29168 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 167,014.3 ns |  3,336.65 ns |  7,038.12 ns |     1.86 |    0.13 | 0.4883 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  89,855.2 ns |  1,793.07 ns |  4,465.36 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |              |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **465,242.5 ns** |  **9,234.98 ns** | **14,912.76 ns** |     **5.12** |    **0.33** | **4.3945** | **1.9531** |   **19469 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 165,943.4 ns |  3,316.42 ns |  6,229.05 ns |     1.83 |    0.12 | 0.2441 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  91,033.9 ns |  1,818.06 ns |  4,915.23 ns |     1.00 |    0.00 |      - |      - |         - |          NA |


### Match false
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |    StdDev |    Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |----------:|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **534.24 μs** | **8.627 μs** |  **9.935 μs** | **533.64 μs** |  **7.54** |    **0.24** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] | 107.68 μs | 1.822 μs |  2.433 μs | 107.31 μs |  1.52 |    0.05 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  71.04 μs | 1.417 μs |  1.516 μs |  70.89 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **415.27 μs** | **7.978 μs** |  **8.193 μs** | **413.94 μs** |  **5.97** |    **0.18** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 133.15 μs | 2.443 μs |  4.343 μs | 132.76 μs |  1.90 |    0.05 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  69.69 μs | 1.380 μs |  1.356 μs |  69.74 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **418.49 μs** | **8.300 μs** |  **8.152 μs** | **417.02 μs** |  **6.08** |    **0.16** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 130.19 μs | 2.447 μs |  4.020 μs | 130.19 μs |  1.88 |    0.06 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  68.87 μs | 1.366 μs |  1.341 μs |  68.96 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **430.65 μs** | **8.243 μs** | **22.285 μs** | **424.25 μs** |  **6.22** |    **0.34** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 129.48 μs | 2.493 μs |  3.152 μs | 129.26 μs |  1.84 |    0.05 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  70.64 μs | 1.318 μs |  1.410 μs |  70.65 μs |  1.00 |    0.00 |         - |          NA |

### Match true
|                 Method | NumberOfMatches |              Pattern |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **518.23 μs** | **10.312 μs** | **17.788 μs** | **517.59 μs** |  **7.92** |    **0.31** |       **1 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] | 115.39 μs |  2.303 μs |  4.546 μs | 115.58 μs |  1.72 |    0.08 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  65.53 μs |  0.924 μs |  0.819 μs |  65.57 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |           |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **423.75 μs** |  **6.959 μs** |  **9.048 μs** | **421.94 μs** |  **6.46** |    **0.18** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 144.82 μs |  2.311 μs |  2.162 μs | 144.46 μs |  2.20 |    0.05 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  65.70 μs |  1.108 μs |  1.231 μs |  65.65 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |           |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **428.16 μs** |  **4.422 μs** |  **4.136 μs** | **428.73 μs** |  **6.74** |    **0.10** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 128.13 μs |  2.550 μs |  2.504 μs | 127.75 μs |  2.01 |    0.04 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  63.61 μs |  1.043 μs |  0.924 μs |  63.64 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |           |           |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **414.41 μs** |  **7.874 μs** |  **8.752 μs** | **415.76 μs** |  **6.28** |    **0.25** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 127.07 μs |  2.493 μs |  2.332 μs | 127.28 μs |  1.92 |    0.09 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  76.33 μs |  2.237 μs |  6.560 μs |  78.58 μs |  1.00 |    0.00 |         - |          NA |

# Credits

This library would not exist without [Dotnet Glob](https://github.com/dazinator/DotNet.Glob) - I've built the specs from its unit tests, and modelled the actual matching algorithms on the implementation in that library (although it is somewhat different in structure). The project has a "give back" mechanism so I've donated a small amount. You could consider doing so too.
