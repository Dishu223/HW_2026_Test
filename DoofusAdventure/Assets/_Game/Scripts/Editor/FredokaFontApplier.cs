#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// Editor utility to create and assign the Fredoka TMP Font Asset across all UI elements.
/// </summary>
public static class FredokaFontApplier
{
    private const string TTF_PATH = "Assets/_Game/Fredoka/static/Fredoka-Bold.ttf";
    private const string FONT_ASSET_PATH = "Assets/_Game/Fredoka/Fredoka-Bold SDF.asset";

    [MenuItem("Tools/Doofus/Apply Fredoka Font Everywhere")]
    public static void ApplyFredokaFontEverywhere()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);

        if (fontAsset == null)
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

            if (ttfFont != null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(ttfFont);
                AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log("[FredokaFontApplier] Successfully generated Fredoka-Bold SDF TMP Font Asset at " + FONT_ASSET_PATH);
            }
            else
            {
                Debug.LogWarning("[FredokaFontApplier] Could not locate Fredoka TTF font file.");
                return;
            }
        }

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

        Debug.Log($"[FredokaFontApplier] Assigned Fredoka font to {count} TextMeshPro components in the active scene!");
    }
}
#endif
