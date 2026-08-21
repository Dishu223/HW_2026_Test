#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// Editor utility to create and assign the Fredoka TMP Font Asset across all UI elements with valid embedded sub-assets.
/// </summary>
[InitializeOnLoad]
public static class FredokaFontApplier
{
    private const string TTF_PATH = "Assets/_Game/Fredoka/static/Fredoka-Bold.ttf";
    private const string FONT_ASSET_PATH = "Assets/_Game/Fredoka/Fredoka-Bold SDF.asset";

    static FredokaFontApplier()
    {
        EditorApplication.delayCall += EnsureValidFontAsset;
    }

    [MenuItem("Tools/Doofus/Fix & Rebuild Fredoka Font Asset")]
    public static void EnsureValidFontAsset()
    {
        Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(TTF_PATH);
        if (ttfFont == null)
        {
            string[] guids = AssetDatabase.FindAssets("Fredoka-Bold t:Font");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ttfFont = AssetDatabase.LoadAssetAtPath<Font>(path);
            }
        }

        if (ttfFont == null)
        {
            Debug.LogWarning("[FredokaFontApplier] Could not locate Fredoka TTF font file.");
            return;
        }

        TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (existingAsset != null && existingAsset.material != null && existingAsset.atlasTexture != null)
        {
            // Font asset is already valid and has sub-assets
            return;
        }

        // Delete broken asset if needed
        if (existingAsset != null)
        {
            AssetDatabase.DeleteAsset(FONT_ASSET_PATH);
        }

        // Create complete TMP Font Asset with 90pt sampling and distance field render mode
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(ttfFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
        if (fontAsset == null)
        {
            Debug.LogError("[FredokaFontApplier] Failed to create TMP_FontAsset from TTF.");
            return;
        }

        fontAsset.name = "Fredoka-Bold SDF";
        AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);

        // Add Atlas Texture sub-asset
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D tex = fontAsset.atlasTextures[i];
                if (tex != null)
                {
                    tex.name = $"{fontAsset.name} Atlas";
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
        }

        // Add Material sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FredokaFontApplier] Successfully built Fredoka-Bold SDF with valid embedded Material and Atlas sub-assets!");
        ApplyToSceneTexts(fontAsset);
    }

    [MenuItem("Tools/Doofus/Apply Fredoka Font Everywhere")]
    public static void ApplyFredokaFontEverywhere()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (fontAsset == null || fontAsset.material == null)
        {
            EnsureValidFontAsset();
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        }

        if (fontAsset != null)
        {
            ApplyToSceneTexts(fontAsset);
        }
    }

    private static void ApplyToSceneTexts(TMP_FontAsset fontAsset)
    {
        int count = 0;
        TextMeshProUGUI[] uiTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in uiTexts)
        {
            if (txt != null)
            {
                Undo.RecordObject(txt, "Apply Fredoka Font");
                txt.font = fontAsset;
                EditorUtility.SetDirty(txt);
                count++;
            }
        }

        TextMeshPro[] worldTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in worldTexts)
        {
            if (txt != null)
            {
                Undo.RecordObject(txt, "Apply Fredoka Font");
                txt.font = fontAsset;
                EditorUtility.SetDirty(txt);
                count++;
            }
        }

        Debug.Log($"[FredokaFontApplier] Assigned valid Fredoka font to {count} TextMeshPro components in active scene!");
    }
}
#endif
