using System.Data;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json.Linq;

class GoogleSheetsHandler
{
    private Dictionary<string, string>? _itemsNaming;
    private GoogleCredential _credential;
    private SheetsService _service;
    private string _sheetId;
    private string _sheetName;
    public GoogleSheetsHandler(
        string applicationName = "AlbionMarketBotDB",
        string sheetId = "1aQaE_pVeMpvxLUEIBhQQlgrqF4fyn1wwLGDtgXQrHQ0",
        string sheetName = "Prices Caerleon")
    {
        _credential = GoogleCredential.FromFile("items-prices-albion-credentials.json").CreateScoped(SheetsService.Scope.Spreadsheets);
        _service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = applicationName,
        });
        _sheetId = sheetId;
        _sheetName = sheetName;

        string itemsNamingJson = File.ReadAllText("items_naming.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _itemsNaming = JsonSerializer.Deserialize<Dictionary<string, string>>(itemsNamingJson, options);
    }

    public void UpdateGoogleSheet(Dictionary<string, int> marketData = null, string cityName = "Caerleon")
    {
        _sheetName = $"Prices {cityName}";

        var spreadSheet = _service.Spreadsheets.Get(_sheetId).Execute();

        ValueRange valuesResponse = _service.Spreadsheets.Values.Get(_sheetId, $"{_sheetName}!A1:BI").Execute();
        IList<IList<object>>? rows = valuesResponse.Values;

        int rowCount = rows.Count;
        int maxColumns = rows.Max(r => r.Count);
        var matrix = new List<IList<object>>(maxColumns);

        for (int r = 0; r < rowCount; r++)
        {
            var emptyRow = Enumerable.Repeat<object>("", maxColumns).ToList();
            //Console.WriteLine(emptyRow.Count);
            matrix.Add(emptyRow);
        }

        matrix[0][0] = rows[0][0]?.ToString() ?? "";
        matrix[1][0] = DateTime.UtcNow;

        //loop through item categories
        for (int col = 1; col < maxColumns; col += 2)
        {
            matrix[0][col] = rows[0][col].ToString();
            matrix[0][col + 1] = rows[0][col + 1].ToString();

            //loop through items in category
            for (int r = 1; r < rowCount; r++)
            {
                //Console.WriteLine($"{r} -- {col} -- {col + 1}");
                string cellValue = (col < rows[r].Count) ? (rows[r][col]?.ToString() ?? "") : "";
                string nextCellValue = (col + 1 < rows[r].Count) ? (rows[r][col + 1]?.ToString() ?? "") : "";
                int oldItemPrice;
                
                try
                {
                    oldItemPrice = int.Parse(nextCellValue);
                    int enchantmentLevelSplit = cellValue.LastIndexOf('_');
                    int TierSplit = cellValue.LastIndexOf('_', enchantmentLevelSplit - 1);

                    string itemEnchantmentLevel = cellValue.Substring(enchantmentLevelSplit + 1);
                    string itemTier = cellValue.Substring(TierSplit + 1, enchantmentLevelSplit - TierSplit - 1);
                    string itemName = cellValue.Substring(0, TierSplit);
                    string itemDataBaseName = $"T{itemTier}{_itemsNaming[itemName]}{((int.Parse(itemEnchantmentLevel) > 0) ? $"@{itemEnchantmentLevel}" : "")}";

                    nextCellValue = marketData[itemDataBaseName].ToString();

                    matrix[r][col] = cellValue;
                    matrix[r][col + 1] = nextCellValue;
                }
                catch
                {
                    matrix[r][col] = cellValue;
                    matrix[r][col + 1] = nextCellValue;
                }
            }
        }

        //Console.WriteLine($"{maxColumns}, {matrix.Count}");

        ValueRange valuesToUpdate = new ValueRange { Range = $"{_sheetName}!A1:BI", Values = matrix };
        var batchUpdate = new BatchUpdateValuesRequest
        {
            Data = new List<ValueRange> { valuesToUpdate },
            ValueInputOption = "RAW"
        };

        var batchResult = _service.Spreadsheets.Values.BatchUpdate(batchUpdate, _sheetId).Execute();

        Console.WriteLine($"Rectangular batch updated: {batchResult.TotalUpdatedCells} cells.");
    }

    public void UpdateLocalDataFromGoogleSheets(string cityName = "Caerleon")
    {
        _sheetName = $"Prices {cityName}";
        Dictionary<string, int> marketData = new Dictionary<string, int>();
        var spreadSheet = _service.Spreadsheets.Get(_sheetId).Execute();

        ValueRange valuesResponse = _service.Spreadsheets.Values.Get(_sheetId, $"{_sheetName}!A1:BI").Execute();
        IList<IList<object>>? rows = valuesResponse.Values;

        int rowCount = rows.Count;
        int maxColumns = rows.Max(r => r.Count);

        for (int col = 1; col < maxColumns; col += 2)
        {
            //loop through items in category
            for (int r = 1; r < rowCount; r++)
            {
                //Console.WriteLine($"{r} -- {col} -- {col + 1}");
                string cellValue = (col < rows[r].Count) ? (rows[r][col]?.ToString() ?? "") : "";
                string nextCellValue = (col + 1 < rows[r].Count) ? (rows[r][col + 1]?.ToString() ?? "") : "";

                if (cellValue != "" && nextCellValue != "")
                {
                    int itemPrice = int.Parse(nextCellValue);

                    int enchantmentLevelSplit = cellValue.LastIndexOf('_');
                    int TierSplit = cellValue.LastIndexOf('_', enchantmentLevelSplit - 1);

                    string itemEnchantmentLevel = cellValue.Substring(enchantmentLevelSplit + 1);
                    string itemTier = cellValue.Substring(TierSplit + 1, enchantmentLevelSplit - TierSplit - 1);
                    string itemName = cellValue.Substring(0, TierSplit);

                    string itemDataBaseName = _itemsNaming[itemName];
                    string itemEnchantString = int.Parse(itemEnchantmentLevel) > 0 ? $"@{itemEnchantmentLevel}" : "";
                    string itemPriceKey = $"T{itemTier}{itemDataBaseName}{itemEnchantString}";

                    marketData[itemPriceKey] = itemPrice;
                }
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(marketData, options);
        File.WriteAllText("max_prices_by_item.json", json);
    }
}