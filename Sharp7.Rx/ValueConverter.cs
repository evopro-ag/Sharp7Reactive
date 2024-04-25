using System.Buffers.Binary;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx;

internal static class ValueConverter
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

        {typeof(byte), (data, _, value) => data[0] = (byte) value},
        {
            typeof(byte[]), (data, address, value) =>
            {
                var source = (byte[]) value;

                var length = Math.Min(Math.Min(source.Length, data.Length), address.Length);

                source.AsSpan(0, length).CopyTo(data);
            }
        },

        {typeof(short), (data, _, value) => BinaryPrimitives.WriteInt16BigEndian(data, (short) value)},
        {typeof(ushort), (data, _, value) => BinaryPrimitives.WriteUInt16BigEndian(data, (ushort) value)},
        {typeof(int), (data, _, value) => BinaryPrimitives.WriteInt32BigEndian(data, (int) value)},
        {typeof(uint), (data, _, value) => BinaryPrimitives.WriteUInt32BigEndian(data, (uint) value)},
        {typeof(long), (data, _, value) => BinaryPrimitives.WriteInt64BigEndian(data, (long) value)},
        {typeof(ulong), (data, _, value) => BinaryPrimitives.WriteUInt64BigEndian(data, (ulong) value)},

        {typeof(float), (data, _, value) => BinaryPrimitives.WriteSingleBigEndian(data, (float) value)},
        {typeof(double), (data, _, value) => BinaryPrimitives.WriteDoubleBigEndian(data, (double) value)},

        {
            typeof(string), (data, address, value) =>
            {
                if (value is not string stringValue) throw new ArgumentException("Value must be of type string", nameof(value));


                switch (address.Type)
                {
                    case DbType.String:
                        EncodeString(data);
                        return;
                    case DbType.WString:
                        EncodeWString(data);
                        return;
                    case DbType.Byte:

                        var readOnlySpan = stringValue.AsSpan(0, Math.Min(address.Length, stringValue.Length));
                        Encoding.ASCII.GetBytes(readOnlySpan, data);
                        return;
                    default:
                        throw new DataTypeMissmatchException($"Cannot write string to {address.Type}", typeof(string), address);
                }

                void EncodeString(Span<byte> span)
                {
                    var encodedLength = Encoding.ASCII.GetByteCount(stringValue);
                    var length = Math.Min(address.Length, encodedLength);

                    span[0] = (byte) address.Length;
                    span[1] = (byte) length;

                    Encoding.ASCII.GetBytes(stringValue.AsSpan(0, length), span[2..]);
                }

                void EncodeWString(Span<byte> span)
                {
                    var length = Math.Min(address.Length, stringValue.Length);

                    BinaryPrimitives.WriteUInt16BigEndian(span, address.Length);
                    BinaryPrimitives.WriteUInt16BigEndian(span[2..], (ushort) length);

                    var readOnlySpan = stringValue.AsSpan(0, length);
                    Encoding.BigEndianUnicode.GetBytes(readOnlySpan, span[4..]);
                }
            }
        }
    };

    private static readonly Dictionary<Type, ReadFunc> readFunctions = new()
    {
        {typeof(bool), (buffer, address) => (buffer[0] >> address.Bit & 1) > 0},

        {typeof(byte), (buffer, _) => buffer[0]},
        {typeof(byte[]), (buffer, _) => buffer.ToArray()},

        {typeof(short), (buffer, _) => BinaryPrimitives.ReadInt16BigEndian(buffer)},
        {typeof(ushort), (buffer, _) => BinaryPrimitives.ReadUInt16BigEndian(buffer)},
        {typeof(int), (buffer, _) => BinaryPrimitives.ReadInt32BigEndian(buffer)},
        {typeof(uint), (buffer, _) => BinaryPrimitives.ReadUInt32BigEndian(buffer)},
        {typeof(long), (buffer, _) => BinaryPrimitives.ReadInt64BigEndian(buffer)},
        {typeof(ulong), (buffer, _) => BinaryPrimitives.ReadUInt64BigEndian(buffer)},
        {typeof(float), (buffer, _) => BinaryPrimitives.ReadSingleBigEndian(buffer)},
        {typeof(double), (buffer, _) => BinaryPrimitives.ReadDoubleBigEndian(buffer)},

        {
            typeof(string), (buffer, address) =>
            {
                return address.Type switch
                {
                    DbType.String => ParseString(buffer),
                    DbType.WString => ParseWString(buffer),
                    DbType.Byte => Encoding.ASCII.GetString(buffer),
                    _ => throw new DataTypeMissmatchException($"Cannot read string from {address.Type}", typeof(string), address)
                };

                string ParseString(Span<byte> data)
                {
                    // First byte is maximal length
                    // Second byte is actual length
                    // https://support.industry.siemens.com/cs/mdm/109747174?c=94063831435&lc=de-DE

                    var length = Math.Min(address.Length, data[1]);

                    return Encoding.ASCII.GetString(data.Slice(2, length));
                }

                string ParseWString(Span<byte> data)
                {
                    // First 2 bytes are maximal length
                    // Second 2 bytes are actual length
                    // https://support.industry.siemens.com/cs/mdm/109747174?c=94063855243&lc=de-DE

                    // the length of the string is two bytes per character
                    var length = Math.Min(address.Length, BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2))) * 2;

                    return Encoding.BigEndianUnicode.GetString(data.Slice(4, length));
                }
            }
        },
    };

    public static TValue ReadFromBuffer<TValue>(Span<byte> buffer, VariableAddress address)
    {
        if (buffer.Length < address.BufferLength)
            throw new ArgumentException($"Buffer must be at least {address.BufferLength} bytes long for {address}", nameof(buffer));

        var type = typeof(TValue);

        if (!readFunctions.TryGetValue(type, out var readFunc))
            throw new UnsupportedS7TypeException($"{type.Name} is not supported. {address}", type, address);

        var result = readFunc(buffer, address);
        return (TValue) result;
    }

    public static void WriteToBuffer<TValue>(Span<byte> buffer, TValue value, VariableAddress address)
    {
        if (buffer.Length < address.BufferLength)
            throw new ArgumentException($"Buffer must be at least {address.BufferLength} bytes long for {address}", nameof(buffer));

        var type = typeof(TValue);

        if (!writeFunctions.TryGetValue(type, out var writeFunc))
            throw new UnsupportedS7TypeException($"{type.Name} is not supported. {address}", type, address);

        writeFunc(buffer, address, value);
    }

    private delegate object ReadFunc(Span<byte> data, VariableAddress address);

    private delegate void WriteFunc(Span<byte> data, VariableAddress address, object value);
}
