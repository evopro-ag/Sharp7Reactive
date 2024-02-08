using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

internal static class S7ValueConverter
{
    private static readonly Dictionary<Type, Func<byte[], S7VariableAddress, object>> readFunctions = new()
    {
        {typeof(bool), (buffer, address) => (buffer[0] >> address.Bit & 1) > 0},

        {typeof(byte), (buffer, address) => buffer[0]},
        {typeof(byte[]), (buffer, address) => buffer},

        {typeof(short), (buffer, address) => BinaryPrimitives.ReadInt16BigEndian(buffer)},
        {typeof(ushort), (buffer, address) => BinaryPrimitives.ReadUInt16BigEndian(buffer)},
        {typeof(int), (buffer, address) => BinaryPrimitives.ReadInt32BigEndian(buffer)},
        {typeof(uint), (buffer, address) => BinaryPrimitives.ReadUInt32BigEndian(buffer)},
        {typeof(long), (buffer, address) => BinaryPrimitives.ReadInt64BigEndian(buffer)},
        {typeof(ulong), (buffer, address) => BinaryPrimitives.ReadUInt64BigEndian(buffer)},

        {
            typeof(float), (buffer, address) =>
            {
                // Todo: Use BinaryPrimitives when switched to newer .net
                var d = new UInt32SingleMap
                {
                    UInt32 = BinaryPrimitives.ReadUInt32BigEndian(buffer)
                };
                return d.Single;
            }
        },

        {
            typeof(double), (buffer, address) =>
            {
                // Todo: Use BinaryPrimitives when switched to newer .net
                var d = new UInt64DoubleMap
                {
                    UInt64 = BinaryPrimitives.ReadUInt64BigEndian(buffer)
                };
                return d.Double;
            }
        },

        {
            typeof(string), (buffer, address) =>
            {
                return address.Type switch
                {
                    DbType.String => ParseString(),
                    DbType.WString => ParseWString(),
                    DbType.Byte => Encoding.ASCII.GetString(buffer),
                    _ => throw new DataTypeMissmatchException($"Cannot read string from {address.Type}", typeof(string), address)
                };

                string ParseString()
                {
                    // First byte is maximal length
                    // Second byte is actual length
                    // https://support.industry.siemens.com/cs/mdm/109747174?c=94063831435&lc=de-DE

                    var length = Math.Min(address.Length, buffer[1]);

                    return Encoding.ASCII.GetString(buffer, 2, length);
                }

                string ParseWString()
                {
                    // First 2 bytes are maximal length
                    // Second 2 bytes are actual length
                    // https://support.industry.siemens.com/cs/mdm/109747174?c=94063855243&lc=de-DE

                    // the length of the string is two bytes per 
                    var length = Math.Min(address.Length, BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2,2))) * 2;

                    return Encoding.BigEndianUnicode.GetString(buffer, 4, length);
                }
            }
        },
    };

    public static TValue ReadFromBuffer<TValue>(byte[] buffer, S7VariableAddress address)
    {
        // Todo: Change to Span<byte> when switched to newer .net

        var type = typeof(TValue);

        if (!readFunctions.TryGetValue(type, out var readFunc))
            throw new UnsupportedS7TypeException($"{type.Name} is not supported. {address}", type, address);

        var result = readFunc(buffer, address);
        return (TValue) result;
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

    [StructLayout(LayoutKind.Explicit)]
    private struct UInt64DoubleMap
    {
        [FieldOffset(0)] public ulong UInt64;
        [FieldOffset(0)] public double Double;
    }
}
