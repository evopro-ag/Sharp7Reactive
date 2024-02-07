using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

internal static class S7ValueConverter
{
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

        if (typeof(TValue) == typeof(byte[]))
            return (TValue) (object) buffer;
        
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

        throw new InvalidOperationException($"type '{typeof(TValue)}' not supported.");
    }

    public static void WriteToBuffer<TValue>(Span<byte> buffer, TValue value, S7VariableAddress address)
    {
        if (buffer.Length < address.BufferLength)
            throw new ArgumentException($"buffer must be at least {address.BufferLength} bytes long for {address}", nameof(buffer));

        if (typeof(TValue) == typeof(bool))
        {
            var byteValue = (bool) (object) value ? (byte) 1 : (byte) 0;
            var shifted = (byte) (byteValue << address.Bit);
            buffer[0] = shifted;
        }

        else if (typeof(TValue) == typeof(int))
        {
            if (address.Length == 2)
                BinaryPrimitives.WriteInt16BigEndian(buffer, (short) (int) (object) value);
            else
                BinaryPrimitives.WriteInt32BigEndian(buffer, (int) (object) value);
        }
        else if (typeof(TValue) == typeof(short))
        {
            if (address.Length == 2)
                BinaryPrimitives.WriteInt16BigEndian(buffer, (short) (object) value);
            else
                BinaryPrimitives.WriteInt32BigEndian(buffer, (short) (object) value);
        }
        else if (typeof(TValue) == typeof(long))
            BinaryPrimitives.WriteInt64BigEndian(buffer, (long) (object) value);
        else if (typeof(TValue) == typeof(ulong))
            BinaryPrimitives.WriteUInt64BigEndian(buffer, (ulong) (object) value);
        else if (typeof(TValue) == typeof(byte))
            buffer[0] = (byte) (object) value;
        else if (typeof(TValue) == typeof(byte[]))
        {
            var source = (byte[]) (object) value;

            var length = Math.Min(Math.Min(source.Length, buffer.Length), address.Length);

            source.AsSpan(0, length).CopyTo(buffer);
        }
        else if (typeof(TValue) == typeof(float))
        {
            var map = new UInt32SingleMap
            {
                Single = (float) (object) value
            };

            BinaryPrimitives.WriteUInt32BigEndian(buffer, map.UInt32);
        }
        else if (typeof(TValue) == typeof(string))
        {
            if (value is not string stringValue) throw new ArgumentException("Value must be of type string", nameof(value));

            // Todo: Serialize directly to Span, when upgrading to .net
            var stringBytes = Encoding.ASCII.GetBytes(stringValue);

            var length = Math.Min(address.Length, stringValue.Length);

            int stringOffset;
            if (address.Type == DbType.String)
            {
                stringOffset = 2;
                buffer[0] = (byte) address.Length;
                buffer[1] = (byte) length;
            }
            else
                stringOffset = 0;

            stringBytes.AsSpan(0, length).CopyTo(buffer.Slice(stringOffset));
        }
        else
        {
            throw new InvalidOperationException($"type '{typeof(TValue)}' not supported.");
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UInt32SingleMap
    {
        [FieldOffset(0)] public uint UInt32;
        [FieldOffset(0)] public float Single;
    }
}
