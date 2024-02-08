using NUnit.Framework;
using Shouldly;

namespace Sharp7.Rx.Tests.S7ValueConverterTests;

[TestFixture]
internal class ReadFromBuffer : ConverterTestBase
{
    [TestCaseSource(nameof(GetValidTestCases))]
    [TestCaseSource(nameof(GetAdditinalReadTestCases))]
    public void Read(ConverterTestCase tc)
    {
        //Arrange
        var convert = CreateReadMethod(tc);

        //Act
        var result = convert.Invoke(null, [tc.Data, tc.VariableAddress]);

        //Assert
        result.ShouldBe(tc.Value);
    }

    public static IEnumerable<ConverterTestCase> GetAdditinalReadTestCases()
    {
        yield return new ConverterTestCase(true, "DB0.DBx0.4", [0x1F]);
        yield return new ConverterTestCase(false, "DB0.DBx0.4", [0xEF]);
        yield return new ConverterTestCase("ABCD", "DB0.string0.10", [0x04, 0x04, 0x41, 0x42, 0x43, 0x44]); // Length in address exceeds PLC string length
    }

    [TestCase((char) 18, "DB0.DBB0", new byte[] {0x12})]
    [TestCase((ushort) 3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    [TestCase(0.25, "DB0.D0", new byte[] {0x3E, 0x80, 0x00, 0x00})]
    public void Invalid<T>(T template, string address, byte[] data)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);

        //Act
        Should.Throw<InvalidOperationException>(() => S7ValueConverter.ReadFromBuffer<T>(data, variableAddress));
    }

    [TestCase(3532, "DB0.DINT0", new byte[] {0xF2, 0x34})]
    public void Argument<T>(T template, string address, byte[] data)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);

        //Act
        Should.Throw<ArgumentException>(() => S7ValueConverter.ReadFromBuffer<T>(data, variableAddress));
    }
}
