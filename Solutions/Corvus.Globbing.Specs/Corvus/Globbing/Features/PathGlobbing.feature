Feature: PathGlobbing
	Zero allocation globbed path matching

Scenario Outline: Glob a path
	When I compare the path "<path>" to the glob "<glob>" with a case <caseSensitive> match
	Then the result should be <result>

	Examples:
		| glob                                                      | path                                                        | caseSensitive | result |
		| literal                                                   | fliteral                                                    | sensitive     | false  |
		| literal                                                   | foo/literal                                                 | sensitive     | false  |
		| literal                                                   | literals                                                    | sensitive     | false  |
		| literal                                                   | literals/foo                                                | sensitive     | false  |
		| path/hats*nd                                              | path/hatsblahn                                              | sensitive     | false  |
		| path/hats*nd                                              | path/hatsblahndt                                            | sensitive     | false  |
		| /**/file.csv                                              | /file.txt                                                   | sensitive     | false  |
		| /*file.txt                                                | /folder                                                     | sensitive     | false  |
		| Shock* 12                                                 | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | false  |
		| *Shock* 12                                                | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | false  |
		| *ave*2                                                    | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | false  |
		| *ave 12                                                   | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | false  |
		| *ave 12                                                   | wave 12/                                                    | sensitive     | false  |
		| C:\\THIS_IS_A_DIR\\**\\somefile.txt                       | C:\\THIS_IS_A_DIR\\awesomefile.txt                          | sensitive     | false  |
		| C:\\name\\**                                              | C:\\name.ext                                                | sensitive     | false  |
		| C:\\name\\**                                              | C:\\name_longer.ext                                         | sensitive     | false  |
		| Bumpy/**/AssemblyInfo.cs                                  | Bumpy.Test/Properties/AssemblyInfo.cs                       | sensitive     | false  |
		| C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\**\\MSVC*120.DLL | C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\ANTLR3.RUNTIME.DLL | sensitive     | false  |
		| literal1                                                  | LITERAL1                                                    | sensitive     | false  |
		| *ral*                                                     | LITERAL1                                                    | sensitive     | false  |
		| [list]s                                                   | LS                                                          | sensitive     | false  |
		| [list]s                                                   | iS                                                          | sensitive     | false  |
		| [list]s                                                   | Is                                                          | sensitive     | false  |
		| range/[a-b][C-D]                                          | range/ac                                                    | sensitive     | false  |
		| range/[a-b][C-D]                                          | range/Ad                                                    | sensitive     | false  |
		| range/[a-b][C-D]                                          | range/BD                                                    | sensitive     | false  |
		| abc/**                                                    | abcd                                                        | sensitive     | false  |
		| **\\segment1\\**\\segment2\\**                            | C:\\test\\segment1\\src\\segment2                           | sensitive     | false  |
		| **/.*                                                     | foobar.                                                     | sensitive     | false  |
		| **/~*                                                     | /                                                           | sensitive     | false  |
		| literal                                                   | literal                                                     | sensitive     | true   |
		| a/literal                                                 | a/literal                                                   | sensitive     | true   |
		| path/*atstand                                             | path/fooatstand                                             | sensitive     | true   |
		| path/?atstand                                             | path/hatstand                                               | sensitive     | true   |
		| path/?atstand?                                            | path/hatstands                                              | sensitive     | true   |
		| p?th/*a[bcd]                                              | pAth/fooooac                                                | sensitive     | true   |
		| p?th/*a[bcd]b[e-g]a[1-4]                                  | pAth/fooooacbfa2                                            | sensitive     | true   |
		| p?th/*a[bcd]b[e-g]a[1-4][!wxyz]                           | pAth/fooooacbfa2v                                           | sensitive     | true   |
		| p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*             | pAth/fooooacbfa2vd4.txt                                     | sensitive     | true   |
		| path/**/somefile.txt                                      | path/foo/bar/baz/somefile.txt                               | sensitive     | true   |
		| p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*             | pGth/yGKNY6acbea3rm8.                                       | sensitive     | true   |
		| /**/file.*                                                | /folder/file.csv                                            | sensitive     | true   |
		| /**/file.*                                                | /file.txt                                                   | sensitive     | true   |
		| **/file.*                                                 | /file.txt                                                   | sensitive     | true   |
		| /*file.txt                                                | /file.txt                                                   | sensitive     | true   |
		| C:\\THIS_IS_A_DIR\\*                                      | C:\\THIS_IS_A_DIR\\somefile                                 | sensitive     | true   |
		| /DIR1/*/*                                                 | /DIR1/DIR2/file.txt                                         | sensitive     | true   |
		| ~/*~3                                                     | ~/abc123~3                                                  | sensitive     | true   |
		| **\\Shock* 12                                             | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | true   |
		| **\\*ave*2                                                | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | true   |
		| **                                                        | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12           | sensitive     | true   |
		| **                                                        | HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12.txt       | sensitive     | true   |
		| Stuff, *                                                  | Stuff, x                                                    | sensitive     | true   |
		| \"Stuff*                                                  | \"Stuff                                                     | sensitive     | true   |
		| path/**/somefile.txt                                      | path//somefile.txt                                          | sensitive     | true   |
		| **/app*.js                                                | dist/app.js                                                 | sensitive     | true   |
		| **/app*.js                                                | dist/app.a72ka8234.js                                       | sensitive     | true   |
		| **/y                                                      | y                                                           | sensitive     | true   |
		| **/gfx/*.gfx                                              | HKEY_LOCAL_MACHINE\\gfx\\foo.gfx                            | sensitive     | true   |
		| **/gfx/*.gfx                                              | HKEY_LOCAL_MACHINE/gfx/foo.gfx                              | sensitive     | true   |
		| **/gfx/**/*.gfx                                           | a_b\\gfx\\bar\\foo.gfx                                      | sensitive     | true   |
		| **/gfx/**/*.gfx                                           | a_b/gfx/bar/foo.gfx                                         | sensitive     | true   |
		| **\\gfx\\**\\*.gfx                                        | a_b\\gfx\\bar\\foo.gfx                                      | sensitive     | true   |
		| **\\gfx\\**\\*.gfx                                        | a_b/gfx/bar/foo.gfx                                         | sensitive     | true   |
		| /foo/bar!.baz                                             | /foo/bar!.baz                                               | sensitive     | true   |
		| /foo/bar[!!].baz                                          | /foo/bar7.baz                                               | sensitive     | true   |
		| /foo/bar[!]].baz                                          | /foo/bar9.baz                                               | sensitive     | true   |
		| /foo/bar[!?].baz                                          | /foo/bar7.baz                                               | sensitive     | true   |
		| /foo/bar[![].baz                                          | /foo/bar7.baz                                               | sensitive     | true   |
		| C:\myergen\[[]a]tor                                       | C:\myergen\[a]tor                                           | sensitive     | true   |
		| C:\myergen\[[]ator                                        | C:\myergen\[ator                                            | sensitive     | true   |
		| C:\myergen\[[][]]ator                                     | C:\myergen\[]ator                                           | sensitive     | true   |
		| C:\myergen[*]ator                                         | C:\myergen*ator                                             | sensitive     | true   |
		| C:\myergen[*][]]ator                                      | C:\myergen*]ator                                            | sensitive     | true   |
		| C:\myergen[*]]ator                                        | C:\myergen*ator                                             | sensitive     | true   |
		| C:\myergen[*]]ator                                        | C:\myergen]ator                                             | sensitive     | true   |
		| C:\myergen[?]ator                                         | C:\myergen?ator                                             | sensitive     | true   |
		| /path[\]hatstand                                          | /path\hatstand                                              | sensitive     | true   |
		| **\[#!]*\**                                               | #test3                                                      | sensitive     | true   |
		| **\[#!]*\**                                               | #test3\                                                     | sensitive     | true   |
		| **\[#!]*\**                                               | \#test3\foo                                                 | sensitive     | true   |
		| **\[#!]*\**                                               | \#test3                                                     | sensitive     | true   |
		| **\[#!]*                                                  | #test3                                                      | sensitive     | true   |
		| **\[#!]*                                                  | #this is a comment                                          | sensitive     | true   |
		| **\[#!]*                                                  | \#test3                                                     | sensitive     | true   |
		| [#!]*\**                                                  | #this is a comment                                          | sensitive     | true   |
		| [#!]*                                                     | #test3                                                      | sensitive     | true   |
		| [#!]*                                                     | #this is a comment                                          | sensitive     | true   |
		| abc/**                                                    | abc/def/hij.txt                                             | sensitive     | true   |
		| a/**/b                                                    | a/b                                                         | sensitive     | true   |
		| abc/**                                                    | abc/def                                                     | sensitive     | true   |
		| literal1                                                  | LITERAL1                                                    | insensitive   | true   |
		| literal1                                                  | literal1                                                    | insensitive   | true   |
		| *ral*                                                     | LITERAL1                                                    | insensitive   | true   |
		| *ral*                                                     | literal1                                                    | insensitive   | true   |
		| [list]s                                                   | LS                                                          | insensitive   | true   |
		| [list]s                                                   | ls                                                          | insensitive   | true   |
		| [list]s                                                   | iS                                                          | insensitive   | true   |
		| [list]s                                                   | Is                                                          | insensitive   | true   |
		| range/[a-b][C-D]                                          | range/ac                                                    | insensitive   | true   |
		| range/[a-b][C-D]                                          | range/Ad                                                    | insensitive   | true   |
		| range/[a-b][C-D]                                          | range/bC                                                    | insensitive   | true   |
		| range/[a-b][C-D]                                          | range/BD                                                    | insensitive   | true   |
		| ***                                                       | /foo/bar                                                    | sensitive     | true   |
		| **/*                                                      | /foo/bar/                                                   | sensitive     | true   |
		| **/*foo                                                   | /foo/bar/baz                                                | sensitive     | false  |
		| api/cases/*                                               | api/cases                                                   | insensitive   | false  |
		| api/cases/*                                               | API/CASES                                                   | insensitive   | false  |
		| api/cases/*                                               | API/CASES                                                   | sensitive     | false  |