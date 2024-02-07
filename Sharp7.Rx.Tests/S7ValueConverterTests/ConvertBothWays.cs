using NUnit.Framework;
using Sharp7.Rx.Interfaces;
using Shouldly;

namespace Sharp7.Rx.Tests.S7ValueConverterTests;

[TestFixture]
public class ConvertBothWays
{
    static readonly IS7VariableNameParser parser = new S7VariableNameParser();

    [TestCase(true, "DB0.DBx0.0")]
    [TestCase(false, "DB0.DBx0.0")]
    [TestCase(true, "DB0.DBx0.4")]
    [TestCase(false, "DB0.DBx0.4")]
    [TestCase((byte) 18, "DB0.DBB0")]
    [TestCase((short) 4660, "DB0.INT0")]
    [TestCase((short)-3532, "DB0.INT0")]
    [TestCase(-3532, "DB0.INT0")]
    [TestCase(305419879, "DB0.DINT0")]
    [TestCase(-231451033, "DB0.DINT0")]
    [TestCase(1311768394163015151L, "DB0.dul0")]
    [TestCase(-994074615050678801L, "DB0.dul0")]
    [TestCase(1311768394163015151uL, "DB0.dul0")]
    [TestCase(17452669458658872815uL, "DB0.dul0")]
    [TestCase(new byte[] { 0x12, 0x34, 0x56, 0x67 }, "DB0.DBB0.4")]
    [TestCase(0.25f, "DB0.D0")]
    [TestCase("ABCD", "DB0.string0.4")]
    [TestCase("ABCD", "DB0.string0.4")] // Clip to length in Address
    [TestCase("ABCD", "DB0.DBB0.4")]
    public void Write<T>(T input, string address)
    {
        //Arrange
        var variableAddress = parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        S7ValueConverter.WriteToBuffer(buffer, input, variableAddress);
        var result = S7ValueConverter.ConvertToType<T>(buffer, variableAddress);

        //Assert
        result.ShouldBe(input);
    }

}
