using GreylingHunt.Utils;
using HarmonyLib;

namespace GreylingHunt.GameClasses
{
    [HarmonyPatch(typeof(Chat), "InputText")]
    public static class ChatHandler
    {
        private static bool Prefix(ref Chat __instance)
        {
            string text = __instance.m_input.text; // Get the chat text
            ChatParser.ParsePlayerInput(text);
            return true;
        }
    }
}
