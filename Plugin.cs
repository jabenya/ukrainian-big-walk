using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace MyFirstPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;

        Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} is loaded!");

        AddComponent<TranslationLoader>();
    }
}

public class TranslationLoader : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(WaitForLocalization().WrapToIl2Cpp());
    }

    private IEnumerator WaitForLocalization()
    {
        Plugin.Log.LogInfo("Waiting for a LocalizationDataSet instance...");

        LocalizationDataSet dataSet = null;

        while (dataSet == null)
        {
            try
            {
                dataSet = LocalizationDataSet.instance;
            }
            catch
            {
                // wait a bit more
            }

            yield return null;
        }

        Plugin.Log.LogInfo($"Found a LocalizationDataSet with {dataSet.items.Length} entries.");


        var translations = LoadTranslations();
        int replaced = 0;

        foreach (var item in dataSet.items)
        {
            if (translations.TryGetValue(item.key, out string translatedText))
            {
                item.russian = translatedText;
                replaced++;
            }
        }

        LocalizedText.RefreshAll();

        Plugin.Log.LogInfo($"Applied {replaced}/{translations.Count} translations.");
    }

    private Dictionary<string, string> LoadTranslations()
    {
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string jsonPath = Path.Combine(directory, "translations.json");

        if (!File.Exists(jsonPath))
        {
            Plugin.Log.LogWarning($"Translation file not found: {jsonPath}");
            return new Dictionary<string, string>();
        }

        try
        {
            string json = File.ReadAllText(jsonPath);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to load translations.json: {ex}");
            return new Dictionary<string, string>();
        }
    }
}

