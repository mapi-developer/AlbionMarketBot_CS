using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhotonPackageParser;
using SharpPcap;

CaptureManager sniffer = new CaptureManager(device: CaptureDeviceList.Instance[3], "out");
sniffer.Start();
Console.ReadLine();      // <- keeps process alive
sniffer.Stop();

DataConverter.ConvertRawData();