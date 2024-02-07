using NUnit.Framework;
using Sharp7.Rx.Interfaces;
using Shouldly;

namespace Sharp7.Rx.Tests;

[TestFixture]
public class S7ValueConverterTests
{
    static readonly IS7VariableNameParser parser = new S7VariableNameParser();

    [TestCase(true, "DB0.DBx0.0", new byte[] {0x01})]
    [TestCase(false, "DB0.DBx0.0", new byte[] {0x00})]
    [TestCase(true, "DB0.DBx0.4", new byte[] {0x10})]
    [TestCase(false, "DB0.DBx0.4", new byte[] {0})]
    [TestCase(true, "DB0.DBx0.4", new byte[] {0x1F})]
    [TestCase(false, "DB0.DBx0.4", new byte[] {0xEF})]
    [TestCase((byte) 18, "DB0.DBB0", new byte[] {0x12})]
    [TestCase((char) 18, "DB0.DBB0", new byte[] {0x12})]
    [TestCase((short) 4660, "DB0.INT0", new byte[] {0x12, 0x34})]
    [TestCase((short) -3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    [TestCase(-3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    [TestCase(305419879, "DB0.DINT0", new byte[] {0x12, 0x34, 0x56, 0x67})]
    [TestCase(-231451033, "DB0.DINT0", new byte[] {0xF2, 0x34, 0x56, 0x67})]
    [TestCase(1311768394163015151L, "DB0.dul0", new byte[] {0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(-994074615050678801L, "DB0.dul0", new byte[] {0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(1311768394163015151uL, "DB0.dul0", new byte[] {0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(17452669458658872815uL, "DB0.dul0", new byte[] {0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(new byte[] {0x12, 0x34, 0x56, 0x67}, "DB0.DBB0.4", new byte[] {0x12, 0x34, 0x56, 0x67})]
    [TestCase(0.25f, "DB0.D0", new byte[] {0x3E, 0x80, 0x00, 0x00})]
    [TestCase(0.25, "DB0.D0", new byte[] {0x3E, 0x80, 0x00, 0x00})]
    [TestCase("ABCD", "DB0.string0.4", new byte[] {0x00, 0x04, 0x41, 0x42, 0x43, 0x44})]
    [TestCase("ABCD", "DB0.string0.4", new byte[] {0x00, 0xF0, 0x41, 0x42, 0x43, 0x44})] // Clip to length in Address
    [TestCase("ABCD", "DB0.DBB0.4", new byte[] {0x41, 0x42, 0x43, 0x44})]
    public void Parse<T>(T expected, string address, byte[] data)
    {
        //Arrange
        var variableAddress = parser.Parse(address);

        //Act
        var result = S7ValueConverter.ConvertToType<T>(data, variableAddress);

        //Assert
        result.ShouldBe(expected);
    }

    [TestCase((ushort) 3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    public void Invalid<T>(T expected, string address, byte[] data)
    {
        //Arrange
        var variableAddress = parser.Parse(address);

        //Act
        Should.Throw<InvalidOperationException>(() => S7ValueConverter.ConvertToType<T>(data, variableAddress));
    }

    [TestCase(3532, "DB0.DINT0", new byte[] {0xF2, 0x34})]
    public void Argument<T>(T expected, string address, byte[] data)
    {
        //Arrange
        var variableAddress = parser.Parse(address);

        //Act
        Should.Throw<ArgumentException>(() => S7ValueConverter.ConvertToType<T>(data, variableAddress));
    }
}
