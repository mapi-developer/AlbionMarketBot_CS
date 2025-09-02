using System.Collections.Concurrent;
using SharpPcap;
using PacketDotNet;
using PhotonPackageParser;
using Newtonsoft.Json.Linq;

public class AlbionObserver
{
    public string observingType = "offer"; // "offer" or "request"
    public JArray tempData = new JArray();
    private readonly ICaptureDevice _device;
    private readonly BlockingCollection<byte[]> _payloadQueue = new BlockingCollection<byte[]>(10000);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task _worker;
    private Parser _parser;
    private GoogleSheetsHandler _updater;

    public AlbionObserver(ICaptureDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));

        _worker = Task.Factory.StartNew(ProcessPayloads, TaskCreationOptions.LongRunning);
        _parser = new Parser(this);
        _updater = new GoogleSheetsHandler();
    }

    public void Start(string observingType = "offer")
    {
        this.observingType = observingType;
        _device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: 1000);

        try { _device.Filter = "udp port 5056"; } catch { /* ignore if not supported here */ }

        _device.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);

        _device.StartCapture();
        Console.WriteLine($"Started capture on {_device.Description ?? _device.Name}");
    }

    public void Stop()
    {
        _cts.Cancel();

        try { _device.StopCapture(); } catch { }
        _device.OnPacketArrival -= new PacketArrivalEventHandler(Device_OnPacketArrival);

        try { _device.Close(); } catch { }

        _payloadQueue.CompleteAdding();
        try { _worker.Wait(2000); } catch { }

        Console.WriteLine("Capture stopped.");
    }

    public Dictionary<string, int> GetRequestPrices()
    {
        try { _worker.Wait(300); } catch { }
        JArray data = new JArray(tempData);
        JObject resultObj = DataConverter.ConvertRawData(marketData: data, orderType: "request");
        tempData.Clear();
        return resultObj.Properties().ToDictionary(p => p.Name, p => (int)p.Value);
    }

    public void ResetTempData(string cityName = "Caerleon")
    {
        int timeWait = cityName == "Caerleon" ? 100 : 500;
        try { _worker.Wait(timeWait); } catch { }
        JArray data = new JArray(tempData);
        JObject resultObj = DataConverter.ConvertRawData(marketData: data, orderType: observingType);
        Dictionary<string, int> marketData = resultObj.Properties().ToDictionary(p => p.Name, p => (int)p.Value);

        _updater.UpdateGoogleSheet(marketData: marketData, cityName: cityName);
        tempData.Clear();
    }

    private void Device_OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            var rawPacket = e.GetPacket();

            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var udp = packet.Extract<UdpPacket>();
            if (udp == null) return;

            if (udp.SourcePort != 5056 && udp.DestinationPort != 5056) return;

            var payload = udp.PayloadData;
            if (payload == null || payload.Length == 0) return;

            _payloadQueue.TryAdd(payload);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Packet handler error: {ex.Message}");
        }
    }

    private void ProcessPayloads()
    {
        try
        {
            foreach (var payload in _payloadQueue.GetConsumingEnumerable(_cts.Token))
            {
                //_parser.ReceivePacket(payload);
                _parser.ReceivePacket(payload);
            }
        }
        catch (OperationCanceledException) { }
    }
}
