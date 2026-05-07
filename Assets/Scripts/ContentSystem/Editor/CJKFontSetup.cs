using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Editor tool to fix "character with Unicode value \uXXXX was not found in LiberationSans SDF".
/// Copies a CJK system font into the project, creates a TMPro SDF font asset from it,
/// and adds it to TMP Settings fallback list.
///
/// Usage: Tools > Content System > Setup CJK Fallback Font
/// </summary>
public static class CJKFontSetup
{
    private const string MenuPath = "Tools/Content System/Setup CJK Fallback Font";
    private const string FontsDir = "Assets/TextMesh Pro/Fonts";
    private const string OutputDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    private const string FontFileName = "CJKFallback.ttf";
    private const string OutputAssetName = "CJKFallback SDF.asset";
    private static string FontFilePath => Path.Combine(FontsDir, FontFileName).Replace("\\", "/");
    private static string OutputPath => Path.Combine(OutputDir, OutputAssetName).Replace("\\", "/");

    // System CJK font paths ordered by preference
    private static readonly (string path, int faceIndex)[] SystemCJKFonts =
    {
        (@"C:\Windows\Fonts\msyh.ttf", 0),   // Microsoft YaHei
        (@"C:\Windows\Fonts\msyhbd.ttf", 0),  // Microsoft YaHei Bold
        (@"C:\Windows\Fonts\simhei.ttf", 0),  // SimHei
        (@"C:\Windows\Fonts\simsun.ttc", 0),  // SimSun
        (@"C:\Windows\Fonts\simkai.ttf", 0),  // KaiTi
        (@"C:\Windows\Fonts\SIMYOU.TTF", 0),  // SimYou (YouYuan)
        (@"C:\Windows\Fonts\Fangsong.ttf", 0), // FangSong
    };

    [MenuItem(MenuPath, false, 30)]
    public static void SetupCJKFallback()
    {
        // 0. Delete any broken existing CJK font asset (will be recreated)
        var existingCJK = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
        if (existingCJK != null)
        {
            bool valid = existingCJK.atlasTextures != null
                && existingCJK.atlasTextures.Length > 0
                && existingCJK.atlasTextures[0] != null;
            if (!valid)
            {
                Debug.Log("[CJKFontSetup] Deleting broken CJK font asset, will recreate.");
                RemoveFromFallback(existingCJK);
                AssetDatabase.DeleteAsset(OutputPath);
            }
            else if (IsInFallbackList(existingCJK))
            {
                Debug.Log("[CJKFontSetup] CJK fallback font already configured correctly.");
                return;
            }
            else
            {
                AddToFallback(existingCJK);
                return;
            }
        }

        // 1. Copy CJK font .ttf into project (if not already there)
        if (!File.Exists(FontFilePath))
        {
            string sourcePath = null;
            foreach (var (path, _) in SystemCJKFonts)
            {
                if (File.Exists(path))
                {
                    sourcePath = path;
                    Debug.Log($"[CJKFontSetup] Found system font: {path}");
                    break;
                }
            }

            if (sourcePath == null)
            {
                Debug.LogError(
                    "[CJKFontSetup] No CJK system font found.\n\n"
                    + "Manual fix:\n"
                    + "1. Download Noto Sans SC from https://fonts.google.com/noto/specimen/Noto+Sans+SC\n"
                    + "2. Drop the .ttf into Assets/TextMesh Pro/Fonts/ as 'CJKFallback.ttf'\n"
                    + "3. Re-run Tools > Content System > Setup CJK Fallback Font");
                return;
            }

            if (!Directory.Exists(FontsDir))
                Directory.CreateDirectory(FontsDir);

            File.Copy(sourcePath, FontFilePath, overwrite: false);
            AssetDatabase.ImportAsset(FontFilePath);
            Debug.Log($"[CJKFontSetup] Copied font to: {FontFilePath}");
        }
        else
        {
            // Ensure imported
            AssetDatabase.ImportAsset(FontFilePath);
        }

        // 2. Load the Unity Font asset
        Font unityFont = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
        if (unityFont == null)
        {
            Debug.LogError($"[CJKFontSetup] Failed to load font at {FontFilePath}. Check import settings.");
            return;
        }

        // Ensure font data is included for TMPro
        {
            var importer = AssetImporter.GetAtPath(FontFilePath) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.fontRenderingMode = FontRenderingMode.HintedSmooth;
                importer.SaveAndReimport();
                unityFont = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
            }
        }

        // 3. Create TMPro font asset with Dynamic population mode
        EditorUtility.DisplayProgressBar("CJK Font Setup", "Creating TMPro font asset...", 0.5f);
        TMP_FontAsset fontAsset = null;

        try
        {
            // Use the API that takes a Font object — creates with Dynamic mode, uses font file from project
            fontAsset = TMP_FontAsset.CreateFontAsset(
                font: unityFont,
                samplingPointSize: 36,
                atlasPadding: 5,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[CJKFontSetup] Font asset creation failed. The font file may not be compatible.");
                return;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 4. Save to asset database
        fontAsset.name = "CJKFallback SDF";
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        AssetDatabase.CreateAsset(fontAsset, OutputPath);

        // Attach material and textures as sub-assets (must happen AFTER CreateAsset)
        if (fontAsset.material != null && !AssetDatabase.IsSubAsset(fontAsset.material))
        {
            fontAsset.material.name = "CJKFallback SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, OutputPath);
        }

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex != null && !AssetDatabase.IsSubAsset(tex))
                {
                    tex.name = "CJKFallback SDF Atlas";
                    AssetDatabase.AddObjectToAsset(tex, OutputPath);
                }
            }
        }

        // 5. Add to TMP Settings fallback
        AddToFallback(fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CJKFontSetup] Done! Chinese text should now render correctly.\n"
            + "If errors persist, restart the Unity Editor to clear domain reload cache.");
    }

    private static bool IsInFallbackList(TMP_FontAsset fontAsset)
    {
        var settings = LoadTMPSettings();
        if (settings == null) return false;
        var so = new SerializedObject(settings);
        var fallback = so.FindProperty("m_fallbackFontAssets");
        for (int i = 0; i < fallback.arraySize; i++)
        {
            if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                return true;
        }
        return false;
    }

    private static void AddToFallback(TMP_FontAsset fontAsset)
    {
        var settings = LoadTMPSettings();
        if (settings == null)
        {
            Debug.LogError("[CJKFontSetup] TMP Settings asset not found.");
            return;
        }

        var so = new SerializedObject(settings);
        var fallback = so.FindProperty("m_fallbackFontAssets");

        for (int i = 0; i < fallback.arraySize; i++)
        {
            if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
            {
                Debug.Log("[CJKFontSetup] Font already in TMP Settings fallback list.");
                return;
            }
        }

        int idx = fallback.arraySize;
        fallback.InsertArrayElementAtIndex(idx);
        fallback.GetArrayElementAtIndex(idx).objectReferenceValue = fontAsset;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[CJKFontSetup] Added CJK font to TMP Settings fallback list.");
    }

    private static void RemoveFromFallback(TMP_FontAsset fontAsset)
    {
        var settings = LoadTMPSettings();
        if (settings == null) return;

        var so = new SerializedObject(settings);
        var fallback = so.FindProperty("m_fallbackFontAssets");
        for (int i = fallback.arraySize - 1; i >= 0; i--)
        {
            if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
            {
                fallback.DeleteArrayElementAtIndex(i);
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[CJKFontSetup] Removed broken CJK font from TMP Settings fallback.");
                return;
            }
        }
    }

    private static TMP_Settings LoadTMPSettings()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_Settings>(
            "Assets/TextMesh Pro/Resources/TMP Settings.asset");
    }
}
