# Corvus.Globbing
A zero allocation globbing library

## Purpose

We built this to provide a zero-allocation globbing library with performance comparable to (or better than) https://github.com/dazinator/DotNet.Glob and raw Regular Expressions, when running under dotnet50.

## Use cases

The particular use case we have optimized for is a case-sensitive glob that is unlikely to be cacheable due to either volume or transience. We want a very high-performance parse of that glob, and then a performant application of the glob to a number of candidate paths.

Our motivation for this came when "link stripping" documents to be returned from an HTTP request, to remove links that the requesting identity is not permitted to see
(perhaps for security, feature enablement, or just local context).

We also want to minimize allocations on the hot-path of a request handler. 

We offer better raw performance (~5-15%) with a pre-compiled and cached glob, for the `StringComparison.Ordinal` (case sensitive) default, than either `Regex` or [Dotnet.Glob](https://github.com/dazinator/DotNet.Glob). Our tokenization is also 6-8x faster than [Dotnet.Glob](https://github.com/dazinator/DotNet.Glob) so it is significantly faster in the single use/throwaway case. This compilation overhead ceases to be significant at ~500 reuses of the tokenized glob, and then raw performance takes over as the differentiator.

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
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [21]** | **13,966.8 ns** | **156.94 ns** | **139.13 ns** | **1.000** | **3.4332** |      **-** |  **14,392 B** |
|         New_DotNet_Glob | p?th/(...)].txt [21] |    902.8 ns |   8.66 ns |   7.68 ns | 0.065 | 0.4549 |      - |   1,904 B |
|         New_Corvus_Glob | p?th/(...)].txt [21] |    137.4 ns |   2.41 ns |   2.26 ns | 0.010 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** | **p?th/(...)].txt [46]** | **17,252.8 ns** | **276.09 ns** | **244.75 ns** |  **1.00** | **4.1504** |      **-** |  **17,432 B** |
|         New_DotNet_Glob | p?th/(...)].txt [46] |  1,399.1 ns |  14.40 ns |  12.76 ns |  0.08 | 0.5665 |      - |   2,376 B |
|         New_Corvus_Glob | p?th/(...)].txt [46] |    229.0 ns |   1.76 ns |   1.65 ns |  0.01 |      - |      - |         - |
|                         |                      |             |           |           |       |        |        |           |
| **New_Compiled_Regex_Glob** |      **p?th/a[e-g].txt** | **12,768.5 ns** | **106.07 ns** |  **94.03 ns** | **1.000** | **3.1433** | **0.0153** |  **13,168 B** |
|         New_DotNet_Glob |      p?th/a[e-g].txt |    742.6 ns |  14.68 ns |  14.42 ns | 0.058 | 0.3328 |      - |   1,392 B |
|         New_Corvus_Glob |      p?th/a[e-g].txt |    104.6 ns |   1.01 ns |   0.79 ns | 0.008 |      - |      - |         - |

### Compile and match false
|                 Method | NumberOfMatches |              Pattern |         Mean |       Error |      StdDev | Ratio |  Gen 0 |  Gen 1 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|------------:|------------:|------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **100,913.4 ns** | **1,928.34 ns** | **1,803.77 ns** | **1.000** | **5.3711** | **2.6855** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     968.2 ns |    17.06 ns |    15.96 ns | 0.010 | 0.4539 |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     130.6 ns |     2.40 ns |     2.24 ns | 0.001 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **112,909.0 ns** | **1,748.09 ns** | **1,635.16 ns** | **1.000** | **6.9580** | **3.4180** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,411.8 ns |     9.28 ns |     8.23 ns | 0.013 | 0.5665 |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     212.6 ns |     2.25 ns |     1.99 ns | 0.002 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **98,313.2 ns** |   **987.14 ns** |   **875.07 ns** | **1.000** | **4.6387** | **2.3193** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     731.6 ns |     9.59 ns |     8.97 ns | 0.007 | 0.3328 |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     107.6 ns |     1.82 ns |     1.70 ns | 0.001 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **294,398.9 ns** | **4,524.02 ns** | **4,231.77 ns** |  **1.00** | **5.3711** | **2.9297** |  **22,648 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 119,976.7 ns | 1,408.68 ns | 1,317.68 ns |  0.41 | 0.3662 |      - |   1,904 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 101,988.0 ns | 2,009.89 ns | 1,781.71 ns |  0.35 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **312,789.5 ns** | **2,228.74 ns** | **1,975.72 ns** |  **1.00** | **6.8359** | **3.4180** |  **29,582 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 121,822.4 ns | 1,539.99 ns | 1,440.51 ns |  0.39 | 0.4883 |      - |   2,376 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 103,786.9 ns |   911.27 ns |   852.40 ns |  0.33 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **293,532.5 ns** | **1,354.58 ns** | **1,267.07 ns** |  **1.00** | **4.3945** | **1.9531** |  **19,788 B** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 122,392.5 ns | 1,344.16 ns | 1,257.33 ns |  0.42 | 0.2441 |      - |   1,392 B |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 102,047.7 ns |   546.58 ns |   484.53 ns |  0.35 |      - |      - |         - |


### Compile and match true
|                 Method | NumberOfMatches |              Pattern |         Mean |       Error |      StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Allocated |
|----------------------- |---------------- |--------------------- |-------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [21]** | **102,153.9 ns** | **1,752.63 ns** | **1,553.66 ns** | **1.000** |    **0.00** | **5.3711** | **2.6855** |  **22,656 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [21] |     930.5 ns |    12.93 ns |    10.80 ns | 0.009 |    0.00 | 0.4549 |      - |   1,904 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [21] |     132.7 ns |     2.10 ns |     1.86 ns | 0.001 |    0.00 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** | **p?th/(...)].txt [46]** | **115,262.2 ns** | **1,820.57 ns** | **1,421.38 ns** | **1.000** |    **0.00** | **6.9580** | **3.4180** |  **29,591 B** |
|     DotNetGlob_IsMatch |               1 | p?th/(...)].txt [46] |   1,403.9 ns |    14.55 ns |    13.61 ns | 0.012 |    0.00 | 0.5665 |      - |   2,376 B |
|     CorvusGlob_IsMatch |               1 | p?th/(...)].txt [46] |     216.2 ns |     2.50 ns |     2.22 ns | 0.002 |    0.00 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |         |        |        |           |
| **Compiled_Regex_IsMatch** |               **1** |      **p?th/a[e-g].txt** |  **99,016.6 ns** | **1,930.47 ns** | **1,895.98 ns** | **1.000** |    **0.00** | **4.6387** | **2.3193** |  **19,799 B** |
|     DotNetGlob_IsMatch |               1 |      p?th/a[e-g].txt |     755.5 ns |    14.15 ns |    12.55 ns | 0.008 |    0.00 | 0.3328 |      - |   1,392 B |
|     CorvusGlob_IsMatch |               1 |      p?th/a[e-g].txt |     107.1 ns |     1.27 ns |     1.12 ns | 0.001 |    0.00 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **301,377.6 ns** | **4,131.04 ns** | **3,449.60 ns** |  **1.00** |    **0.00** | **5.3711** | **2.9297** |  **22,648 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 120,428.9 ns | 1,094.13 ns |   969.92 ns |  0.40 |    0.01 | 0.3662 |      - |   1,904 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 114,452.0 ns | 1,656.70 ns | 1,549.68 ns |  0.38 |    0.01 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **312,024.0 ns** | **2,595.52 ns** | **2,167.38 ns** |  **1.00** |    **0.00** | **6.8359** | **3.4180** |  **29,582 B** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 123,763.7 ns | 1,614.35 ns | 1,510.07 ns |  0.40 |    0.00 | 0.4883 |      - |   2,376 B |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 115,788.5 ns | 3,117.02 ns | 9,092.49 ns |  0.38 |    0.04 |      - |      - |         - |
|                        |                 |                      |              |             |             |       |         |        |        |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **301,720.6 ns** | **5,343.14 ns** | **9,216.65 ns** |  **1.00** |    **0.00** | **4.3945** | **1.9531** |  **19,796 B** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 122,207.3 ns | 1,735.94 ns | 1,623.80 ns |  0.40 |    0.02 | 0.2441 |      - |   1,392 B |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 102,780.3 ns |   915.17 ns |   856.05 ns |  0.33 |    0.01 |      - |      - |         - |

## Match false
|                 Method | NumberOfMatches |              Pattern |      Mean |    Error |   StdDev | Ratio | Allocated |
|----------------------- |---------------- |--------------------- |----------:|---------:|---------:|------:|----------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **354.98 μs** | **4.678 μs** | **4.376 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 116.59 μs | 0.741 μs | 0.693 μs |  0.33 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 100.86 μs | 1.925 μs | 1.707 μs |  0.28 |         - |
|                        |                 |                      |           |          |          |       |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **353.17 μs** | **6.017 μs** | **5.334 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 119.95 μs | 1.269 μs | 1.187 μs |  0.34 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 101.69 μs | 0.814 μs | 0.722 μs |  0.29 |         - |
|                        |                 |                      |           |          |          |       |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **350.99 μs** | **2.575 μs** | **2.408 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 119.78 μs | 1.194 μs | 1.117 μs |  0.34 |         - |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt |  95.80 μs | 1.546 μs | 1.446 μs |  0.27 |         - |

## Match true
|                 Method | NumberOfMatches |              Pattern |     Mean |   Error |  StdDev | Ratio | Allocated |
|----------------------- |---------------- |--------------------- |---------:|--------:|--------:|------:|----------:|
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [21]** | **370.4 μs** | **3.43 μs** | **3.21 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 119.1 μs | 0.92 μs | 0.76 μs |  0.32 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [21] | 107.4 μs | 1.95 μs | 1.73 μs |  0.29 |         - |
|                        |                 |                      |          |         |         |       |           |
| **Compiled_Regex_IsMatch** |           **10000** | **p?th/(...)].txt [46]** | **361.0 μs** | **2.99 μs** | **2.79 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 118.1 μs | 1.53 μs | 1.43 μs |  0.33 |         - |
|     CorvusGlob_IsMatch |           10000 | p?th/(...)].txt [46] | 109.5 μs | 0.94 μs | 0.88 μs |  0.30 |         - |
|                        |                 |                      |          |         |         |       |           |
| **Compiled_Regex_IsMatch** |           **10000** |      **p?th/a[e-g].txt** | **349.4 μs** | **2.06 μs** | **1.92 μs** |  **1.00** |         **-** |
|     DotNetGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 118.7 μs | 0.92 μs | 0.82 μs |  0.34 |         - |
|     CorvusGlob_IsMatch |           10000 |      p?th/a[e-g].txt | 100.3 μs | 1.60 μs | 1.49 μs |  0.29 |         - |

# Credits

This library would not exist without [Dotnet Glob](https://github.com/dazinator/DotNet.Glob) - I've built the specs from its unit tests, and modelled the actual matching algorithms on the implementation in that library (although it is somewhat different in structure). The project has a "give back" mechanism so I've donated a small amount. You could consider doing so too.
