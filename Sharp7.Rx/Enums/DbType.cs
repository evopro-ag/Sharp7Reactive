namespace Sharp7.Rx.Enums;

// see https://support.industry.siemens.com/cs/mdm/109747174?c=88343664523&lc=de-DE
internal enum DbType
{
    Bit,

    /// <summary>
    ///     ASCII string
    /// </summary>
    String,

    /// <summary>
    ///     UTF16 string
    /// </summary>
    WString,

    Byte,

    /// <summary>
    ///     Int16
    /// </summary>
    Int,

    /// <summary>
    ///     UInt16
    /// </summary>
    UInt,

    /// <summary>
    ///     Int32
    /// </summary>
    DInt,

    /// <summary>
    ///     UInt32
    /// </summary>
    UDInt,

    /// <summary>
    ///     Int64
    /// </summary>
    LInt,

    /// <summary>
    ///     UInt64
    /// </summary>
    ULInt,

    Single,
    Double,
}
