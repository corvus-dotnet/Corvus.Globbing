// <copyright file="GlobbingSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Globbing.Steps
{
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Steps for the globbing specs.
    /// </summary>
    [Binding]
    public class GlobbingSteps
    {
        private const string ResultKey = "Result";

        private readonly ScenarioContext scenarioContext;

        public GlobbingSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [When(@"I compare the path ""(.*)"" to the glob ""(.*)"" with a case sensitive match")]
        public void WhenIMatchThePathToTheGlobCaseSensitive(string path, string glob)
        {
            this.scenarioContext.Set(GlobWithWildcardOptimzation.Match(glob, path), ResultKey);
        }

        [When(@"I compare the path ""(.*)"" to the glob ""(.*)"" with a case insensitive match")]
        public void WhenIMatchThePathToTheGlobCaseInsensitive(string path, string glob)
        {
            this.scenarioContext.Set(GlobWithWildcardOptimzation.Match(glob, path, System.StringComparison.OrdinalIgnoreCase), ResultKey);
        }

        [Then(@"the result should be true")]
        public void ThenTheResultShouldBeTrue()
        {
            Assert.IsTrue(this.scenarioContext.Get<bool>(ResultKey));
        }

        [Then(@"the result should be false")]
        public void ThenTheResultShouldBeFalse()
        {
            Assert.IsFalse(this.scenarioContext.Get<bool>(ResultKey));
        }
    }
}
