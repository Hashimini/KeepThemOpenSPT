using HarmonyLib;
using EFT.Interactive;
using System.Reflection;
using SPT.Reflection.Patching;

namespace KEEPTHEMOPEN.Patches
{
    public class DenyAutoClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        { return AccessTools.Method(typeof(LootableContainer), "Close"); }

        [PatchPrefix]
        static bool Prefix()
        {
            // Allow the EFT to close
            if (SharedState.AllowNextClose)
            {
                SharedState.AllowNextClose = false;
                return true;
            }

            // Deny EFT to close the container(s)
            return false;
        }
    }
}