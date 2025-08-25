using System.Collections.Concurrent;
using SharpPcap;
using PacketDotNet;
using PhotonPackageParser;
using Newtonsoft.Json.Linq;

public class AlbionObserver
{
    private readonly ICaptureDevice _device;
    private readonly BlockingCollection<byte[]> _payloadQueue = new BlockingCollection<byte[]>(10000);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task _worker;
    private Parser _parser;
    public JArray tempData = new JArray();

    public AlbionObserver(ICaptureDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));

        _worker = Task.Factory.StartNew(ProcessPayloads, TaskCreationOptions.LongRunning);
        _parser = new Parser(this);
    }

    public void Start()
    {
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

        JObject resultObj = DataConverter.ConvertRawData(tempData);
        File.WriteAllText("max_prices_by_item.json", resultObj.ToString());

        Console.WriteLine("Capture stopped.");
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
                _parser.ReceivePacket(payload);
            }
        }
        catch (OperationCanceledException) { }
    }
}
