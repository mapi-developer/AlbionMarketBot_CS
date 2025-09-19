using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using SharpPcap;

public class OrderWriter
{
    public string cathOrdersType = "offer"; // "offer" or "request":

    private decimal _minimalProfitRateToOrder = 0.8m;

    private AlbionObserver _observer;
    private InputSender _sender;
    private MarketActionsController _marketController;
    private GoogleSheetsHandler _updater;

    private GoogleCredential _credential;
    private SheetsService _service;
    private string _sheetId;
    private string _sheetName;
    private Dictionary<string, string> _itemsNaming;
    private static List<string[]> _categoryValues = new List<string[]>
    {
        new[] {"All"},
        new[] {"Bow", "Crossbow", "Axe", "Dagger", "Hammer", "War Gloves", "Mace", "Quarterstaff", "Spear", "Sword", "Arcane Staff", "Cursed Staff", "Fire Staff", "Frost Staff", "Holy Staff", "Nature Staff", "Shapeshifter Staff"},
        new[] {"Cloth Armor", "Leather Armor", "Plate Armor"},
        new[] {"Cloth Helmet", "Leather Helmet", "Plate Helmet"},
        new[] {"Cloth Shoes", "Leather Shoes", "Plate Shoes"},
        new[] {"Off Mage", "Off Hunter", "Off Warrior"},
        new[] {"Bag", "Satchel of Insight"}
    };

    public OrderWriter(
        string applicationName = "AlbionMarketBotDB",
        string sheetId = "1aQaE_pVeMpvxLUEIBhQQlgrqF4fyn1wwLGDtgXQrHQ0",
        string sheetName = "ItemsList",
        decimal minimalProfitRateToOrder = 0.8m)
    {
        _minimalProfitRateToOrder = minimalProfitRateToOrder;
        _credential = GoogleCredential.FromFile("items-prices-albion-credentials.json").CreateScoped(SheetsService.Scope.Spreadsheets);
        _service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = applicationName,
        });
        _sheetId = sheetId;
        _sheetName = sheetName;

        _observer = new AlbionObserver(device: CaptureDeviceList.Instance[3]);
        _sender = new InputSender([2560, 1600]);
        _marketController = new MarketActionsController(_sender);
        _updater = new GoogleSheetsHandler();

        string itemsNamingJson = File.ReadAllText("items_naming.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _itemsNaming = JsonSerializer.Deserialize<Dictionary<string, string>>(itemsNamingJson, options);
    }

    public void MakeOrders(bool removeOldOrders, string cityName = "Caerleon", string[]? categories = null, string[]? except_categories = null, int[]? tiers = null, int[]? enchantments = null)
    {
        _updater.UpdateLocalDataFromGoogleSheets(cityName: cityName);
        _sender.SetForeground();

        if (removeOldOrders == true) RemoveOldOrders();

        string marketJson = File.ReadAllText("max_prices_by_item.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Dictionary<string, int> blackMarketData = JsonSerializer.Deserialize<Dictionary<string, int>>(marketJson, options);

        var spreadSheet = _service.Spreadsheets.Get(_sheetId).Execute();

        ValueRange valuesResponse = _service.Spreadsheets.Values.Get(_sheetId, $"{_sheetName}!A1:AD").Execute();
        IList<IList<object>>? rows = valuesResponse.Values;

        int rowCount = rows.Count;
        int maxColumns = rows.Max(r => r.Count);

        _marketController.ChangeTab("create_buy_order");
        _observer.Start(observingType: "request");

        for (int col = 0; col < maxColumns; col += 1)
        {
            string categoryName = (col < rows[0].Count) ? (rows[0][col]?.ToString() ?? "") : "";
            bool categoryToCheck = categories == null || (categoryName != "" && categories.Contains(categoryName));
            bool notExcluded = except_categories == null || (categoryName != "" && !except_categories.Contains(categoryName));
            if (categoryToCheck && notExcluded)
            {
                for (int r = 1; r < rowCount; r++)
                {
                    string cellValue = (col < rows[r].Count) ? (rows[r][col]?.ToString() ?? "") : "";
                    if (cellValue != "")
                    {
                        for (int tier = 4; tier < 9; tier++)
                        {
                            if (tiers == null || tiers.Contains(tier))
                            {
                                for (int enchantment = 0; enchantment < 4; enchantment++)
                                {
                                    if (enchantments == null || enchantments.Contains(enchantment))
                                    {
                                        _marketController.SearchItem(searchTitle: $"{cellValue} {tier} {enchantment}");
                                        _marketController.ChooseCategory(categoryValue: _categoryValues.FindIndex(arr => arr.Contains(categoryName)));
                                        _marketController.ClickButton(buttonTitle: "buy_order");

                                        Thread.Sleep(300);

                                        string itemDataBaseName = $"T{tier}{_itemsNaming[cellValue.ToLower()]}{((enchantment > 0) ? $"@{enchantment}" : "")}";
                                        int requestPrice = -1;
                                        try { requestPrice = _observer.GetRequestPrices()[itemDataBaseName]; } catch { }

                                        if (requestPrice == -1)
                                        {
                                            _marketController.ClickButton(buttonTitle: "close_order_popup");
                                            continue;
                                        }

                                        decimal profitRate = GetOrderProfitRate(blackMarketData[itemDataBaseName], requestPrice + 1);

                                        Console.WriteLine($"{cellValue}_{tier}_{enchantment} - {requestPrice} - {profitRate}");

                                        if (profitRate >= _minimalProfitRateToOrder)
                                        {
                                            int itemToBuyAmount = GetItemAmountToOrder(requestPrice);

                                            _marketController.ChangeItemAmountInOrder(itemAmount: itemToBuyAmount);
                                            _marketController.ClickButton("one_silver_more");
                                            _marketController.ClickButton("create_order");
                                            _marketController.ClickButton("crate_order_confirmation");
                                        }
                                        else
                                        {
                                            _marketController.ClickButton(buttonTitle: "close_order_popup");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        _observer.Stop();
    }

    private void RemoveOldOrders()
    {
        _marketController.ChangeTab("my_orders_tab");

        for (int i = 0; i < 10; i++)
        {
            for (int x = 0; x < 30; x++)
            {
                _marketController.ClickButton("cancel_order");
            }
            _marketController.ScrollUp();
        }
    }

    private decimal GetOrderProfitRate(int itemBlackMarketPrice, int itemOrderPrice)
    {
        return decimal.Round(((decimal)itemBlackMarketPrice - itemOrderPrice) / itemOrderPrice, 2);
    }

    private int GetItemAmountToOrder(int orederPrice)
    {
        int items_amount;

        switch (orederPrice)
        {
            case <= 2500:
                items_amount = 10;
                break;
            case <= 10000:
                items_amount = 8;
                break;
            case <= 20000:
                items_amount = 4;
                break;
            case <= 40000:
                items_amount = 3;
                break;
            case <= 60000:
                items_amount = 2;
                break;
            default:
                items_amount = 1;
                break;
        }

        return items_amount;
    }
}