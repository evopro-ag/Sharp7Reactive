using System.Collections.Generic;

namespace Sharp7.Rx
{
    public static class S7ErrorCodes
    {
        /// <summary>
        ///     This list is not exhaustive and should be considered work in progress.
        /// </summary>
        private static readonly HashSet<int> notDisconnectedErrorCodes = new HashSet<int>
        {
            0x000000, // OK
            0xC00000, // CPU: Item not available
            0x900000, // CPU: Address out of range
        };

        /// <summary>
        ///     Some error codes indicate connection lost, in which case, the driver tries to reestablish connection.
        ///     Other error codes indicate a user error, like reading from an unavailable DB or exceeding
        ///     the DBs range. In this case the driver should not consider the connection to be lost.
        /// </summary>
        public static bool AssumeConnectionLost(int errorCode)
        {
            return !notDisconnectedErrorCodes.Contains(errorCode);
        }
    }
}