using SharpPcap;

AlbionObserver sniffer = new AlbionObserver(device: CaptureDeviceList.Instance[3]);
sniffer.Start();
Console.ReadLine();
sniffer.Stop();
