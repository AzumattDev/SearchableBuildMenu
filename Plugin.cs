using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using SearchableBuildMenu.Utilities;
using TMPro;
using UnityEngine;

namespace SearchableBuildMenu
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SearchableBuildMenuPlugin : BaseUnityPlugin
    {
        internal const string ModName = "SearchableBuildMenu";
        internal const string ModVersion = "1.0.1";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource SearchableBuildMenuLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        internal static AssetBundle Asset = null!;
        internal static GameObject BuildSearchBox = null!;
        internal static TMP_InputField BuildSearchInputField = null!;


        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            LoadAssets();
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
        }

        private static AssetBundle GetAssetBundleFromResources(string filename)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
            using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }

        public static void LoadAssets()
        {
            Asset = GetAssetBundleFromResources("buildpiecesearch");
        }

        private void Start()
        {
            AssetLoadTracker.MapPrefabsToBundles();
            AssetLoadTracker.MapBundlesToAssemblies();
        }
    }
}