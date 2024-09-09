namespace Winch.BepInExPlugin;

using BepInEx;
using BepInEx.Logging;
using Core;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null;

    private void Awake()
    {
        // Plugin startup logic
        Log = Logger;
        Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        WinchCore.Initialize();
    }
}