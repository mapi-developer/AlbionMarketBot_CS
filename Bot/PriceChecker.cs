using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using SharpPcap;

class PriceChecker
{
    AlbionObserver _observer;
    InputSender _sender;
    MarketActionsController _marketController;

    private GoogleCredential _credential;
    private SheetsService _service;
    private string _sheetId;
    private string _sheetName;

    public PriceChecker(
        string applicationName = "AlbionMarketBotDB",
        string sheetId = "1aQaE_pVeMpvxLUEIBhQQlgrqF4fyn1wwLGDtgXQrHQ0",
        string sheetName = "ItemsList")
    {
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
    }

    public void UpdatePrices(string[]? categoriesToUpdate = null)
    {
        _sender.SetForeground();

        var spreadSheet = _service.Spreadsheets.Get(_sheetId).Execute();

        ValueRange valuesResponse = _service.Spreadsheets.Values.Get(_sheetId, $"{_sheetName}!A1:AD").Execute();
        IList<IList<object>>? rows = valuesResponse.Values;

        int rowCount = rows.Count;
        int maxColumns = rows.Max(r => r.Count);

        _observer.Start(observingType: "request");

        for (int col = 0; col < maxColumns; col += 1)
        {
            string categoryName = (col < rows[0].Count) ? (rows[0][col]?.ToString() ?? "") : "";
            if (categoriesToUpdate == null || (categoryName != "" && categoriesToUpdate.Contains(categoryName)))
            {
                for (int r = 1; r < rowCount; r++)
                {
                    string cellValue = (col < rows[r].Count) ? (rows[r][col]?.ToString() ?? "") : "";
                    if (cellValue != "")
                    {
                        _marketController.SearchItem(cellValue);
                        for (int i = 0; i < 10; i++)
                        {
                            _marketController.ChangePage();
                        }
                    }
                }

                _observer.ResetTempData();
                Thread.Sleep(1000);
            }
        }
    }
}