# Corvus.Globbing
A zero allocation globbing library

## Purpose

We built this to provide a zero-allocation path globbing library with performance comparable to (or better than) https://github.com/dazinator/DotNet.Glob, or raw RegEx when running under dotnet50.

## Use cases

The particular use case we have optimized for is a glob that has come from an external source (e.g. a database or configuration) that is unlikely to be cacheable due to either volume or transience.

We want a very high-performance parse of that glob, and then a performant application of the glob to a number of candidate paths.

Our motivation for this came when "link stripping" documents to be returned from an HTTP request, to remove links that the requesting identity is not permitted to see
(perhaps for security, feature enablement, or just local context).

We also want to minimize allocations on the hot-path of a request handler. 

## Usage

The simplest case just requires you to pass the glob and the candidate path to match.

```csharp
bool isMatch = Glob.Match("path/*atstand", "path/fooatstand"); 
```

If you want to hold on to the tokenized glob and match against a number of paths, you can stack allocate a tokenized glob array, and reuse it:

```csharp
string pattern = "path/*atstand";

// There can't be more tokens than there are characters the glob pattern, so we allocate an array at least that long.
Span<GlobToken> tokenizedGlob = stackalloc GlobToken[pattern.Length];
int tokenCount = Corvus.Globbing.GlobTokenizer.Tokenize(this.Pattern, ref tokenizedGlob);
// And then slice off the number of tokens we actually used
ReadOnlySpan<GlobToken> glob = tokenizedGlob.Slice(0, tokenCount);

bool firstMatch = Glob.Match(pattern, glob, "path/fooatstand");
bool secondMatch = Glob.Match(pattern, glob, "badpath/fooatstand");
```

For very long potential globs, you could fall back to the `ArrayPool` allocation technique:

```csharp
// Pick a token array length threshold
int MaxGlobTokenArrayLength = 1024;

// There can't be more tokens than there are characters the glob.
GlobToken[] globTokenArray = Array.Empty<GlobToken>();
Span<GlobToken> globTokens = stackalloc GlobToken[0];

if (glob.Length > MaxGlobTokenArrayLength)
{
    globTokenArray = ArrayPool<GlobToken>.Shared.Rent(glob.Length);
    globTokens = globTokenArray.AsSpan();
}
else
{
    globTokens = stackalloc GlobToken[glob.Length];
}

try
{
    int tokenCount = GlobTokenizer.Tokenize(glob, ref globTokens);
    ReadOnlySpan<GlobToken> tokenizedGlob = globTokens.Slice(0, tokenCount);

    // Do your matching here...
    bool firstMatch = Glob.Match(pattern, glob, "path/fooatstand");
    bool secondMatch = Glob.Match(pattern, glob, "badpath/fooatstand");
}
finally
{
    if (glob.Length > MaxGlobTokenArrayLength)
    {
        ArrayPool<GlobToken>.Shared.Return(globTokenArray);
    }
}
```

## Benchmarks

We have used Benchmark Dotnet to compare the performance with raw RegEx and DotNet.Glob. This represents a current example run.

### Compile
|                  Method |              Pattern |        Mean |     Error |    StdDev | Ratio |  Gen 0 |  Gen 1 | Allocated |
|------------------------ |--------------------- |------------:|----------:|----------:|------:|-------:|-------:|----------:|
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** | **14,909.2 ns** | **133.33 ns** | **118.19 ns** | **1.000** | **3.4332** |      **-** |  **14,392 B** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |    945.6 ns |  17.62 ns |  16.48 ns | 0.063 | 0.4539 |      - |   1,904 B |
|         New_Corvus_Glob | p?th/(...)].txt [21] |    143.2 ns |   2.32 ns |   2.17 ns | 0.010 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **18,208.4 ns** | **240.42 ns** | **224.88 ns** |  **1.00** | **4.1504** |      **-** |  **17,432 B** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,474.3 ns |  23.87 ns |  22.33 ns |  0.08 | 0.5665 |      - |   2,376 B |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    237.4 ns |   2.63 ns |   2.46 ns |  0.01 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** | **13,446.5 ns** | **236.34 ns** | **209.51 ns** | **1.000** | **3.1433** | **0.0153** |  **13,168 B** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |    751.9 ns |  14.59 ns |  13.65 ns | 0.056 | 0.3328 |      - |   1,392 B |
|         New_Corvus_Glob |      p?th/a[e-g].txt |    112.0 ns |   1.62 ns |   1.80 ns | 0.008 |      - |      - |         - |


### Compile and match false
|                 Method | NumberOfMatches |              Pattern |         Mean |       Error |      StdDev | Ratio |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|------------:|------------:|------:|-------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **109,683.1 ns** | **2,170.83 ns** | **4,944.08 ns** | **1.000** | **5.3711** | **2.6855** |      **-** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |   1,047.2 ns |    19.38 ns |    18.13 ns | 0.009 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     178.7 ns |     3.53 ns |     3.31 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **119,914.8 ns** | **1,053.23 ns** |   **933.66 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,517.9 ns |    13.69 ns |    12.14 ns | 0.013 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     270.0 ns |     2.84 ns |     2.66 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** | **102,695.8 ns** |   **910.79 ns** |   **807.40 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     781.7 ns |    14.48 ns |    12.83 ns | 0.008 | 0.3328 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     135.9 ns |     1.66 ns |     1.47 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** | **p?th/(...)].txt [21]** | **105,913.4 ns** | **1,257.47 ns** | **1,050.05 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              25 | p?th/(...)].txt [21] |   1,212.6 ns |    23.66 ns |    33.93 ns | 0.011 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              25 | p?th/(...)].txt [21] |     809.1 ns |    11.83 ns |    11.07 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** | **p?th/(...)].txt [46]** | **116,495.4 ns** | **1,450.00 ns** | **1,356.33 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |              25 | p?th/(...)].txt [46] |   1,675.7 ns |    22.96 ns |    20.36 ns | 0.014 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              25 | p?th/(...)].txt [46] |     924.4 ns |    16.12 ns |    14.29 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** |      **p?th/a[e-g].txt** | **100,932.1 ns** |   **879.10 ns** |   **779.30 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              25 |      p?th/a[e-g].txt |     998.9 ns |    19.89 ns |    25.15 ns | 0.010 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              25 |      p?th/a[e-g].txt |     883.0 ns |     9.05 ns |     8.03 ns | 0.009 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [21]** | **105,720.3 ns** | **1,271.93 ns** | **1,189.77 ns** |  **1.00** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,465.4 ns |    28.88 ns |    44.11 ns |  0.01 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,494.1 ns |    28.95 ns |    28.43 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [46]** | **121,509.1 ns** | **2,417.40 ns** | **4,420.36 ns** |  **1.00** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [46] |   1,929.1 ns |    29.70 ns |    24.80 ns |  0.02 | 0.5646 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [46] |   1,621.9 ns |    23.12 ns |    20.49 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** |      **p?th/a[e-g].txt** | **105,058.5 ns** | **1,972.33 ns** | **1,844.92 ns** |  **1.00** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,201.6 ns |    13.53 ns |    12.65 ns |  0.01 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,694.6 ns |    33.72 ns |    34.63 ns |  0.02 |      - |      - |      - |         - |

### Compile and match true
|                 Method | NumberOfMatches |              Pattern |         Mean |       Error |      StdDev |       Median | Ratio |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|------------:|------------:|-------------:|------:|-------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **107,008.0 ns** |   **647.46 ns** |   **605.64 ns** | **106,978.8 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     976.3 ns |    18.50 ns |    22.02 ns |     974.8 ns | 0.009 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     169.5 ns |     3.12 ns |     2.92 ns |     170.4 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **115,648.9 ns** | **1,154.25 ns** |   **963.85 ns** | **115,890.0 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,467.5 ns |    28.34 ns |    33.74 ns |   1,475.9 ns | 0.013 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     270.0 ns |     5.35 ns |     7.84 ns |     267.7 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** | **108,997.2 ns** | **2,350.94 ns** | **6,857.79 ns** | **107,960.3 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     760.9 ns |    10.94 ns |     9.70 ns |     758.5 ns | 0.007 | 0.3328 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     133.8 ns |     2.16 ns |     2.02 ns |     133.8 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** | **p?th/(...)].txt [21]** | **104,789.2 ns** | **1,665.29 ns** | **1,557.72 ns** | **104,119.0 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              25 | p?th/(...)].txt [21] |   1,209.6 ns |    21.52 ns |    19.08 ns |   1,209.4 ns | 0.012 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              25 | p?th/(...)].txt [21] |     804.1 ns |     9.00 ns |     8.41 ns |     804.8 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** | **p?th/(...)].txt [46]** | **115,939.9 ns** |   **985.90 ns** |   **922.21 ns** | **115,916.7 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |              25 | p?th/(...)].txt [46] |   1,634.5 ns |    31.48 ns |    37.48 ns |   1,625.6 ns | 0.014 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              25 | p?th/(...)].txt [46] |     899.4 ns |     8.07 ns |     6.30 ns |     898.0 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **25** |      **p?th/a[e-g].txt** | **105,203.3 ns** | **2,047.93 ns** | **4,827.21 ns** | **103,594.3 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              25 |      p?th/a[e-g].txt |     972.8 ns |     8.56 ns |     8.01 ns |     974.3 ns | 0.009 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              25 |      p?th/a[e-g].txt |     799.9 ns |    14.29 ns |    13.36 ns |     800.3 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [21]** | **107,371.2 ns** |   **843.06 ns** |   **747.35 ns** | **107,411.0 ns** |  **1.00** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,401.5 ns |    15.56 ns |    14.55 ns |   1,404.1 ns |  0.01 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,519.5 ns |    30.19 ns |    58.88 ns |   1,494.7 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [46]** | **118,455.1 ns** | **2,039.64 ns** | **1,808.08 ns** | **118,066.1 ns** |  **1.00** | **6.9580** | **3.4180** | **0.2441** |  **29,591 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [46] |   2,034.7 ns |    39.65 ns |    56.86 ns |   2,012.9 ns |  0.02 | 0.5646 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [46] |   1,639.4 ns |    30.97 ns |    27.46 ns |   1,634.4 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** |      **p?th/a[e-g].txt** | **104,063.2 ns** | **1,841.14 ns** | **1,808.24 ns** | **103,641.9 ns** |  **1.00** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,158.9 ns |    23.10 ns |    21.61 ns |   1,162.9 ns |  0.01 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,663.3 ns |    13.55 ns |    12.01 ns |   1,661.0 ns |  0.02 |      - |      - |      - |         - |
