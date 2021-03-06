# Corvus.Globbing
[![Build Status](https://dev.azure.com/endjin-labs/Corvus.Globbing/_apis/build/status/corvus-dotnet.Corvus.Globbing?branchName=main)](https://dev.azure.com/endjin-labs/Corvus.Globbing/_build/latest?definitionId=4&branchName=main)
[![GitHub license](https://img.shields.io/badge/License-Apache%202-blue.svg)](https://raw.githubusercontent.com/corvus-dotnet/Corvus.Globbing/main/LICENSE)
[![IMM](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)](https://endimmfuncdev.azurewebsites.net/api/imm/github/corvus-dotnet/Corvus.Globbing/total?cache=false)

A zero allocation globbing library.

## Purpose

We built this to provide a zero-allocation globbing library with performance comparable to (or better than) https://github.com/dazinator/DotNet.Glob and raw Regular Expressions, when running under dotnet50.

## Use cases

The particular use case we have optimized for is a case-sensitive glob that will be matched against a large number of paths (100+), but will not necessarily be cachable.

We want a very high-performance parse of that glob, and then a performant application of the glob to a large number of candidate paths.

Our motivation for this came when "link stripping" documents to be returned from an HTTP request, to remove links that the requesting identity is not permitted to see
(perhaps for security, feature enablement, or just local context).

We also want to minimize allocations on the hot-path of a request handler. 

### Performance targets
We offer better raw matching performance (~10-30%) against a pre-tokenized glob pattern, for the `StringComparison.Ordinal` (case sensitive) default, than [Dotnet.Glob](https://github.com/dazinator/DotNet.Glob).

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
ReadOnlySpan<GlobToken> glob = tokenizedGlob.Slice(0, tokenCount);

bool firstMatch = Glob.Match(pattern, glob, "path/fooatstand");
bool secondMatch = Glob.Match(pattern, glob, "badpath/fooatstand");
```

For very long potential globs, you could fall back to the `ArrayPool` allocation technique:

```csharp
// Pick a token array length threshold
int MaxGlobTokenArrayLength = 1024;

string pattern = "path/*atstand";

// There can't be more tokens than there are characters the glob.
GlobToken[] globTokenArray = Array.Empty<GlobToken>();
Span<GlobToken> globTokens = stackalloc GlobToken[0];

if (pattern.Length > MaxGlobTokenArrayLength)
{
    globTokenArray = ArrayPool<GlobToken>.Shared.Rent(pattern.Length);
    globTokens = globTokenArray.AsSpan();
}
else
{
    globTokens = stackalloc GlobToken[pattern.Length];
}

try
{
    int tokenCount = GlobTokenizer.Tokenize(pattern, ref globTokens);
    ReadOnlySpan<GlobToken> tokenizedGlob = globTokens.Slice(0, tokenCount);

    // Do your matching here...
    bool firstMatch = Glob.Match(pattern, tokenizedGlob, "path/fooatstand");
    bool secondMatch = Glob.Match(pattern, tokenizedGlob, "badpath/fooatstand");
}
finally
{
    if (pattern.Length > MaxGlobTokenArrayLength)
    {
        ArrayPool<GlobToken>.Shared.Return(globTokenArray);
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
|                  Method |              Pattern |         Mean |      Error |     StdDev |  Ratio | RatioSD |  Gen 0 |  Gen 1 | Allocated |
|------------------------ |--------------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [50]** | **24,518.93 ns** | **446.904 ns** | **418.035 ns** | **145.92** |    **2.71** | **5.0659** |      **-** |  **21,272 B** |
|         New_DotNet_Glob | p?th/(...)].txt [50] |  1,614.43 ns |  24.121 ns |  22.563 ns |   9.61 |    0.18 | 0.6733 |      - |   2,816 B |
|         New_Corvus_Glob | p?th/(...)].txt [50] |    168.04 ns |   1.866 ns |   1.746 ns |   1.00 |    0.00 |      - |      - |         - |
|                         |                      |              |            |            |        |         |        |        |           |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** | **13,920.81 ns** | **150.097 ns** | **133.057 ns** | **159.84** |    **2.16** | **3.4332** |      **-** |  **14,392 B** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |    900.49 ns |  12.604 ns |  11.790 ns |  10.34 |    0.17 | 0.4549 |      - |   1,904 B |
|         New_Corvus_Glob | p?th/(...)].txt [21] |     87.10 ns |   0.790 ns |   0.700 ns |   1.00 |    0.00 |      - |      - |         - |
|                         |                      |              |            |            |        |         |        |        |           |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **17,286.88 ns** | **183.742 ns** | **171.873 ns** | **112.94** |    **1.72** | **4.1504** |      **-** |  **17,432 B** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,375.73 ns |  10.866 ns |  10.165 ns |   8.99 |    0.11 | 0.5665 |      - |   2,376 B |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    153.08 ns |   1.539 ns |   1.439 ns |   1.00 |    0.00 |      - |      - |         - |
|                         |                      |              |            |            |        |         |        |        |           |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** | **12,350.67 ns** | **206.552 ns** | **193.209 ns** | **188.56** |    **6.84** | **3.1433** | **0.0153** |  **13,168 B** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |    726.14 ns |   8.640 ns |   7.659 ns |  11.08 |    0.27 | 0.3328 |      - |   1,392 B |
|         New_Corvus_Glob |      p?th/a[e-g].txt |     65.32 ns |   1.307 ns |   1.605 ns |   1.00 |    0.00 |      - |      - |         - |


### Compile and match false
|                 Method | NumberOfMatches |              Pattern |          Mean |        Error |       StdDev |    Ratio | RatioSD |  Gen 0 |  Gen 1 | Allocated |
|----------------------- |---------------- |--------------------- |--------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **102,434.95 ns** | **1,962.576 ns** | **3,734.006 ns** | **1,129.38** |   **51.15** | **5.3711** | **2.6855** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     954.59 ns |    17.737 ns |    16.591 ns |    10.41 |    0.27 | 0.4549 |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |      91.73 ns |     1.822 ns |     1.521 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **111,423.68 ns** |   **447.546 ns** |   **396.738 ns** |   **703.52** |   **13.52** | **6.9580** | **3.4180** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,404.32 ns |    21.916 ns |    20.500 ns |     8.86 |    0.24 | 0.5665 |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     158.77 ns |     3.049 ns |     2.994 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **95,250.25 ns** | **1,534.617 ns** | **1,360.398 ns** | **1,310.39** |   **24.75** | **4.6387** | **2.3193** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     733.96 ns |     9.342 ns |     8.738 ns |    10.11 |    0.12 | 0.3328 |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |      72.70 ns |     0.659 ns |     0.584 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **294,937.84 ns** | **3,379.605 ns** | **2,995.932 ns** |     **3.30** |    **0.04** | **5.3711** | **2.9297** |  **22,648 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 115,916.75 ns | 1,218.873 ns | 1,080.499 ns |     1.30 |    0.02 | 0.3662 |      - |   1,904 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  89,329.52 ns |   829.171 ns |   775.607 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **306,961.22 ns** | **4,678.470 ns** | **4,376.244 ns** |     **3.34** |    **0.07** | **6.8359** | **3.4180** |  **29,582 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 119,904.90 ns | 1,252.195 ns | 1,171.304 ns |     1.30 |    0.02 | 0.4883 |      - |   2,376 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  91,902.09 ns | 1,427.773 ns | 1,335.540 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **289,016.17 ns** | **4,305.232 ns** | **4,027.117 ns** |     **3.16** |    **0.04** | **4.3945** | **1.9531** |  **19,788 B** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 120,199.71 ns | 1,357.718 ns | 1,270.010 ns |     1.31 |    0.02 | 0.2441 |      - |   1,392 B |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  91,516.00 ns |   768.001 ns |   641.316 ns |     1.00 |    0.00 |      - |      - |         - |



### Compile and match true
|                 Method | NumberOfMatches |              Pattern |          Mean |        Error |       StdDev |    Ratio | RatioSD |  Gen 0 |  Gen 1 | Allocated |
|----------------------- |---------------- |--------------------- |--------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **101,632.25 ns** | **1,067.540 ns** |   **891.444 ns** | **1,132.45** |   **11.15** | **5.3711** | **2.6855** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     945.00 ns |     8.850 ns |     7.846 ns |    10.53 |    0.09 | 0.4539 |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |      89.75 ns |     0.389 ns |     0.325 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **110,443.78 ns** |   **847.213 ns** |   **792.483 ns** |   **710.63** |    **7.04** | **6.9580** | **3.4180** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,371.05 ns |    12.836 ns |    12.007 ns |     8.84 |    0.08 | 0.5665 |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     155.31 ns |     0.988 ns |     0.825 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **95,855.67 ns** |   **525.585 ns** |   **465.917 ns** | **1,310.64** |   **11.98** | **4.6387** | **2.3193** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     713.67 ns |     7.240 ns |     6.773 ns |     9.76 |    0.13 | 0.3328 |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |      73.10 ns |     0.600 ns |     0.561 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **292,953.87 ns** | **3,174.962 ns** | **2,969.862 ns** |     **3.30** |    **0.04** | **5.3711** | **2.9297** |  **22,648 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 117,235.79 ns | 1,106.554 ns |   924.023 ns |     1.32 |    0.02 | 0.3662 |      - |   1,904 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  88,815.76 ns |   957.988 ns |   896.103 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **305,527.82 ns** | **4,855.610 ns** | **3,790.941 ns** |     **3.34** |    **0.06** | **6.8359** | **3.4180** |  **29,582 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 118,614.04 ns | 1,087.238 ns | 1,017.003 ns |     1.29 |    0.01 | 0.4883 |      - |   2,376 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  91,627.21 ns |   884.869 ns |   827.707 ns |     1.00 |    0.00 |      - |      - |         - |
|                        |                 |                      |               |              |              |          |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **291,808.94 ns** | **4,918.313 ns** | **3,839.895 ns** |     **3.15** |    **0.06** | **4.3945** | **1.9531** |  **19,788 B** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 121,114.24 ns | 1,232.934 ns | 1,092.964 ns |     1.31 |    0.03 | 0.2441 |      - |   1,392 B |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  92,326.72 ns | 1,832.623 ns | 1,714.237 ns |     1.00 |    0.00 |      - |      - |         - |


### Match false
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |   StdDev | Ratio | RatioSD | Allocated |
|----------------------- |---------------- |--------------------- |----------:|---------:|---------:|------:|--------:|----------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **446.86 ??s** | **4.783 ??s** | **3.994 ??s** |  **4.74** |    **0.06** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] | 101.13 ??s | 1.021 ??s | 0.955 ??s |  1.07 |    0.01 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  94.26 ??s | 1.052 ??s | 0.932 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **357.73 ??s** | **1.617 ??s** | **1.433 ??s** |  **3.74** |    **0.03** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 120.99 ??s | 1.675 ??s | 1.566 ??s |  1.27 |    0.02 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  95.53 ??s | 0.648 ??s | 0.606 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **386.72 ??s** | **4.710 ??s** | **4.175 ??s** |  **3.92** |    **0.04** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 121.05 ??s | 1.141 ??s | 0.952 ??s |  1.23 |    0.01 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  98.54 ??s | 0.614 ??s | 0.574 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **369.37 ??s** | **1.664 ??s** | **1.475 ??s** |  **3.88** |    **0.02** |         **-** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 116.95 ??s | 1.689 ??s | 1.659 ??s |  1.23 |    0.02 |         - |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  95.02 ??s | 0.457 ??s | 0.357 ??s |  1.00 |    0.00 |         - |

### Match true
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |   StdDev | Ratio | RatioSD | Allocated |
|----------------------- |---------------- |--------------------- |----------:|---------:|---------:|------:|--------:|----------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [50]** | **450.13 ??s** | **2.709 ??s** | **2.402 ??s** |  **4.88** |    **0.08** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  99.80 ??s | 1.215 ??s | 1.136 ??s |  1.08 |    0.02 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [50] |  92.14 ??s | 1.372 ??s | 1.283 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **345.40 ??s** | **1.602 ??s** | **1.499 ??s** |  **3.78** |    **0.03** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 115.90 ??s | 0.647 ??s | 0.606 ??s |  1.27 |    0.01 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] |  91.29 ??s | 0.616 ??s | 0.546 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **374.15 ??s** | **6.377 ??s** | **5.965 ??s** |  **4.06** |    **0.09** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 121.76 ??s | 1.353 ??s | 1.200 ??s |  1.32 |    0.02 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] |  92.13 ??s | 1.152 ??s | 1.078 ??s |  1.00 |    0.00 |         - |
|                        |                 |                      |           |          |          |       |         |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **354.97 ??s** | **2.833 ??s** | **2.511 ??s** |  **3.88** |    **0.04** |         **-** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 115.02 ??s | 1.082 ??s | 1.012 ??s |  1.26 |    0.01 |         - |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  91.42 ??s | 0.766 ??s | 0.679 ??s |  1.00 |    0.00 |         - |

# Credits

This library would not exist without [Dotnet Glob](https://github.com/dazinator/DotNet.Glob) - I've built the specs from its unit tests, and modelled the actual matching algorithms on the implementation in that library (although it is somewhat different in structure). The project has a "give back" mechanism so I've donated a small amount. You could consider doing so too.
