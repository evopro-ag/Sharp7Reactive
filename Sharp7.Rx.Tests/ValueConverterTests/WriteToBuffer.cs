using NUnit.Framework;
using Shouldly;

namespace Sharp7.Rx.Tests.ValueConverterTests;

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
        yield return new ConverterTestCase("a", "DB0.Byte80.3", [0x61, 0x00, 0x00]); // short string
        yield return new ConverterTestCase("abc", "DB0.Byte80.3", [0x61, 0x62, 0x63]); // matching string
        yield return new ConverterTestCase("abcxx", "DB0.Byte80.3", [0x61, 0x62, 0x63]); // long string

        yield return new ConverterTestCase("a", "DB0.string0.3", [0x03, 0x01, 0x61, 0x00, 0x00]); // short string
        yield return new ConverterTestCase("abc", "DB0.string0.3", [0x03, 0x03, 0x61, 0x62, 0x63]); // matching string
        yield return new ConverterTestCase("abcxx", "DB0.string0.3", [0x03, 0x03, 0x61, 0x62, 0x63]); // long string

        yield return new ConverterTestCase("a", "DB0.wstring0.3", [0x00, 0x03, 0x00, 0x01, 0x00, 0x61, 0x00, 0x00, 0x00, 0x00]); // short string
        yield return new ConverterTestCase("abc", "DB0.wstring0.3", [0x00, 0x03, 0x00, 0x03, 0x00, 0x61, 0x00, 0x62, 0x00, 0x63]); // matching string
        yield return new ConverterTestCase("abcxx", "DB0.wstring0.3", [0x00, 0x03, 0x00, 0x03, 0x00, 0x61, 0x00, 0x62, 0x00, 0x63]); // long string


        yield return new ConverterTestCase("aaaaBCDE", "DB0.string0.4", [0x04, 0x04, 0x61, 0x61, 0x61, 0x61]); // Length in address exceeds PLC string length
        yield return new ConverterTestCase("aaaaBCDE", "DB0.WString0.4", [0x00, 0x04, 0x00, 0x04, 0x00, 0x61, 0x00, 0x61, 0x00, 0x61, 0x00, 0x61]); // Length in address exceeds PLC string length
        yield return new ConverterTestCase("aaaaBCDE", "DB0.DBB0.4", [0x61, 0x61, 0x61, 0x61]); // Length in address exceeds PLC array length

        // Length in address exceeds PLC string length, multi char unicode point
        yield return new ConverterTestCase("\ud83d\udc69\ud83c\udffd\u200d\ud83d\ude80", "DB0.WString0.2", [0x00, 0x02, 0x00, 0x02, 0xD8, 0x3D, 0xDC, 0x69]);

        // Length in address exceeds PLC string length, multi char unicode point
        yield return new ConverterTestCase("\ud83d\udc69\ud83c\udffd\u200d\ud83d\ude80", "DB0.String0.2", [0x02, 0x02, 0x3F, 0x3F]);

        // Length in address exceeds PLC string length, multi char unicode point
        yield return new ConverterTestCase("\ud83d\udc69\ud83c\udffd\u200d\ud83d\ude80", "DB0.DBB0.4", [0x3F, 0x3F, 0x3F, 0x3F]);
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
        Should.Throw<ArgumentException>(() => ValueConverter.WriteToBuffer(buffer, input, variableAddress));
    }

    [TestCase((char) 18, "DB0.DBB0")]
    public void UnsupportedType<T>(T input, string address)
    {
        //Arrange
        var variableAddress = Parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        Should.Throw<UnsupportedS7TypeException>(() => ValueConverter.WriteToBuffer(buffer, input, variableAddress));
    }
}
