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

_ = plc.ConnectionState.Dump();

await plc.InitializeConnection();

// create an IObservable
_ = plc.CreateNotification<short>($"DB{db}.Int6", Sharp7.Rx.Enums.TransmissionMode.OnChange).Dump("Int 6");
_ = plc.CreateNotification<float>($"DB{db}.Real10", Sharp7.Rx.Enums.TransmissionMode.OnChange).Dump("Real 10");



for (int i = 0; i < 15; i++)
{
	switch (i%3) 
	{
		case 0:
			await plc.SetValue($"DB{db}.Int6", (short)i);
			break;
		case 1:
			await plc.SetValue($"DB{db}.Real10", i * 0.123f);
			break;
	}	
	
	await Task.Delay(300);
}


