using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhotonPackageParser
{
    public class ExampleParser : PhotonParser
    {
        protected override void OnEvent(byte code, Dictionary<byte, object> parameters) { }

        protected override void OnRequest(byte operationCode, Dictionary<byte, object> parameters) { }

        protected override void OnResponse(byte operationCode, short returnCode, string debugMessage, Dictionary<byte, object> parameters)
        {
            foreach (KeyValuePair<byte, object> parameter in parameters)
            {
                if (parameter.Value.GetType() == typeof(string[]))
                {
                    var jArray = new JArray();

                    foreach (var jsonString in parameter.Value as string[])
                    {
                        if (string.IsNullOrWhiteSpace(jsonString)) continue;

                        // Parse the JSON string into a JObject
                        JObject obj;
                        try
                        {
                            obj = JObject.Parse(jsonString);
                        }
                        catch (JsonReaderException ex)
                        {
                            Console.WriteLine($"Skipping invalid JSON element: {ex.Message}");
                            continue;
                        }

                        // Add captured timestamp (ms since epoch)
                        obj["_captured_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // OPTIONAL: if you need to rescale prices (example: divide by 1000)
                        // long p = obj["UnitPriceSilver"].Value<long>();
                        // obj["UnitPriceSilver"] = p / 1000;

                        jArray.Add(obj);
                    }

                    string fileName = "out.json";
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    
                    JArray existing = new JArray();
                    var text = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        try { existing = JArray.Parse(text); }
                        catch (JsonReaderException)
                        {
                            Console.WriteLine("Existing file not a valid JSON array. Overwriting with merged array.");
                            existing = new JArray();
                        }
                    }

                    foreach (var it in jArray) existing.Add(it);
                    File.WriteAllText(path, existing.ToString(Formatting.Indented));
                }
                
            }
            Console.WriteLine($"On Response: {parameters}");
        }
    }
}