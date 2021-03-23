using GreylingHunt.Minigames;
using HarmonyLib;

namespace GreylingHunt.GameClasses
{
    [HarmonyPatch(typeof(EnemyHud), "ShowHud")]
    public static class ShowHudPatch
    {
        private static bool Prefix(Character c)
        {
            if (GameManager.Instance.GameState == GameState.InProgress)
            {
                //Don't show hider huds to seekers
                return false;
            }

            return true;
        }
    }

}