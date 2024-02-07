using System.Reflection;
using NUnit.Framework;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Shouldly;

namespace Sharp7.Rx.Tests.S7VariableAddressTests;

[TestFixture]
public class MatchesType
{
    static readonly IS7VariableNameParser parser = new S7VariableNameParser();

    
    public void Supported(Type type, string address)
    {
        Check(type, address, true);
    }

    public IEnumerable<TestCase> GetValid()
    {
        yield return new TestCase(typeof(bool), "DB0.DBx0.0");
        yield return new TestCase(typeof(short), "DB0.INT0");
        yield return new TestCase(typeof(int), "DB0.DINT0");
        yield return new TestCase(typeof(long), "DB0.DUL0");
        yield return new TestCase(typeof(ulong), "DB0.DUL0");
    }


    private static void Check(Type type, string address, bool expected)
    {
        //Arrange
        var variableAddress = parser.Parse(address);

        //Act
        variableAddress.MatchesType(type).ShouldBe(expected);
    }

    public record TestCase(Type Type, string Address);
}
