#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Sharp7.Rx;

public static class S7ErrorCodes
{
    /// <summary>
    ///     This list is not exhaustive and should be considered work in progress.
    /// </summary>
    private static readonly HashSet<int> notDisconnectedErrorCodes =
    [
        0x000000, // OK
        0xC00000, // CPU: Item not available
        0x900000 // CPU: Address out of range
    ];

    private static readonly IReadOnlyDictionary<int, string> additionalErrorTexts = new Dictionary<int, string>
    {
        {0xC00000, "This happens when the DB does not exist."},
        {0x900000, "This happens when the DB is not long enough."},
        {
            0x40000, """
                     This error occurs when the DB is "optimized" or "PUT/GET communication" is not enabled.
                     See https://snap7.sourceforge.net/snap7_client.html#target_compatibility.
                     """
        }
    };

    /// <summary>
    ///     Some error codes indicate connection lost, in which case, the driver tries to reestablish connection.
    ///     Other error codes indicate a user error, like reading from an unavailable DB or exceeding
    ///     the DBs range. In this case the driver should not consider the connection to be lost.
    /// </summary>
    public static bool AssumeConnectionLost(int errorCode) =>
        !notDisconnectedErrorCodes.Contains(errorCode);

    public static string? GetAdditionalErrorText(int errorCode) =>
        additionalErrorTexts.GetValueOrDefault(errorCode);
}
