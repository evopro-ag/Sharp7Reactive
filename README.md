# Sharp7Reactive

[![.NET Core Build](https://github.com/evopro-ag/Sharp7Reactive/actions/workflows/dotnet-core.yml/badge.svg)](https://github.com/evopro-ag/Sharp7Reactive/actions/workflows/dotnet-core.yml)
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
The example below shows you how to create and use the Sharp7Rx PLC.

```csharp
using (var disposables = new CompositeDisposable())
{
    // create a new PLC
    var plc = new Sharp7Plc("10.30.3.10", 0, 2);
    disposables.Add(plc);
    
    // initialize the plc
    await plc.InitializeAsync();
    
    //wait for the plc to get connected
    await plc.ConnectionState
             .FirstAsync(c => c == Sharp7.Rx.Enums.ConnectionState.Connected)
             .ToTask();
    
    await plc.SetValue<bool>("DB2.DBX0.4", true); // set a bit
    var bit = await plc.GetValue<int>("DB2.int4"); // get a bit
    
    // create a nofication for data change in the plc
    var notification = plc.CreateNotification<bool>("DB1.DBX0.2", TransmissionMode.OnChange, TimeSpan.FromMilliseconds(100))
                          .Where(b => b) //select rising edge
                          .Do(_ => doStuff())
                          .Subscribe();
    disposables.Add(notification);
    
    //wait for enter before ending the program
    Console.ReadLine();
    
}
```

the best way to test your PLC application is running your [SoftPLC](https://github.com/fbarresi/softplc) locally.

## S7 Addressing rules

Sharp7Reactive uses a syntax for identifying addresses similar to official siemens syntax. 
Every address has the form (case unsensible) `DB<number>.<TYPE><Start>.<Length/Position>`.
<br/>i.e.: `DB42.DBX0.7` => (means) Datablock 42, bit (DBX), Start: 0, Position: 7 
<br/>or<br/>
`DB42.DBB4.25` => (means) Datablock 42, bytes (DBB), Start: 4, Length: 25.

Following types are supported:
- `DBX` => Bit (bool)
- `DBB` => byte or byte[]
- `INT`
- `DINT`
- `DUL` => LINT
- `D` => REAL

## Would you like to contribute?

Yes, please!

Try the library and feel free to open an issue or ask for support. 

Don't forget to **star this project**! 
