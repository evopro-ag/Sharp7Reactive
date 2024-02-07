using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

internal static class S7ValueConverter
{
    public static void WriteToBuffer<TValue>(Span<byte> buffer, TValue value, S7VariableAddress address)
    {
    }

    public static TValue ConvertToType<TValue>(byte[] buffer, S7VariableAddress address)
    {
        if (typeof(TValue) == typeof(bool))
            return (TValue) (object) (((buffer[0] >> address.Bit) & 1) > 0);

        if (typeof(TValue) == typeof(int))
        {
            if (address.Length == 2)
                return (TValue) (object) (int) BinaryPrimitives.ReadInt16BigEndian(buffer);
            if (address.Length == 4)
                return (TValue) (object) BinaryPrimitives.ReadInt32BigEndian(buffer);

            throw new InvalidOperationException($"length must be 2 or 4 but is {address.Length}");
        }

        if (typeof(TValue) == typeof(long))
            return (TValue) (object) BinaryPrimitives.ReadInt64BigEndian(buffer);

        if (typeof(TValue) == typeof(ulong))
            return (TValue) (object) BinaryPrimitives.ReadUInt64BigEndian(buffer);

        if (typeof(TValue) == typeof(short))
            return (TValue) (object) BinaryPrimitives.ReadInt16BigEndian(buffer);

        if (typeof(TValue) == typeof(byte))
            return (TValue) (object) buffer[0];
        if (typeof(TValue) == typeof(char))
            return (TValue) (object) (char) buffer[0];

        if (typeof(TValue) == typeof(byte[]))
            return (TValue) (object) buffer;

        if (typeof(TValue) == typeof(double))
        {
            var d = new UInt32SingleMap
            {
                UInt32 = BinaryPrimitives.ReadUInt32BigEndian(buffer)
            };
            return (TValue) (object) (double) d.Single;
        }

        if (typeof(TValue) == typeof(float))
        {
            var d = new UInt32SingleMap
            {
                UInt32 = BinaryPrimitives.ReadUInt32BigEndian(buffer)
            };
            return (TValue) (object) d.Single;
        }

        if (typeof(TValue) == typeof(string))
            if (address.Type == DbType.String)
            {
                // First byte is maximal length
                // Second byte is actual length
                // https://cache.industry.siemens.com/dl/files/480/22506480/att_105176/v1/s7_scl_string_parameterzuweisung_e.pdf

                var length = Math.Min(address.Length, buffer[1]);

                return (TValue) (object) Encoding.ASCII.GetString(buffer, 2, length);
            }
            else
                return (TValue) (object) Encoding.ASCII.GetString(buffer).Trim();

        throw new InvalidOperationException(string.Format("type '{0}' not supported.", typeof(TValue)));
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UInt32SingleMap
    {
        [FieldOffset(0)] public uint UInt32;
        [FieldOffset(0)] public float Single;
    }
}
