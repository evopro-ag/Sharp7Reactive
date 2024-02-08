using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

internal static class S7ValueConverter
{
    private static readonly Dictionary<Type, WriteFunc> writeFunctions = new()
    {
        {
            typeof(bool), (data, address, value) =>
            {
                var byteValue = (bool) value ? (byte) 1 : (byte) 0;
                var shifted = (byte) (byteValue << address.Bit!);
                data[0] = shifted;
            }
        },

        {typeof(byte), (data, address, value) => data[0] = (byte) value},
        {
            typeof(byte[]), (data, address, value) =>
            {
                var source = (byte[]) value;

                var length = Math.Min(Math.Min(source.Length, data.Length), address.Length);

                source.AsSpan(0, length).CopyTo(data);
            }
        },

        {typeof(short), (data, address, value) => BinaryPrimitives.WriteInt16BigEndian(data, (short) value)},
        {typeof(ushort), (data, address, value) => BinaryPrimitives.WriteUInt16BigEndian(data, (ushort) value)},
        {typeof(int), (data, address, value) => BinaryPrimitives.WriteInt32BigEndian(data, (int) value)},
        {typeof(uint), (data, address, value) => BinaryPrimitives.WriteUInt32BigEndian(data, (uint) value)},
        {typeof(long), (data, address, value) => BinaryPrimitives.WriteInt64BigEndian(data, (long) value)},
        {typeof(ulong), (data, address, value) => BinaryPrimitives.WriteUInt64BigEndian(data, (ulong) value)},

        {
            typeof(float), (data, address, value) =>
            {
                var map = new UInt32SingleMap
                {
                    Single = (float) value
                };

                BinaryPrimitives.WriteUInt32BigEndian(data, map.UInt32);
            }
        },
        {
            typeof(double), (data, address, value) =>
            {
                var map = new UInt64DoubleMap
                {
                    Double = (double) value
                };

                BinaryPrimitives.WriteUInt64BigEndian(data, map.UInt64);
            }
        },

        {
            typeof(string), (data, address, value) =>
            {
                if (value is not string stringValue) throw new ArgumentException("Value must be of type string", nameof(value));

                var length = Math.Min(address.Length, stringValue.Length);

                switch (address.Type)
                {
                    case DbType.String:
                        data[0] = (byte) address.Length;
                        data[1] = (byte) length;

                        // Todo: Serialize directly to Span, when upgrading to .net
                        Encoding.ASCII.GetBytes(stringValue)
                            .AsSpan(0, length)
                            .CopyTo(data.Slice(2));
                        return;
                    case DbType.WString:
                        BinaryPrimitives.WriteUInt16BigEndian(data, address.Length);
                        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(2), (ushort) length);

                        // Todo: Serialize directly to Span, when upgrading to .net
                        Encoding.BigEndianUnicode.GetBytes(stringValue)
                            .AsSpan(0, length * 2)
                            .CopyTo(data.Slice(4));
                        return;
                    case DbType.Byte:
                        // Todo: Serialize directly to Span, when upgrading to .net
                        Encoding.ASCII.GetBytes(stringValue)
                            .AsSpan(0, length)
                            .CopyTo(data);
                        return;
                    default:
                        throw new DataTypeMissmatchException($"Cannot write string to {address.Type}", typeof(string), address);
                }
            }
        }
    };

    private static readonly Dictionary<Type, ReadFunc> readFunctions = new()
    {
        {typeof(bool), (buffer, address) => (buffer[0] >> address.Bit & 1) > 0},

        {typeof(byte), (buffer, address) => buffer[0]},
        {typeof(byte[]), (buffer, address) => buffer.ToArray()},

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
                    DbType.Byte => Encoding.ASCII.GetString(buffer.ToArray()),
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
                    var length = Math.Min(address.Length, BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2, 2))) * 2;

                    return Encoding.BigEndianUnicode.GetString(buffer, 4, length);
                }
            }
        },
    };

    public static TValue ReadFromBuffer<TValue>(byte[] buffer, S7VariableAddress address)
    {
        // Todo: Change to Span<byte> when switched to newer .net

        if (buffer.Length < address.BufferLength)
            throw new ArgumentException($"Buffer must be at least {address.BufferLength} bytes long for {address}", nameof(buffer));

        var type = typeof(TValue);

        if (!readFunctions.TryGetValue(type, out var readFunc))
            throw new UnsupportedS7TypeException($"{type.Name} is not supported. {address}", type, address);

        var result = readFunc(buffer, address);
        return (TValue) result;
    }

    public static void WriteToBuffer<TValue>(Span<byte> buffer, TValue value, S7VariableAddress address)
    {
        if (buffer.Length < address.BufferLength)
            throw new ArgumentException($"Buffer must be at least {address.BufferLength} bytes long for {address}", nameof(buffer));

        var type = typeof(TValue);

        if (!writeFunctions.TryGetValue(type, out var writeFunc))
            throw new UnsupportedS7TypeException($"{type.Name} is not supported. {address}", type, address);

        writeFunc(buffer, address, value);
    }

    delegate object ReadFunc(byte[] data, S7VariableAddress address);

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

    delegate void WriteFunc(Span<byte> data, S7VariableAddress address, object value);
}
