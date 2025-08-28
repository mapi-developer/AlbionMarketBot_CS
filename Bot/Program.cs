// PriceChecker checker = new PriceChecker();
// checker.UpdatePrices();

using SharpPcap;

// AlbionObserver sniffer = new AlbionObserver(device: CaptureDeviceList.Instance[3]);
// sniffer.Start("request");
// Console.ReadLine();
// sniffer.Stop();

OrderWriter orderWriter = new OrderWriter();
orderWriter.MakeOrders(categories: ["Bow"], tiers: [4, 5, 6], enchantments: [0, 1, 2]);
