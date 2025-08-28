using System.Text.Json;

class MarketActionsController
{
    private InputSender _sender;
    private Dictionary<string, int[]> _mousePositions;

    public MarketActionsController(InputSender sender)
    {
        _sender = sender;

        string mousePositionsJson = File.ReadAllText("mouse_positions.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _mousePositions = JsonSerializer.Deserialize<Dictionary<string, int[]>>(mousePositionsJson, options);
    }

    public void ResetFilters()
    {
        _sender.LeftClick(_mousePositions["market_category_reset"]);
    }

    public void ResetSearch()
    {
        _sender.LeftClick(_mousePositions["market_search_reset"]);
    }

    public void SearchItem(string searchTitle)
    {
        ResetSearch();
        ResetFilters();
        _sender.LeftClick(_mousePositions["market_search"]);

        _sender.TypeText(
            text: searchTitle,
            delayMs: searchTitle.Length < 8 ? 50 : 25
        );
        _sender.KeyPress(WindowsInput.VirtualKeyCode.RETURN);
    }

    public void ChooseCategory(int categoryValue)
    {
        _sender.LeftClick(_mousePositions["market_category"]);

        int addPixelsY = categoryValue * 40;
        _sender.LeftClick([_mousePositions["market_category_all"][0], _mousePositions["market_category_all"][1] + addPixelsY]);
    }

    public void ChooseTier(int tierValue = 0)
    {
        _sender.LeftClick(_mousePositions["market_tier"]);

        int addPixelsY = tierValue * 40;
        _sender.LeftClick([_mousePositions["market_tier_all"][0], _mousePositions["market_tier_all"][1] + addPixelsY]);
    }

    public void ChooseEnchantment(int enchantmentValue = 0)
    {
        _sender.LeftClick(_mousePositions["market_enchantment"]);

        int addPixelsY = enchantmentValue * 40;
        _sender.LeftClick([_mousePositions["market_enchantment_all"][0], _mousePositions["market_enchantment_all"][1] + addPixelsY]);
    }

    public void ChooseQuality(string qualityTitle)
    {
        Console.WriteLine($"Choose quality: {qualityTitle}");
    }

    public void ChangePage(string state = "next")
    {
        if (state == "next")
        {
            _sender.LeftClick(_mousePositions["market_next_page"]);
        }
        else
        {
            _sender.LeftClick(_mousePositions["market_previous_page"]);
        }
    }

    public void ChangeItemAmountInOrder(int itemAmount = 1)
    {
        ClickButton(buttonTitle: "change_amount");
        _sender.TypeText(
            text: itemAmount.ToString(),
            delayMs: itemAmount.ToString().Length < 8 ? 50 : 25
        );
    }

    public void ChangeTab(string tabTitle)
    {
        string mousePositionKey = $"market_tab_{tabTitle}";
        _sender.LeftClick(_mousePositions[mousePositionKey]);
    }

    public void ClickButton(string buttonTitle)
    {
        string mousePositionKey = $"market_button_{buttonTitle}";
        _sender.LeftClick(_mousePositions[mousePositionKey]);
    }
}