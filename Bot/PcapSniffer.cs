// using directives
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SharpPcap;
using SharpPcap.LibPcap;   // for LibPcapLiveDeviceList
using PacketDotNet;
using PhotonPackageParser;

public class CaptureManager : IDisposable
{
    private readonly ICaptureDevice _device;
    private readonly BlockingCollection<byte[]> _payloadQueue = new BlockingCollection<byte[]>(10000);
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task _worker;
    private ExampleParser _parser;

    public CaptureManager(ICaptureDevice device, string outputPcapFile)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));

        // start worker to process UDP payloads off capture thread
        _worker = Task.Factory.StartNew(ProcessPayloads, TaskCreationOptions.LongRunning);
        _parser = new ExampleParser();
    }

    public void Start()
    {
        // open writer BEFORE starting capture (matches CreatingCaptureFile example)

        // open device in promiscuous mode (and set options if desired)
        _device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: 1000);

        // set BPF filter
        try { _device.Filter = "udp port 5056"; } catch { /* ignore if not supported here */ }

        // NOTE: subscribe using the new PacketCapture handler type
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

    // <-- NEW handler signature using PacketCapture (not CaptureEventArgs) -->
    private void Device_OnPacketArrival(object sender, PacketCapture e)
    {
        try
        {
            // get the raw packet (same data as old RawCapture)
            var rawPacket = e.GetPacket();

            // write raw packet to pcap file quickly

            // parse for UDP payload
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var udp = packet.Extract<UdpPacket>();
            if (udp == null) return;

            // optional: restrict to port 5056
            if (udp.SourcePort != 5056 && udp.DestinationPort != 5056) return;

            var payload = udp.PayloadData;
            if (payload == null || payload.Length == 0) return;

            // enqueue for background processing; best-effort (non-blocking)
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
                // TODO: feed to Photon parser (reflection or direct call)
                //TryExtractJsonAndPrint(payload);
                _parser.ReceivePacket(payload);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void TryExtractJsonAndPrint(byte[] data)
    {
        string text;
        try { text = Encoding.UTF8.GetString(data); } catch { text = Encoding.ASCII.GetString(data); }
        // naive JSON extraction (same as before) - or plug your Photon parser here
        int idx = 0;
        while (idx < text.Length)
        {
            int start = text.IndexOf('{', idx);
            if (start < 0) break;
            int depth = 0; bool inString = false;
            for (int j = start; j < text.Length; j++)
            {
                char c = text[j];
                if (c == '"' && (j == 0 || text[j - 1] != '\\')) inString = !inString;
                if (!inString)
                {
                    if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            var candidate = text.Substring(start, j - start + 1);
                            Console.WriteLine("[JSON] " + (candidate.Length > 300 ? candidate.Substring(0, 300) + "..." : candidate));
                            idx = j + 1;
                            break;
                        }
                    }
                }
                if (j == text.Length - 1) idx = start + 1;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        try { _device?.Close(); } catch { }
    }
}
