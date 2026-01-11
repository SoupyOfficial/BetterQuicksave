using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace BetterQuicksave.Patches
{
    [HarmonyPatch(typeof(SaveHandler), "QuickSaveCurrentGame")]
    public class QuickSaveCurrentGamePatch
    {
        public static event Action OnQuicksave;

        private static bool Prefix(SaveHandler __instance)
        {
            // Replace the vanilla quicksave with our custom one
            var customSaveName = QuicksaveManager.GetNextQuicksaveName();

            // Call SaveAs directly with our custom name instead of letting QuickSaveCurrentGame run
            var saveAsMethod = typeof(SaveHandler).GetMethod("SaveAs", BindingFlags.Public | BindingFlags.Instance);
            saveAsMethod?.Invoke(__instance, new object[] { customSaveName });

            // Return false to prevent the original QuickSaveCurrentGame from executing
            return false;
        }

        private static void Postfix()
        {
            OnQuicksave?.Invoke();
        }
    }
}
