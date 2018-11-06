using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Sharp7.Rx.Tests
{
    [Binding]
    public class ParsingS7VariableName
    {
        private S7VariableNameParser parser;

        [Given(@"I have an Parser")]
        public void GivenIHaveAnParser()
        {
            parser = new S7VariableNameParser();
        }

        [Given(@"I have the following variables")]
        public void GivenIHaveTheFollowingVariables(Table table)
        {
            var names = table.CreateSet<Vars>();

            ScenarioContext.Current.Set(names);
        }

        [When(@"I parse the var name")]
        public void WhenIParseTheVarName()
        {
            var names = ScenarioContext.Current.Get<IEnumerable<Vars>>();
            var addresses = names.Select(v => parser.Parse(v.VarName)).ToArray();

            ScenarioContext.Current.Set(addresses);
        }

        [Then(@"the result should be")]
        public void ThenTheResultShouldBe(Table table)
        {
            var addresses = ScenarioContext.Current.Get<S7VariableAddress[]>();
            table.CompareToSet(addresses);
        }
    }

    class Vars
    {
        public string VarName { get; set; }
    }
}
