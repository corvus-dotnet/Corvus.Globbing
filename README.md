# Corvus.Globbing
[![Build Status](https://dev.azure.com/endjin-labs/Corvus.Globbing/_apis/build/status/corvus-dotnet.Corvus.Globbing?branchName=main)](https://dev.azure.com/endjin-labs/Corvus.Globbing/_build/latest?definitionId=4&branchName=main)
[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/corvus-dotnet/Corvus.Globbing/main/LICENSE)
[![IMM](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)

A zero allocation globbing library.

## Repository Structure 

- `Solutions` - Contains source code files, benchmarks and specs.
- `Documentation` - Contains documentation, including polyglot notebooks containing code examples inside the `Examples` subfolder.

## Getting Started 

`Corvus.Globbing` is available on [NuGet](https://www.nuget.org/packages/Corvus.Globbing). To add a reference to the package in your project, run the following command
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
|                  Method |              Pattern |         Mean |      Error |    StdDev |  Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [50]** | **13,877.19 ns** |  **81.928 ns** | **76.636 ns** | **111.47** |    **0.68** | **1.2512** | **0.0763** |   **21176 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [50] |  1,303.24 ns |   9.116 ns |  8.527 ns |  10.47 |    0.07 | 0.1678 | 0.0019 |    2816 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [50] |    124.40 ns |   0.353 ns |  0.313 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |           |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** |  **8,345.30 ns** |  **37.882 ns** | **31.633 ns** | **136.11** |    **0.59** | **0.8392** | **0.0305** |   **14288 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |    807.77 ns |   6.698 ns |  5.938 ns |  13.18 |    0.10 | 0.1135 |      - |    1904 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [21] |     61.26 ns |   0.364 ns |  0.341 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |           |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **10,036.70 ns** | **100.789 ns** | **94.278 ns** |  **88.88** |    **0.87** | **1.0223** | **0.0458** |   **17336 B** |          **NA** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,102.24 ns |   5.694 ns |  4.755 ns |   9.76 |    0.04 | 0.1411 |      - |    2376 B |          NA |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    112.92 ns |   0.281 ns |  0.263 ns |   1.00 |    0.00 |      - |      - |         - |          NA |
|                         |                      |              |            |           |        |         |        |        |           |             |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** |  **7,218.57 ns** |  **62.485 ns** | **58.448 ns** | **160.61** |    **1.79** | **0.7782** | **0.0305** |   **13064 B** |          **NA** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |    613.90 ns |   6.127 ns |  5.431 ns |  13.66 |    0.11 | 0.0830 |      - |    1392 B |          NA |
|         New_Corvus_Glob |      p?th/a[e-g].txt |     44.95 ns |   0.233 ns |  0.218 ns |   1.00 |    0.00 |      - |      - |         - |          NA |


### Compile and match false
|                 Method | NumberOfMatches |              Pattern |          Mean |        Error |       StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |--------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** |  **75,896.72 ns** |   **482.550 ns** |   **427.768 ns** | **1,098.17** |    **6.25** | **1.2207** | **0.6104** |   **22284 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     813.62 ns |     6.044 ns |     5.654 ns |    11.80 |    0.08 | 0.1135 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |      69.05 ns |     0.437 ns |     0.365 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** |  **84,555.94 ns** |   **528.328 ns** |   **468.349 ns** |   **694.16** |    **4.79** | **1.7090** | **0.8545** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,127.42 ns |     7.842 ns |     7.336 ns |     9.26 |    0.06 | 0.1411 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     121.79 ns |     0.320 ns |     0.299 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **74,288.45 ns** |   **499.135 ns** |   **466.891 ns** | **1,425.78** |   **11.63** | **1.0986** | **0.4883** |   **19477 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     597.17 ns |     3.189 ns |     2.827 ns |    11.46 |    0.08 | 0.0830 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |      52.10 ns |     0.215 ns |     0.201 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **195,131.76 ns** |   **433.322 ns** |   **384.129 ns** |     **4.50** |    **0.07** | **1.2207** | **0.4883** |   **22284 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  66,112.97 ns | 1,296.349 ns | 1,387.078 ns |     1.53 |    0.04 |      - |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  43,345.40 ns |   802.764 ns |   711.630 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **206,238.39 ns** | **1,227.304 ns** | **1,087.973 ns** |     **4.81** |    **0.02** | **1.7090** | **0.7324** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  66,828.16 ns | 1,318.026 ns | 1,759.527 ns |     1.56 |    0.04 | 0.1221 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  42,863.40 ns |   141.561 ns |   125.491 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **194,019.89 ns** |   **846.170 ns** |   **791.508 ns** |     **4.54** |    **0.03** | **0.9766** | **0.4883** |   **19469 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  63,989.33 ns |   428.623 ns |   334.641 ns |     1.50 |    0.01 |      - |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  42,718.38 ns |   301.756 ns |   267.499 ns |     1.00 |    0.00 |      - |      - |         - |          NA |


### Compile and match true
|                 Method | NumberOfMatches |              Pattern |          Mean |        Error |       StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |--------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** |  **77,030.97 ns** | **1,093.429 ns** | **1,022.794 ns** | **1,109.15** |   **15.59** | **1.2207** | **0.6104** |   **22284 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     786.76 ns |     8.713 ns |     8.150 ns |    11.33 |    0.12 | 0.1135 |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |      69.45 ns |     0.372 ns |     0.348 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** |  **84,950.14 ns** | **1,110.923 ns** |   **984.804 ns** |   **694.92** |    **7.58** | **1.7090** | **0.8545** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,130.98 ns |     6.183 ns |     5.784 ns |     9.25 |    0.05 | 0.1411 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     122.24 ns |     0.332 ns |     0.294 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **74,415.53 ns** |   **319.384 ns** |   **298.752 ns** | **1,432.32** |   **10.49** | **1.0986** | **0.4883** |   **19477 B** |          **NA** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     629.92 ns |     7.227 ns |     6.760 ns |    12.12 |    0.17 | 0.0830 |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |      51.96 ns |     0.304 ns |     0.285 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **199,287.12 ns** |   **590.785 ns** |   **552.620 ns** |     **4.51** |    **0.06** | **1.2207** | **0.4883** |   **22276 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  63,911.88 ns |   353.536 ns |   313.400 ns |     1.45 |    0.02 |      - |      - |    1904 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  44,162.74 ns |   642.971 ns |   569.977 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **202,581.92 ns** |   **689.381 ns** |   **644.847 ns** |     **4.74** |    **0.02** | **1.7090** | **0.7324** |   **29175 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  67,650.85 ns |   774.893 ns |   724.835 ns |     1.58 |    0.02 | 0.1221 |      - |    2376 B |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  42,734.74 ns |   137.297 ns |   107.193 ns |     1.00 |    0.00 |      - |      - |         - |          NA |
|                        |                 |                      |               |              |              |          |         |        |        |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **195,077.89 ns** |   **450.167 ns** |   **399.061 ns** |     **4.58** |    **0.02** | **0.9766** | **0.4883** |   **19469 B** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  64,242.37 ns |   827.481 ns |   733.541 ns |     1.51 |    0.02 |      - |      - |    1392 B |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  42,597.05 ns |   232.092 ns |   193.807 ns |     1.00 |    0.00 |      - |      - |         - |          NA |


### Match false
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |----------:|---------:|---------:|----------:|------:|--------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **279.37 μs** | **1.137 μs** | **1.008 μs** | **279.38 μs** |  **5.94** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  62.83 μs | 0.223 μs | 0.209 μs |  62.81 μs |  1.34 |    0.01 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  47.00 μs | 0.208 μs | 0.184 μs |  46.97 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **210.16 μs** | **1.505 μs** | **1.408 μs** | **209.75 μs** |  **4.50** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  68.52 μs | 1.357 μs | 1.902 μs |  68.80 μs |  1.45 |    0.04 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  46.70 μs | 0.132 μs | 0.117 μs |  46.67 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **213.06 μs** | **0.647 μs** | **0.574 μs** | **213.27 μs** |  **4.07** |    **0.28** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  66.41 μs | 1.280 μs | 1.665 μs |  65.70 μs |  1.26 |    0.09 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  51.67 μs | 1.140 μs | 3.363 μs |  49.30 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |           |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **207.46 μs** | **0.609 μs** | **0.570 μs** | **207.43 μs** |  **4.44** |    **0.01** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  68.23 μs | 1.326 μs | 1.579 μs |  67.37 μs |  1.46 |    0.03 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  46.77 μs | 0.098 μs | 0.082 μs |  46.79 μs |  1.00 |    0.00 |         - |          NA |

### Match true
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |   StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------------- |--------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **279.32 μs** | **1.830 μs** | **1.712 μs** |  **6.23** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  63.30 μs | 0.241 μs | 0.225 μs |  1.41 |    0.00 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  44.85 μs | 0.137 μs | 0.114 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **209.55 μs** | **0.983 μs** | **0.821 μs** |  **4.67** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  66.46 μs | 1.294 μs | 1.490 μs |  1.48 |    0.04 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  44.87 μs | 0.148 μs | 0.131 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **212.46 μs** | **1.412 μs** | **1.321 μs** |  **4.73** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  68.88 μs | 0.537 μs | 0.502 μs |  1.53 |    0.01 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  44.90 μs | 0.173 μs | 0.135 μs |  1.00 |    0.00 |         - |          NA |
|                        |                 |                      |           |          |          |       |         |           |             |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **206.77 μs** | **1.097 μs** | **1.026 μs** |  **4.64** |    **0.03** |         **-** |          **NA** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  66.17 μs | 0.124 μs | 0.097 μs |  1.49 |    0.00 |         - |          NA |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  44.56 μs | 0.115 μs | 0.107 μs |  1.00 |    0.00 |         - |          NA |

# Credits

This library would not exist without [Dotnet Glob](https://github.com/dazinator/DotNet.Glob) - I've built the specs from its unit tests, and modelled the actual matching algorithms on the implementation in that library (although it is somewhat different in structure). The project has a "give back" mechanism so I've donated a small amount. You could consider doing so too.
