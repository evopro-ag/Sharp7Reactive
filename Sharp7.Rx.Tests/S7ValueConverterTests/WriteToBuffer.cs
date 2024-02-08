using NUnit.Framework;
using Shouldly;

namespace Sharp7.Rx.Tests.S7ValueConverterTests;

[TestFixture]
internal class WriteToBuffer : ConverterTestBase
{
    [TestCaseSource(nameof(GetValidTestCases))]
    [TestCaseSource(nameof(GetAdditinalWriteTestCases))]
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

    public static IEnumerable<ConverterTestCase> GetAdditinalWriteTestCases()
    {
        yield return new ConverterTestCase("aaaaBCDE", "DB0.string0.4", [0x04, 0x04, 0x61, 0x61, 0x61, 0x61]); // Length in address exceeds PLC string length
        yield return new ConverterTestCase("aaaaBCDE", "DB0.WString0.4", [0x00, 0x04, 0x00, 0x04, 0x00, 0x61, 0x00, 0x61, 0x00, 0x61, 0x00, 0x61]); // Length in address exceeds PLC string length
    }

    [TestCase(18, "DB0.DInt12", 3)]
    [TestCase(0.25f, "DB0.Real1", 3)]
    [TestCase("test", "DB0.String1.10", 9)]
    public void BufferToSmall<T>(T input, string address, int bufferSize)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);
        var buffer = new byte[bufferSize];

        //Act
        Should.Throw<ArgumentException>(() => S7ValueConverter.WriteToBuffer(buffer, input, variableAddress));
    }

    [TestCase((char) 18, "DB0.DBB0")]
    public void UnsupportedType<T>(T input, string address)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        Should.Throw<UnsupportedS7TypeException>(() => S7ValueConverter.WriteToBuffer(buffer, input, variableAddress));
    }
}
