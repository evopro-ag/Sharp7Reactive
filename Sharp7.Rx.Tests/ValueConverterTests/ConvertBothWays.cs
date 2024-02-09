using NUnit.Framework;
using Shouldly;

namespace Sharp7.Rx.Tests.ValueConverterTests;

[TestFixture]
internal class ConvertBothWays : ConverterTestBase
{
    [TestCaseSource(nameof(GetValidTestCases))]
    public void Convert(ConverterTestCase tc)
    {
        //Arrange
        var buffer = new byte[tc.VariableAddress.BufferLength];

        var write = CreateWriteMethod(tc);
        var read = CreateReadMethod(tc);

        //Act
        write.Invoke(null, [buffer, tc.Value, tc.VariableAddress]);
        var result = read.Invoke(null, [buffer, tc.VariableAddress]);

        //Assert
        result.ShouldBe(tc.Value);
    }
}
