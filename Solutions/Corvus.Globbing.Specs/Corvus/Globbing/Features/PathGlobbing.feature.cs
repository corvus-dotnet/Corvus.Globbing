// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Corvus.Globbing.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("PathGlobbing")]
    public partial class PathGlobbingFeature
    {
        
        private TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "PathGlobbing.feature"
#line hidden
        
        [NUnit.Framework.OneTimeSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Corvus/Globbing/Features", "PathGlobbing", "\tZero allocation globbed path matching", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.OneTimeTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<NUnit.Framework.TestContext>(NUnit.Framework.TestContext.CurrentContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Glob a path")]
        [NUnit.Framework.TestCaseAttribute("literal", "fliteral", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("literal", "foo/literal", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("literal", "literals", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("literal", "literals/foo", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("path/hats*nd", "path/hatsblahn", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("path/hats*nd", "path/hatsblahndt", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("/**/file.csv", "/file.txt", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("/*file.txt", "/folder", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("*Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("*ave*2", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("*ave 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("*ave 12", "wave 12/", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\THIS_IS_A_DIR\\**\\somefile.txt", "C:\\THIS_IS_A_DIR\\awesomefile.txt", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\name\\**", "C:\\name.ext", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\name\\**", "C:\\name_longer.ext", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("Bumpy/**/AssemblyInfo.cs", "Bumpy.Test/Properties/AssemblyInfo.cs", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\**\\MSVC*120.DLL", "C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\ANTLR3.RUNTIME.DLL", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("literal1", "LITERAL1", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("*ral*", "LITERAL1", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "LS", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "iS", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "Is", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/ac", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/Ad", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/BD", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("abc/**", "abcd", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("**\\segment1\\**\\segment2\\**", "C:\\test\\segment1\\src\\segment2", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("**/.*", "foobar.", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("**/~*", "/", "sensitive", "false", null)]
        [NUnit.Framework.TestCaseAttribute("literal", "literal", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("a/literal", "a/literal", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("path/*atstand", "path/fooatstand", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("path/?atstand", "path/hatstand", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("path/?atstand?", "path/hatstands", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("p?th/*a[bcd]", "pAth/fooooac", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("p?th/*a[bcd]b[e-g]a[1-4]", "pAth/fooooacbfa2", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("p?th/*a[bcd]b[e-g]a[1-4][!wxyz]", "pAth/fooooacbfa2v", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*", "pAth/fooooacbfa2vd4.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("path/**/somefile.txt", "path/foo/bar/baz/somefile.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("p?th/*a[bcd]b[e-g]a[1-4][!wxyz][!a-c][!1-3].*", "pGth/yGKNY6acbea3rm8.", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/**/file.*", "/folder/file.csv", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/**/file.*", "/file.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/file.*", "/file.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/*file.txt", "/file.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\THIS_IS_A_DIR\\*", "C:\\THIS_IS_A_DIR\\somefile", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/DIR1/*/*", "/DIR1/DIR2/file.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("~/*~3", "~/abc123~3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\*ave*2", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("Stuff, *", "Stuff, x", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("\\\"Stuff*", "\\\"Stuff", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("path/**/somefile.txt", "path//somefile.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/app*.js", "dist/app.js", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/app*.js", "dist/app.a72ka8234.js", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/y", "y", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/gfx/*.gfx", "HKEY_LOCAL_MACHINE\\gfx\\foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/gfx/*.gfx", "HKEY_LOCAL_MACHINE/gfx/foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/gfx/**/*.gfx", "a_b\\gfx\\bar\\foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/gfx/**/*.gfx", "a_b/gfx/bar/foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\gfx\\**\\*.gfx", "a_b\\gfx\\bar\\foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\gfx\\**\\*.gfx", "a_b/gfx/bar/foo.gfx", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/foo/bar!.baz", "/foo/bar!.baz", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/foo/bar[!!].baz", "/foo/bar7.baz", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/foo/bar[!]].baz", "/foo/bar9.baz", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/foo/bar[!?].baz", "/foo/bar7.baz", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/foo/bar[![].baz", "/foo/bar7.baz", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen\\[[]a]tor", "C:\\myergen\\[a]tor", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen\\[[]ator", "C:\\myergen\\[ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen\\[[][]]ator", "C:\\myergen\\[]ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen[*]ator", "C:\\myergen*ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen[*][]]ator", "C:\\myergen*]ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen[*]]ator", "C:\\myergen*ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen[*]]ator", "C:\\myergen]ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("C:\\myergen[?]ator", "C:\\myergen?ator", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("/path[\\]hatstand", "/path\\hatstand", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*\\**", "#test3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*\\**", "#test3\\", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*\\**", "\\#test3\\foo", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*\\**", "\\#test3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*", "#test3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*", "#this is a comment", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**\\[#!]*", "\\#test3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[#!]*\\**", "#this is a comment", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[#!]*", "#test3", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[#!]*", "#this is a comment", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("abc/**", "abc/def/hij.txt", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("a/**/b", "a/b", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("abc/**", "abc/def", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("literal1", "LITERAL1", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("literal1", "literal1", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("*ral*", "LITERAL1", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("*ral*", "literal1", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "LS", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "ls", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "iS", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("[list]s", "Is", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/ac", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/Ad", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/bC", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("range/[a-b][C-D]", "range/BD", "insensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("***", "/foo/bar", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/*", "/foo/bar/", "sensitive", "true", null)]
        [NUnit.Framework.TestCaseAttribute("**/*foo", "/foo/bar/baz", "sensitive", "false", null)]
        public virtual void GlobAPath(string glob, string path, string caseSensitive, string result, string[] exampleTags)
        {
            string[] tagsOfScenario = exampleTags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("glob", glob);
            argumentsOfScenario.Add("path", path);
            argumentsOfScenario.Add("caseSensitive", caseSensitive);
            argumentsOfScenario.Add("result", result);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Glob a path", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
#line 4
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 5
 testRunner.When(string.Format("I compare the path \"{0}\" to the glob \"{1}\" with a case {2} match", path, glob, caseSensitive), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 6
 testRunner.Then(string.Format("the result should be {0}", result), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
