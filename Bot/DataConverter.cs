using Newtonsoft.Json.Linq;

namespace PhotonPackageParser;

public static class DataConverter
{
    public static void ConvertRawData()
    {
        long conversionFactor = 10000;
        
        string fileName = "out.json";
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        
        JArray marketData = JArray.Parse(File.ReadAllText(path));

        var maxPerItem = marketData
            .Where(tok => tok["ItemTypeId"] != null && tok["UnitPriceSilver"] != null && (int)tok["QualityLevel"] < 4)
            .GroupBy(tok => (string)tok["ItemTypeId"])
            .ToDictionary(
                g => g.Key,
                g => {
                    long maxRaw = g
                        .Select(x => {
                            var v = x["UnitPriceSilver"];
                            if (v == null) return 0L;
                            try { return (long) v; } catch {}
                            try { return Convert.ToInt64((string)v); } catch {}
                            try { return Convert.ToInt64((double)v); } catch {}
                            return 0L;
                        })
                        .Max();
                    return maxRaw / conversionFactor;
                }
            );

        // Convert to JObject for pretty JSON output
        var resultObj = JObject.FromObject(maxPerItem);

        File.WriteAllText("max_prices_by_item.json", resultObj.ToString());
    }
}