<Query Kind="Statements">
  <NuGetReference Prerelease="true">Sharp7.Rx</NuGetReference>
  <Namespace>Sharp7.Rx</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Threading.Tasks</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// Set IP address of S7
var ip = "10.30.3.221";

// For rack number and cpu mpi address see 
// https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot
var rackNumber = 0;
var cpuMpiAddress = 0;

// Create Sharp7Plc
using var plc = new Sharp7Plc(ip, rackNumber, cpuMpiAddress);

// Initialize connection
await plc.InitializeConnection();

// wait for connection to be established
await plc.ConnectionState
	.FirstAsync(c => c == Sharp7.Rx.Enums.ConnectionState.Connected)
	.ToTask();

"Connection established".Dump();

try
{	        
	await Task.Delay(Timeout.Infinite, this.QueryCancelToken);
}
catch (TaskCanceledException)
{
	"Script stopped by user. Disconnecting by disposing plc.".Dump();
}


