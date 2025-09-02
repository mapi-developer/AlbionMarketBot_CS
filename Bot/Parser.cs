﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhotonPackageParser;

public class Parser : PhotonParser
{
    private AlbionObserver _observer;

    public Parser(AlbionObserver observer)
    {
        _observer = observer;
    }

    protected override void OnResponse(byte operationCode, short returnCode, string debugMessage, Dictionary<byte, object> parameters)
    {
        foreach (KeyValuePair<byte, object> parameter in parameters)
        {
            if (parameter.Value != null && parameter.Value.GetType() == typeof(string[]))
            {
                var jArray = new JArray();

                foreach (var jsonString in parameter.Value as string[])
                {
                    if (string.IsNullOrWhiteSpace(jsonString)) continue;

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

                    obj["_captured_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    jArray.Add(obj);
                }

                foreach (var it in jArray)
                {
                    _observer.tempData.Add(it);
                }
            }

        }
        //Console.WriteLine($"On Response: {parameters}");
    }

    protected override void OnEvent(byte code, Dictionary<byte, object> parameters) { }

    protected override void OnRequest(byte operationCode, Dictionary<byte, object> parameters) { }
}