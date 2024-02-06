using System;
using System.Linq;
using System.Text;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx
{
    internal static class S7ValueConverter
    {
        public static TValue ConvertToType<TValue>(byte[] buffer, S7VariableAddress address)
        {
            if (typeof(TValue) == typeof(bool))
            {
                return (TValue) (object) Convert.ToBoolean(buffer[0] & (1 << address.Bit));
            }

            if (typeof(TValue) == typeof(int))
            {
                if (address.Length == 2)
                    return (TValue)(object)((buffer[0] << 8) + buffer[1]);
                if (address.Length == 4)
                {
                    Array.Reverse(buffer);
                    return (TValue)(object)BitConverter.ToInt32(buffer,0);
                }

                throw new InvalidOperationException($"length must be 2 or 4 but is {address.Length}");
            }

            if (typeof(TValue) == typeof(long))
            {
                Array.Reverse(buffer);
                return (TValue)(object)BitConverter.ToInt64(buffer,0);
            }

            if (typeof(TValue) == typeof(ulong))
            {
                Array.Reverse(buffer);
                return (TValue)(object)BitConverter.ToUInt64(buffer, 0);
            }

            if (typeof(TValue) == typeof(short))
            {
                return (TValue)(object)(short)((buffer[0] << 8) + buffer[1]);
            }

            if (typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(char))
            {
                return (TValue)(object)buffer[0];
            }

            if (typeof(TValue) == typeof(byte[]))
            {
                return (TValue)(object)buffer;
            }

            if (typeof(TValue) == typeof(double) || typeof(TValue) == typeof(float))
            {
                var d = BitConverter.ToSingle(buffer.Reverse().ToArray(),0);
                return (TValue)(object)d;
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
                {
                    return (TValue) (object) Encoding.ASCII.GetString(buffer).Trim();
                }

            throw new InvalidOperationException(string.Format("type '{0}' not supported.", typeof(TValue)));
        }
    }
}