using BepInEx;
using BepInEx.Logging;
using KEEPTHEMOPEN.Patches;

namespace KEEPTHEMOPEN
{
    [BepInPlugin("com.BBkinha.KEEPTHEMOPEN", "KEEP THEM OPEN!", "1.0.0")]
    [BepInDependency("com.SPT.core", "4.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;

            Logger.LogInfo("KEEP THE DAMN CONTAINER OPEN, loaded");

            new DenyAutoClosePatch().Enable();
            new AllowSearchPatch().Enable();
        }
    }
}