# Corvus.Globbing
A zero allocation globbing library

## Purpose

We built this to provide a zero-allocation globbing library with performance comparable to (or better than) https://github.com/dazinator/DotNet.Glob and raw Regular Expressions, when running under dotnet50.

## Use cases

The particular use case we have optimized for is a glob that has come from an external source (e.g. a database or configuration) that is unlikely to be cacheable due to either volume or transience.

We want a very high-performance parse of that glob, and then a performant application of the glob to a number of candidate paths.

Our motivation for this came when "link stripping" documents to be returned from an HTTP request, to remove links that the requesting identity is not permitted to see
(perhaps for security, feature enablement, or just local context).

We also want to minimize allocations on the hot-path of a request handler. 

[Dotnet.Glob](https://github.com/dazinator/DotNet.Glob) offers better raw performance with a pre-compiled and cached glob used many times (at the expense of some heap allocation per glob), but Corvus.Globbing offers better performance when compiling and using a glob transiently against ~100 paths or fewer, with zero allocations.

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

// There can't be more tokens than there are characters the glob pattern, so we allocate an array at least that long.
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

### Compile
|                  Method |              Pattern |        Mean |     Error |    StdDev | Ratio |  Gen 0 |  Gen 1 | Allocated |
|------------------------ |--------------------- |------------:|----------:|----------:|------:|-------:|-------:|----------:|
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** | **14,883.7 ns** | **283.13 ns** | **278.08 ns** | **1.000** | **3.4332** |      **-** |  **14,392 B** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |    955.9 ns |  15.88 ns |  16.99 ns | 0.064 | 0.4549 |      - |   1,904 B |
|         New_Corvus_Glob | p?th/(...)].txt [21] |    142.2 ns |   2.31 ns |   2.16 ns | 0.010 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **17,817.1 ns** | **346.61 ns** | **412.62 ns** |  **1.00** | **4.1504** |      **-** |  **17,440 B** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,435.8 ns |  25.88 ns |  24.21 ns |  0.08 | 0.5665 |      - |   2,376 B |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    233.0 ns |   4.67 ns |   5.19 ns |  0.01 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** | **13,232.9 ns** |  **78.72 ns** |  **73.63 ns** | **1.000** | **3.1433** | **0.0153** |  **13,168 B** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |    751.6 ns |   5.45 ns |   4.55 ns | 0.057 | 0.3328 |      - |   1,392 B |
|         New_Corvus_Glob |      p?th/a[e-g].txt |    108.1 ns |   2.20 ns |   3.22 ns | 0.008 |      - |      - |         - |

### Compile and match false
|                 Method | NumberOfMatches |              Pattern |         Mean |        Error |       StdDev |       Median | Ratio |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|-------------:|-------------:|-------------:|------:|-------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **110,675.0 ns** |  **3,623.11 ns** | **10,453.51 ns** | **107,280.6 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     996.4 ns |     18.58 ns |     17.38 ns |     993.6 ns | 0.009 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     153.2 ns |      3.00 ns |      7.18 ns |     152.1 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **125,468.4 ns** |  **2,442.93 ns** |  **4,148.28 ns** | **125,102.3 ns** | **1.000** | **6.8359** | **3.4180** |      **-** |  **29,590 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,537.9 ns |     43.03 ns |    122.07 ns |   1,520.2 ns | 0.013 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     211.3 ns |      1.41 ns |      1.18 ns |     211.5 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **96,843.6 ns** |  **1,025.64 ns** |    **909.21 ns** |  **96,805.6 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     742.9 ns |     13.44 ns |     11.91 ns |     743.6 ns | 0.008 | 0.3328 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     111.1 ns |      1.85 ns |      2.27 ns |     110.8 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [21]** | **100,380.4 ns** |    **451.83 ns** |    **400.54 ns** | **100,364.1 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,319.0 ns |     10.17 ns |      9.02 ns |   1,318.7 ns | 0.013 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [21] |     786.2 ns |      8.50 ns |      7.53 ns |     787.6 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [46]** | **112,300.1 ns** |    **876.26 ns** |    **819.65 ns** | **112,135.9 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [46] |   1,812.7 ns |     19.93 ns |     17.67 ns |   1,805.8 ns | 0.016 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [46] |     867.6 ns |     10.91 ns |      9.67 ns |     870.8 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** |      **p?th/a[e-g].txt** | **155,175.7 ns** | **20,866.73 ns** | **61,526.03 ns** | **131,159.9 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,195.6 ns |     32.82 ns |     95.20 ns |   1,159.0 ns | 0.009 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              50 |      p?th/a[e-g].txt |     811.2 ns |     15.58 ns |     32.53 ns |     815.8 ns | 0.005 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** | **p?th/(...)].txt [21]** | **116,029.8 ns** |  **2,286.81 ns** |  **2,633.49 ns** | **115,549.1 ns** |  **1.00** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |             100 | p?th/(...)].txt [21] |   2,044.5 ns |     20.18 ns |     18.87 ns |   2,042.0 ns |  0.02 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |             100 | p?th/(...)].txt [21] |   1,535.2 ns |     30.18 ns |     42.30 ns |   1,520.4 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** | **p?th/(...)].txt [46]** | **113,523.1 ns** |  **1,084.59 ns** |    **961.47 ns** | **113,622.4 ns** |  **1.00** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |             100 | p?th/(...)].txt [46] |   2,184.1 ns |     39.93 ns |     37.35 ns |   2,190.2 ns |  0.02 | 0.5646 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |             100 | p?th/(...)].txt [46] |   1,603.1 ns |     31.50 ns |     40.96 ns |   1,586.6 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |              |              |              |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** |      **p?th/a[e-g].txt** |  **98,851.8 ns** |  **1,371.59 ns** |  **1,145.34 ns** |  **98,621.0 ns** |  **1.00** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |             100 |      p?th/a[e-g].txt |   1,447.6 ns |     23.53 ns |     22.01 ns |   1,440.9 ns |  0.01 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |             100 |      p?th/a[e-g].txt |   1,430.0 ns |     25.51 ns |     22.61 ns |   1,428.3 ns |  0.01 |      - |      - |      - |         - |

### Compile and match true
|                 Method | NumberOfMatches |              Pattern |         Mean |       Error |      StdDev | Ratio |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|------------:|------------:|------:|-------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **112,391.4 ns** | **2,246.81 ns** | **6,263.21 ns** | **1.000** | **5.3711** | **2.6855** |      **-** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |   1,116.9 ns |    28.11 ns |    78.83 ns | 0.010 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     139.9 ns |     1.06 ns |     0.99 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **118,275.3 ns** | **1,867.09 ns** | **1,746.48 ns** | **1.000** | **6.9580** | **3.4180** | **0.2441** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,428.0 ns |    26.24 ns |    23.26 ns | 0.012 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     221.0 ns |     4.32 ns |     4.24 ns | 0.002 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** | **103,587.5 ns** | **1,564.19 ns** | **2,192.78 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     817.7 ns |    13.20 ns |    11.03 ns | 0.008 | 0.3328 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     115.3 ns |     0.89 ns |     0.83 ns | 0.001 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [21]** | **107,047.7 ns** | **1,792.64 ns** | **1,760.61 ns** | **1.000** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [21] |   1,354.0 ns |    20.88 ns |    19.53 ns | 0.013 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [21] |     807.3 ns |    13.87 ns |    12.98 ns | 0.008 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** | **p?th/(...)].txt [46]** | **118,934.6 ns** | **1,480.41 ns** | **1,312.35 ns** | **1.000** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |              50 | p?th/(...)].txt [46] |   1,855.1 ns |    23.50 ns |    20.83 ns | 0.016 | 0.5665 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |              50 | p?th/(...)].txt [46] |     866.3 ns |    12.72 ns |    11.28 ns | 0.007 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |              **50** |      **p?th/a[e-g].txt** | **105,303.3 ns** | **2,092.17 ns** | **3,495.54 ns** | **1.000** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |              50 |      p?th/a[e-g].txt |   1,196.9 ns |    11.83 ns |    10.48 ns | 0.011 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |              50 |      p?th/a[e-g].txt |     746.9 ns |    12.21 ns |    11.42 ns | 0.007 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** | **p?th/(...)].txt [21]** | **107,425.4 ns** | **1,965.90 ns** | **1,534.84 ns** |  **1.00** | **5.3711** | **2.6855** | **0.1221** |  **22,656 B** |
|     DotNetGlob_IsMatch |             100 | p?th/(...)].txt [21] |   1,905.5 ns |    31.33 ns |    27.78 ns |  0.02 | 0.4539 |      - |      - |   1,904 B |
|     CorvusGlob_IsMatch |             100 | p?th/(...)].txt [21] |   1,481.4 ns |    20.38 ns |    19.07 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** | **p?th/(...)].txt [46]** | **120,977.3 ns** | **1,673.10 ns** | **1,643.21 ns** |  **1.00** | **6.9580** | **3.4180** | **0.1221** |  **29,591 B** |
|     DotNetGlob_IsMatch |             100 | p?th/(...)].txt [46] |   2,454.8 ns |    32.64 ns |    30.53 ns |  0.02 | 0.5646 |      - |      - |   2,376 B |
|     CorvusGlob_IsMatch |             100 | p?th/(...)].txt [46] |   1,579.8 ns |    22.45 ns |    19.90 ns |  0.01 |      - |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |        |           |
| **Compiled_Regex_IsMatch** |             **100** |      **p?th/a[e-g].txt** | **103,786.5 ns** | **1,253.12 ns** |   **978.36 ns** |  **1.00** | **4.6387** | **2.3193** | **0.1221** |  **19,799 B** |
|     DotNetGlob_IsMatch |             100 |      p?th/a[e-g].txt |   1,531.5 ns |    26.84 ns |    23.80 ns |  0.01 | 0.3319 |      - |      - |   1,392 B |
|     CorvusGlob_IsMatch |             100 |      p?th/a[e-g].txt |   1,408.6 ns |    16.46 ns |    14.59 ns |  0.01 |      - |      - |      - |         - |

# Credits

This library would not exist without [Dotnet Glob](https://github.com/dazinator/DotNet.Glob) - I've built the specs from its unit tests, and modelled the actual matching algorithms on the implementation in that library (although it is somewhat different in structure). The project has a "give back" mechanism so I've donated a small amount. You could consider doing so too.
