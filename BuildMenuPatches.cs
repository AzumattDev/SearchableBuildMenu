using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using HarmonyLib;
using SearchableBuildMenu.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SearchableBuildMenu;

[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
static class HudAwakePatch
{
    static void Postfix(Hud __instance)
    {
        // Attach the search input field to the m_pieceSelectionWindow and put it to the left of the m_pieceSelectionWindow, m_pieceSelectionWindow should be the parent
        SearchableBuildMenuPlugin.BuildSearchBox = Object.Instantiate(SearchableBuildMenuPlugin.Asset.LoadAsset<GameObject>("BuildPieceSearch"), __instance.m_pieceSelectionWindow.transform);
        SearchableBuildMenuPlugin.BuildSearchBox.name = "BuildPieceSearchBox";
        DragWindowCntrl.ApplyDragWindowCntrl(SearchableBuildMenuPlugin.BuildSearchBox);
        SearchableBuildMenuPlugin.BuildSearchInputField = SearchableBuildMenuPlugin.BuildSearchBox.GetComponentInChildren<TMP_InputField>();

        // Set the font of the placeholder and input text to be the same as the rest of the UI
        var font = __instance.m_pieceDescription.GetComponentInChildren<TMP_Text>().font;
        SearchableBuildMenuPlugin.BuildSearchInputField.textComponent.font = font;
        SearchableBuildMenuPlugin.BuildSearchInputField.placeholder.GetComponentInChildren<TMP_Text>().font = font;

        // Correct the position of the search box to the top left of the m_pieceSelectionWindow
        RectTransform searchBoxTransform = SearchableBuildMenuPlugin.BuildSearchBox.GetComponent<RectTransform>();

        searchBoxTransform.anchorMin = new Vector2(0.0f, 1.0f); // Top-left anchor
        searchBoxTransform.anchorMax = new Vector2(0.0f, 1.0f); // Top-left anchor
        searchBoxTransform.pivot = new Vector2(0.0f, 1.0f); // Pivot at top-left

        if (PlayerPrefs.HasKey("SearchableBuildMenuSearchBoxPosition"))
        {
            string savedPosition = PlayerPrefs.GetString("SearchableBuildMenuSearchBoxPosition");
            Vector2 loadedPosition = StringToVector2(savedPosition);
            searchBoxTransform.anchoredPosition = loadedPosition;
        }
        else
        {
            // Set to default position if not previously saved
            searchBoxTransform.anchoredPosition = new Vector2(-10.0f, 35.0f);
        }


        searchBoxTransform.sizeDelta = new Vector2(200.0f, 30.0f);

        SearchableBuildMenuPlugin.BuildSearchInputField.onValueChanged.AddListener(PieceTableUpdateAvailablePatch.OnSearchValueChanged);
    }

    internal static Vector2 StringToVector2(string s)
    {
        if (string.IsNullOrEmpty(s)) throw new ArgumentException("SearchableBuildMenuSearchBoxPosition Input string is null or empty.");
        string[] split = s.Substring(1, s.Length - 2).Split(',');
        if (split.Length != 2) throw new FormatException("SearchableBuildMenuSearchBoxPosition Input string was not in a correct format.");
        return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
    }
}

[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
static class PieceTableUpdateAvailablePatch
{
    static void Postfix(PieceTable __instance)
    {
        if (SearchableBuildMenuPlugin.BuildSearchInputField == null) return;
        string searchText = SearchableBuildMenuPlugin.BuildSearchInputField.text.ToLower();

        if (string.IsNullOrEmpty(searchText)) return;
        if (searchText.StartsWith("@", StringComparison.Ordinal))
        {
            string modIdentifier = searchText.Substring(1).ToLower(); // Extract the mod identifier without '@'

            for (int categoryIndex = 0; categoryIndex < __instance.m_availablePieces.Count; categoryIndex++)
            {
                __instance.m_availablePieces[categoryIndex] = __instance.m_availablePieces[categoryIndex]
                    .Where(piece =>
                    {
                        // Assuming each piece's prefab name can be used to trace back to its originating mod/assembly
                        string prefabNameLower = piece.gameObject.name.ToLowerInvariant();
                        Assembly? assembly = AssetLoadTracker.GetAssemblyForPrefab(prefabNameLower);

                        if (assembly != null)
                        {
                            string assemblyName = assembly.GetName().Name.ToLowerInvariant();
                            return assemblyName.Contains(modIdentifier);
                        }

                        return false; // If the piece doesn't belong to an assembly, it doesn't match the search
                    }).ToList();
            }
        }
        else if (searchText.StartsWith("!", StringComparison.Ordinal))
        {
            string searchQuery = searchText.Substring(1).ToLower(); // Extract the search query without '!'
            for (int categoryIndex = 0; categoryIndex < __instance.m_availablePieces.Count; categoryIndex++)
            {
                __instance.m_availablePieces[categoryIndex] = __instance.m_availablePieces[categoryIndex]
                    .Where(piece =>
                    {
                        // Check if any of the piece's resources match the search query.
                        return piece.m_resources.Any(resource =>
                        {
                            // Assuming the item exists and has a name.
                            string resourceName = Localization.instance.Localize(resource.m_resItem?.m_itemData?.m_shared?.m_name).ToLower();
                            // Check if the resource's name contains the search query.
                            return resourceName.Contains(searchQuery);
                        });
                    })
                    .ToList();
            }
        }
        else
        {
            for (int categoryIndex = 0; categoryIndex < __instance.m_availablePieces.Count; categoryIndex++)
            {
                __instance.m_availablePieces[categoryIndex] = __instance.m_availablePieces[categoryIndex]
                    .Where(piece => (piece.m_name.ToLower().Contains(searchText) || piece.m_description.ToLower().Contains(searchText) || Localization.instance.Localize(piece.m_name).ToLower().Contains(searchText) || Localization.instance.Localize(piece.m_description).ToLower().Contains(searchText)))
                    .ToList();
            }
        }
    }

    internal static void OnSearchValueChanged(string searchText)
    {
        Player p = Player.m_localPlayer;
        if (p == null) return;
        if (!string.IsNullOrEmpty(searchText))
        {
            p.UpdateAvailablePiecesList();
        }
        else
        {
            if (p.m_rightItem != null && p.m_rightItem.m_shared.m_buildPieces)
                p.SetPlaceMode(p.m_rightItem.m_shared.m_buildPieces);
            else
                p.SetPlaceMode((PieceTable)null);
        }
    }
}

[HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
static class TextInputIsVisiblePatch
{
    static void Postfix(TextInput __instance, ref bool __result)
    {
        if (SearchableBuildMenuPlugin.BuildSearchInputField.isFocused)
        {
            __result = true; // Just to prevent the player from moving around and shit
        }
    }
}