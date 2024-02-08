using NUnit.Framework;
using Sharp7.Rx.Interfaces;
using Shouldly;

namespace Sharp7.Rx.Tests.S7ValueConverterTests;

[TestFixture]
internal class WriteToBuffer:ConverterTestBase
{
    [TestCaseSource(nameof(GetValidTestCases))]
    public void Write(ConverterTestCase tc)
    {
        //Arrange
        var buffer = new byte[tc.VariableAddress.BufferLength];
        var write = CreateWriteMethod(tc);

        //Act
        write.Invoke(null, [buffer, tc.Value, tc.VariableAddress]);

        //Assert
        buffer.ShouldBe(tc.Data);
    }

    [TestCase((char) 18, "DB0.DBB0")]
    [TestCase(0.25, "DB0.D0")]
    public void Invalid<T>(T input, string address)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        Should.Throw<InvalidOperationException>(() => S7ValueConverter.WriteToBuffer<T>(buffer, input, variableAddress));
    }
}
