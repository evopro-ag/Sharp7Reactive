# Sharp7Reactive

[![Release](https://github.com/evopro-ag/Sharp7Reactive/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/evopro-ag/Sharp7Reactive/actions/workflows/release.yml)
![Licence](https://img.shields.io/github/license/evopro-ag/Sharp7Reactive.svg)
[![Nuget Version](https://img.shields.io/nuget/v/Sharp7.Rx.svg)](https://www.nuget.org/packages/Sharp7.Rx/)


This is an additional library for the usage if [Sharp7](https://github.com/fbarresi/sharp7).
It combines the S7 communication library with the power of System.Reactive.

## Main features
- Completly free and ready for usage (the library is already widely used in many enterprise environments)
- Connection status observation and auto-reconnect
- Type safe and with generics
- Threadsafe (Sharp7 is basically not threadsafe)

## Quick start

Install the [Sharp7.Rx Nuget package](https://www.nuget.org/packages/Sharp7.Rx).

The example below shows you how to create and use the Sharp7Rx PLC.

```csharp
using (var disposables = new CompositeDisposable())
{
    // create a new PLC
    var plc = new Sharp7Plc("10.30.3.10", 0, 2);
    disposables.Add(plc);
    
    // initialize and connect to the plc
    await plc.InitializeConnection();
    
    await plc.SetValue<bool>("DB2.DBX0.4", true); // set a bit
    var value = await plc.GetValue<short>("DB2.Int4"); // get an 16 bit integer
    Console.WriteLine(value)

    // create a nofication for data change in the plc
    var notification = plc.CreateNotification<bool>("DB1.DBX0.2", TransmissionMode.OnChange)
                          .Where(b => b) //select rising edge
                          .Do(_ => doStuff())
                          .Subscribe();
    disposables.Add(notification);
    
    //wait for enter before ending the program
    Console.ReadLine();
    
}
```

The best way to test your PLC application is running your [SoftPLC](https://github.com/fbarresi/softplc) locally.

## Examples

This library comes with integrated [LinqPad](https://www.linqpad.net/) examples - even for the free edition. Just download the [Sharp7.Rx Nuget package](https://www.nuget.org/packages/Sharp7.Rx) after pressing `Ctrl + Shift + P` and browse the "Samples".

[Sharp7Monitor](https://github.com/Peter-B-/Sharp7.Monitor) is a console application for monitoring S7 variables over RFC1006, based on this library.

## Addressing rules

Sharp7Reactive uses a syntax for identifying addresses similar to official siemens syntax. 
Every address has the form (case unsensitive) `DB<number>.<TYPE><Start>.<Length/Position>`.

| Example                              | Explaination                                                      |
| ------------------------------------ | ----------------------------------------------------------------- |
| `DB42.Int4` or<br> `DB42.DBD4`       | Datablock 42, 16 bit integer from bytes 4 to 5 (zero based index) |
| `DB42.Bit0.7` or<br>`DB42.DBX0.7`    | Datablock 42, bit from byte 0, position 7                         |
| `DB42.Byte4.25` or<br>`DB42.DBB4.25` | Datablock 42, 25 bytes from byte 4 to 29 (zero based index)       |

Here is a table of supported data types:

|.Net Type|Identifier                   |Description                                   |Length or bit                           |Example            |Example remark            |
|---------|-----------------------------|----------------------------------------------|----------------------------------------|-------------------|--------------------------|
|bool     |bit, dbx                     |Bit as boolean value                          |Bit index (0 .. 7)                      |`Db200.Bit2.2`     |Reads bit 3               |
|byte     |byte, dbb, b*                |8 bit unsigned integer                        |                                        |`Db200.Byte4`      |                          |
|byte[]   |byte, dbb, b*                |Array of bytes                                |Array length in bytes                   |`Db200.Byte4.16`   |                          |
|short    |int, dbw, w*                 |16 bit signed integer                         |                                        |`Db200.Int4`       |                          |
|ushort   |uint                         |16 bit unsigned integer                       |                                        |`Db200.UInt4`      |                          |
|int      |dint, dbd                    |32 bit signed integer                         |                                        |`Db200.DInt4`      |                          |
|uint     |udint                        |32 bit unsigned integer                       |                                        |`Db200.UDInt4`     |                          |
|long     |lint                         |64 bit signed integer                         |                                        |`Db200.LInt4`      |                          |
|ulong    |ulint, dul*, dulint*, dulong*|64 bit unsigned integer                       |                                        |`Db200.ULInt4`     |                          |
|float    |real, d*                     |32 bit float                                  |                                        |`Db200.Real4`      |                          |
|double   |lreal                        |64 bit float                                  |                                        |`Db200.LReal4`     |                          |
|string   |string, s*                   |ASCII text string with string size            |String length in bytes (1 .. 254)       |`Db200.String4.16` |Uses 18 bytes = 16 + 2    |
|string   |wstring                      |UTF-16 Big Endian text string with string size|String length in characters (1 .. 16382)|`Db200.WString4.16`|Uses 36 bytes = 16 * 2 + 4|
|string   |byte[]                       |ASCII string as byte array                    |String length in bytes                  |`Db200.Byte4.16`   |Uses 16 bytes             |

> Identifiers marked with * are kept for compatability reasons and might be removed in the future.

## Performance considerations

Frequent reads of variables using `GetValue` can cause performance pressure on the S7 PLC, resulting in an increase of cycle time.

If you frequently read variables, like polling triggers, use `CreateNotification`. Internally all variable polling initialized with `CreateNotification` is pooled to a single (or some) multi-variable-reads.

You can provide a cycle time (delay between consecutive multi variable reads) in the `Sharp7Plc` constructor:

```csharp
public Sharp7Plc(string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102, TimeSpan? multiVarRequestCycleTime = null)
```

The default value for `multiVarRequestCycleTime` is 100 ms, the minimal value is 5 ms.

## Would you like to contribute?

Yes, please!

Try the library and feel free to open an issue or ask for support. 

Don't forget to **star this project**! 
