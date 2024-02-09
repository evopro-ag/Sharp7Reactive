<Query Kind="Statements">
  <NuGetReference Prerelease="true">Sharp7.Rx</NuGetReference>
  <Namespace>Sharp7.Rx</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Threading.Tasks</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

var ip = "10.30.3.221";  // Set IP address of S7
var db = 3;              // Set to an existing DB

// For rack number and cpu mpi address see 
// https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot
var rackNumber = 0;
var cpuMpiAddress = 0;

using var plc = new Sharp7Plc(ip, rackNumber, cpuMpiAddress);

await plc.InitializeAsync();
await plc.ConnectionState
	.FirstAsync(c => c == Sharp7.Rx.Enums.ConnectionState.Connected)
	.ToTask();

"Connection established".Dump();

for (int i = 0; i < 10; i++)
{
	await plc.SetValue($"DB{db}.Int6", (short)i);
	var value = await plc.GetValue<short>($"DB{db}.Int6");
	value.Dump();
	
	await Task.Delay(200);
}


