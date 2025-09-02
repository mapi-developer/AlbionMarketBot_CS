using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

static class ConvertRequest
{
    static public void DumpRequestToJsonlFile(byte op, Dictionary<byte, object> parameters, string path = "requests.jsonl")
    {
        var obj = new JObject
        {
            ["op"] = op,
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["params"] = ConvertParametersToJObject(parameters)
        };
        File.AppendAllText(path, obj.ToString(Formatting.None) + Environment.NewLine);
    }
    static private JObject ConvertParametersToJObject(Dictionary<byte, object> parameters)
    {
        var root = new JObject();
        foreach (var kv in parameters)
        {
            string keyName = kv.Key.ToString(); // or map to friendly name if you have one
            root[keyName] = ConvertToJToken(kv.Value);
        }
        return root;
    }

    static private JToken ConvertToJToken(object value)
    {
        if (value == null) return JValue.CreateNull();

        switch (value)
        {
            case string s: return new JValue(s);
            case string[] sa: return new JArray(sa.Select(x => (JToken)new JValue(x)));
            case byte[] bytes: return new JValue(Convert.ToBase64String(bytes)); // binary -> base64
            case IDictionary dict:
                var obj = new JObject();
                foreach (DictionaryEntry de in dict)
                {
                    var k = de.Key?.ToString() ?? "null";
                    obj[k] = ConvertToJToken(de.Value);
                }
                return obj;
            case IEnumerable<object> ie:
                return new JArray(ie.Select(ConvertToJToken));
            default:
                // fallback: try to serialize with JSON.NET
                try { return JToken.FromObject(value); }
                catch { return new JValue(value.ToString()); }
        }
    }
}