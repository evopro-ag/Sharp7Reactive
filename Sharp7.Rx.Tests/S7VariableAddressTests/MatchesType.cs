using NUnit.Framework;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Tests.S7ValueConverterTests;
using Shouldly;

namespace Sharp7.Rx.Tests.S7VariableAddressTests;

[TestFixture]
public class MatchesType
{
    static readonly IS7VariableNameParser parser = new S7VariableNameParser();

    private static readonly IReadOnlyList<Type> typeList = new[]
    {
        typeof(byte),
        typeof(byte[]),

        typeof(bool),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),

        typeof(float),
        typeof(double),

        typeof(string),

        typeof(int[]),
        typeof(float[]),
        typeof(DateTime[]),
        typeof(object),
    };

    [TestCaseSource(nameof(GetValid))]
    public void Supported(TestCase tc) => Check(tc.Type, tc.Address, true);

    [TestCaseSource(nameof(GetInvalid))]
    public void Unsupported(TestCase tc) => Check(tc.Type, tc.Address, false);


    public static IEnumerable<TestCase> GetValid()
    {
        return
            ConverterTestBase.GetValidTestCases()
                .Select(tc => new TestCase(tc.Value.GetType(), tc.Address));
    }

    public static IEnumerable<TestCase> GetInvalid()
    {
        return
            ConverterTestBase.GetValidTestCases()
                .DistinctBy(tc => tc.Value.GetType())
                .SelectMany(tc =>
                                typeList.Where(type => type != tc.Value.GetType())
                                    .Select(type => new TestCase(type, tc.Address))
                )

                // Explicitly remove some valid combinations
                .Where(tc => !(
                            (tc.Type == typeof(string) && tc.Address == "DB99.Byte5") ||
                            (tc.Type == typeof(string) && tc.Address == "DB99.Byte5.4") ||
                            (tc.Type == typeof(byte[]) && tc.Address == "DB99.Byte5") 
                           ))
            ;
    }


    private static void Check(Type type, string address, bool expected)
    {
        //Arrange
        var variableAddress = parser.Parse(address);

        //Act
        variableAddress.MatchesType(type).ShouldBe(expected);
    }

    public record TestCase(Type Type, string Address)
    {
        public override string ToString() => $"{Type.Name} {Address}";
    }
}
