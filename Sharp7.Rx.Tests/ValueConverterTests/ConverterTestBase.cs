using System.Reflection;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx.Tests.ValueConverterTests;

internal abstract class ConverterTestBase
{
    protected static readonly IVariableNameParser Parser = new VariableNameParser();

    public static MethodInfo CreateReadMethod(ConverterTestCase tc)
    {
        var convertMi = typeof(ConverterTestBase).GetMethod(nameof(ReadFromBuffer));
        var convert = convertMi!.MakeGenericMethod(tc.Value.GetType());
        return convert;
    }

    public static MethodInfo CreateWriteMethod(ConverterTestCase tc)
    {
        var writeMi = typeof(ConverterTestBase).GetMethod(nameof(WriteToBuffer));
        var write = writeMi!.MakeGenericMethod(tc.Value.GetType());
        return write;
    }

    public static IEnumerable<ConverterTestCase> GetValidTestCases()
    {
        yield return new ConverterTestCase(true, "DB99.bit5.4", [0x10]);
        yield return new ConverterTestCase(false, "DB99.bit5.4", [0x00]);

        yield return new ConverterTestCase((byte) 18, "DB99.Byte5", [0x12]);
        yield return new ConverterTestCase((short) 4660, "DB99.Int5", [0x12, 0x34]);
        yield return new ConverterTestCase((short) -3532, "DB99.Int5", [0xF2, 0x34]);
        yield return new ConverterTestCase((ushort) 4660, "DB99.UInt5", [0x12, 0x34]);
        yield return new ConverterTestCase((ushort) 62004, "DB99.UInt5", [0xF2, 0x34]);
        yield return new ConverterTestCase(305419879, "DB99.DInt5", [0x12, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(-231451033, "DB99.DInt5", [0xF2, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(305419879u, "DB99.UDInt5", [0x12, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(4063516263u, "DB99.UDInt5", [0xF2, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(1311768394163015151L, "DB99.LInt5", [0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(-994074615050678801L, "DB99.LInt5", [0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(1311768394163015151uL, "DB99.ULInt5", [0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(17452669458658872815uL, "DB99.ULInt5", [0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(0.25f, "DB99.Real5", [0x3E, 0x80, 0x00, 0x00]);
        yield return new ConverterTestCase(0.25, "DB99.LReal5", [0x3F, 0xD0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        yield return new ConverterTestCase(new byte[] {0x12, 0x34, 0x56, 0x67}, "DB99.Byte5.4", [0x12, 0x34, 0x56, 0x67]);

        yield return new ConverterTestCase("ABCD", "DB99.String10.4", [0x04, 0x04, 0x41, 0x42, 0x43, 0x44]);
        yield return new ConverterTestCase("ABCD", "DB99.String10.6", [0x06, 0x04, 0x41, 0x42, 0x43, 0x44, 0x00, 0x00]);
        yield return new ConverterTestCase("ABCD", "DB99.WString10.4", [0x00, 0x04, 0x00, 0x04, 0x00, 0x41, 0x00, 0x42, 0x00, 0x43, 0x00, 0x44]);
        yield return new ConverterTestCase("ABCD", "DB99.WString10.6", [0x00, 0x06, 0x00, 0x04, 0x00, 0x41, 0x00, 0x42, 0x00, 0x43, 0x00, 0x44, 0x00, 0x00, 0x00, 0x00]);
        yield return new ConverterTestCase("ABCD", "DB99.Byte5.4", [0x41, 0x42, 0x43, 0x44]);

        yield return new ConverterTestCase(true, "DB99.DBx0.0", [0x01]);
        yield return new ConverterTestCase(false, "DB99.DBx0.0", [0x00]);
        yield return new ConverterTestCase(true, "DB99.DBx0.4", [0x10]);
        yield return new ConverterTestCase(false, "DB99.DBx0.4", [0]);
        yield return new ConverterTestCase((byte) 18, "DB99.DBB0", [0x12]);
        yield return new ConverterTestCase((short) 4660, "DB99.INT0", [0x12, 0x34]);
        yield return new ConverterTestCase((short) -3532, "DB99.INT0", [0xF2, 0x34]);
        yield return new ConverterTestCase(305419879, "DB99.DINT0", [0x12, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(-231451033, "DB99.DINT0", [0xF2, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(1311768394163015151uL, "DB99.dul0", [0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(17452669458658872815uL, "DB99.dul0", [0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF]);
        yield return new ConverterTestCase(new byte[] {0x12, 0x34, 0x56, 0x67}, "DB99.DBB0.4", [0x12, 0x34, 0x56, 0x67]);
        yield return new ConverterTestCase(0.25f, "DB99.D0", [0x3E, 0x80, 0x00, 0x00]);
    }

    /// <summary>
    ///     This helper method exists, since I could not manage to invoke a generic method
    ///     with a Span&lt;T&gt; parameter.
    /// </summary>
    public static void WriteToBuffer<TValue>(byte[] buffer, TValue value, VariableAddress address) =>
        ValueConverter.WriteToBuffer(buffer, value, address);

    /// <summary>
    ///     This helper method exists, since I could not manage to invoke a generic method
    ///     with a Span&lt;T&gt; parameter.
    /// </summary>
    public static TValue ReadFromBuffer<TValue>(byte[] buffer, VariableAddress address) =>
        ValueConverter.ReadFromBuffer<TValue>(buffer, address);

    public record ConverterTestCase(object Value, string Address, byte[] Data)
    {
        public VariableAddress VariableAddress => Parser.Parse(Address);

        public override string ToString() => $"{Value.GetType().Name}, {Address}: {Value}";
    }
}
