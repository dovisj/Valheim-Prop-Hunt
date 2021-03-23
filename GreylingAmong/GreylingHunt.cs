using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace GreylingHunt
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class GreylingHunt : BaseUnityPlugin
    {
        public const string ModGuid = AuthorName + "." + ModName;
        private const string AuthorName = "dovisj";
        private const string ModName = "GreylingHunt";
        private const string ModVer = "0.0.1";

        public static Harmony harmony = new Harmony("mod.GreylingHunt");

        internal static GreylingHunt Instance { get; private set; }

        /// <summary>
        /// Awake is called when the script instance is being loaded. 
        /// </summary>
        private void Awake()
        {
            Instance = this;
            ConfigEntry<bool> modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Log.Init(Logger);
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}