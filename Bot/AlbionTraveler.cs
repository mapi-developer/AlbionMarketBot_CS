using System.Text.Json;

public class Route
{
    public int[] Coordinates { get; set; }
    public int WaitTime { get; set; }
}

public class AlbionTraveler
{
    private InputSender _sender;
    private Dictionary<string, List<Route>> _travelPositions { get; set; }
     private Dictionary<string, int[]> _mousePositions;
    public AlbionTraveler()
    {
        _sender = new InputSender([2560, 1600]);
        _sender.SetForeground();

        string mousePositionsJson_ = File.ReadAllText("mouse_positions.json");
        var options_ = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _mousePositions = JsonSerializer.Deserialize<Dictionary<string, int[]>>(mousePositionsJson_, options_);

        string mousePositionsJson = File.ReadAllText("travaler_positions.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw = JsonSerializer.Deserialize<Dictionary<string, List<object[]>>>(mousePositionsJson, options);
        _travelPositions = new Dictionary<string, List<Route>>();
        foreach (var kvp in raw)
        {
            var routes = new List<Route>();

            foreach (var entry in kvp.Value)
            {
                var coords = JsonSerializer.Deserialize<int[]>(((JsonElement)entry[0]).GetRawText());
                int cost = ((JsonElement)entry[1]).GetInt32();

                routes.Add(new Route { Coordinates = coords, WaitTime = cost });
            }

            _travelPositions[kvp.Key] = routes;
        }
    }

    public void FromIslandToTravaler()
    {
        _sender.LeftClick([1084, 429]);
        Thread.Sleep(2000);
    }

    public void WalkTo(string destenition)
    {
        for (int i = 0; i < _travelPositions[destenition].Count; i++)
        {
            var route = _travelPositions[destenition][i];
            if (i == _travelPositions[destenition].Count - 1)
                _sender.LeftClick(route.Coordinates);
            else
                _sender.RightClick(route.Coordinates);
            Thread.Sleep(route.WaitTime);
        }
    }

    public void TeleportToIsland(string destination, bool fromSearch = false)
    {
        if (fromSearch)
        {
            _sender.LeftClick(_mousePositions["travel_travel_to_search"]);
            _sender.TypeText(destination);
            _sender.LeftClick(_mousePositions["travel_first_island_from_search"]);
        }
        else if (destination == "brecilien")
        {
            _sender.LeftClick(_mousePositions["travel_travel_to_search"]);
            _sender.LeftClick(_mousePositions[$"travel_{destination}_section"]);
        }
        else
        {
            _sender.LeftClick(_mousePositions[$"travel_{destination}_section"]);
        }

        _sender.LeftClick(_mousePositions["travel_buy_journey"]);
        Thread.Sleep(6000);
    }
}