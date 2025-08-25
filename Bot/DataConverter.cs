using Newtonsoft.Json.Linq;

public static class DataConverter
{
    public static JObject ConvertRawData(JArray marketData)
    {
        long conversionFactor = 10000;

        var maxPerItem = marketData
            .Where(tok =>
                tok["ItemTypeId"] != null &&
                tok["UnitPriceSilver"] != null &&
                tok["QualityLevel"] != null &&
                tok.Value<int?>("QualityLevel") < 4
            )
            .Select(tok => new
            {
                Key = tok["ItemTypeId"]?.Value<string>() ?? throw new InvalidOperationException("missing ItemTypeId"),
                Price = tok["UnitPriceSilver"]?.Value<long>() ?? 0L
            })
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.Max(r => r.Price) / conversionFactor);          

        // Convert to JObject
        return JObject.FromObject(maxPerItem);
    }
}