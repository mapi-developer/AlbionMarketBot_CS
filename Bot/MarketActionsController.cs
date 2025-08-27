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

        _sender.TypeText(text: searchTitle);
        _sender.KeyPress(WindowsInput.VirtualKeyCode.RETURN);
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
}