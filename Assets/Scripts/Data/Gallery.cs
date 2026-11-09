using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// The player's saved drawings. Editor: UnitPreset assets in the project gallery folder (Workshop saves them, they ship with builds).
/// Builds: png layers + a small json with the name in the player's save folder (builds can't create assets)
/// </summary>
public static class Gallery
{
    public const string projectFolderPath = "Assets/ScrObjects/Gallery/";
    public const string projectTextureFolderPath = projectFolderPath + "Textures/";

    [System.Serializable]
    class SavedDrawing
    {
        public string unitName;
    }

    static string SaveFolderPath => Path.Combine(Application.persistentDataPath, "Gallery");
    static List<UnitPreset> savedDrawings; // drawings from the save folder, loaded once
    static readonly Dictionary<UnitPreset, string> savedFilePaths = new Dictionary<UnitPreset, string>(); // drawing -> its files without the ending

    /// <summary>
    /// Drawings the gallery manager shows and can rename/delete (editor: project gallery assets, builds: the save folder)
    /// </summary>
    public static List<UnitPreset> GetPlayerDrawings()
    {
#if UNITY_EDITOR
        List<UnitPreset> presets = new List<UnitPreset>();
        foreach (string guid in AssetDatabase.FindAssets("t:UnitPreset", new[] { projectFolderPath.TrimEnd('/') }))
        {
            presets.Add(AssetDatabase.LoadAssetAtPath<UnitPreset>(AssetDatabase.GUIDToAssetPath(guid)));
        }
        return presets;
#else
        return new List<UnitPreset>(GetSavedDrawings());
#endif
    }

    public static bool HasPlayerDrawings() => GetPlayerDrawings().Count > 0;

    /// <summary>
    /// Builds: drawings from the save folder
    /// </summary>
    public static List<UnitPreset> GetSavedDrawings()
    {
        if (savedDrawings != null) return savedDrawings;
        savedDrawings = new List<UnitPreset>();
        if (!Directory.Exists(SaveFolderPath)) return savedDrawings;

        foreach (string jsonPath in Directory.GetFiles(SaveFolderPath, "*.json"))
        {
            try
            {
                SavedDrawing data = JsonUtility.FromJson<SavedDrawing>(File.ReadAllText(jsonPath));
                string basePath = jsonPath.Substring(0, jsonPath.Length - ".json".Length);

                Texture2D[] layers = new Texture2D[2];
                for (int i = 0; i < layers.Length; i++)
                {
                    string layerPath = $"{basePath}_{i + 1}.png";
                    if (!File.Exists(layerPath)) continue;

                    layers[i] = new Texture2D(2, 2);
                    layers[i].LoadImage(File.ReadAllBytes(layerPath));
                }

                UnitPreset preset = CreateRuntimePreset(data.unitName, layers);
                savedDrawings.Add(preset);
                savedFilePaths[preset] = basePath;
            }
            catch (System.Exception e) { Debug.LogWarning("Gallery: could not load saved drawing " + jsonPath + ": " + e.Message); }
        }
        return savedDrawings;
    }

    /// <summary>
    /// Builds: saves the drawing into the save folder, returns the saved drawing (null if there was nothing to save)
    /// </summary>
    public static UnitPreset SaveToSaveFolder(string unitName, Texture2D[] layers)
    {
        if (layers == null) return null;

        Directory.CreateDirectory(SaveFolderPath);
        string basePath = Path.Combine(SaveFolderPath, GetFreeFileName(unitName, SaveFolderPath + "/", ".json"));

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null) File.WriteAllBytes($"{basePath}_{i + 1}.png", layers[i].EncodeToPNG());
        }
        WriteName(basePath, unitName);

        // adding it to the loaded drawings right away
        UnitPreset preset = CreateRuntimePreset(unitName, layers);
        GetSavedDrawings().Add(preset);
        savedFilePaths[preset] = basePath;
        Debug.Log("Gallery: saved drawing " + unitName + " to " + SaveFolderPath);
        return preset;
    }

    public static void Rename(UnitPreset preset, string newName)
    {
        preset.unitName = newName;

#if UNITY_EDITOR
        EditorUtility.SetDirty(preset);
        AssetDatabase.SaveAssets();
#else
        if (savedFilePaths.TryGetValue(preset, out string basePath)) WriteName(basePath, newName);
#endif
    }

    public static void Delete(UnitPreset preset)
    {
#if UNITY_EDITOR
        // the preset and its layer pngs
        foreach (Sprite sprite in preset.sprite)
        {
            if (sprite != null) AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(sprite));
        }
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(preset));
#else
        if (savedFilePaths.TryGetValue(preset, out string basePath))
        {
            File.Delete(basePath + ".json");
            for (int i = 0; i < preset.sprite.Length; i++) File.Delete($"{basePath}_{i + 1}.png");
            savedFilePaths.Remove(preset);
        }
        GetSavedDrawings().Remove(preset);
#endif
    }

    static void WriteName(string basePath, string unitName)
    {
        File.WriteAllText(basePath + ".json", JsonUtility.ToJson(new SavedDrawing { unitName = unitName }));
    }

    static UnitPreset CreateRuntimePreset(string unitName, Texture2D[] layers)
    {
        UnitPreset preset = ScriptableObject.CreateInstance<UnitPreset>();
        preset.unitName = unitName;
        for (int i = 0; i < layers.Length && i < preset.sprite.Length; i++)
        {
            if (layers[i] == null) continue;
            layers[i].filterMode = FilterMode.Point;
            preset.sprite[i] = Sprite.Create(layers[i], new Rect(0, 0, layers[i].width, layers[i].height), new Vector2(0.5f, 0.5f));
        }
        return preset;
    }

    /// <summary>
    /// Removes characters that can't be in a file name and adds a number if a file with that name already exists
    /// </summary>
    public static string GetFreeFileName(string cardName, string folderPath, string extension)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars()) cardName = cardName.Replace(invalidChar.ToString(), "");
        if (cardName.Trim() == "") cardName = "drawing";

        string freeName = cardName;
        int number = 2;
        while (File.Exists($"{folderPath}{freeName}{extension}")) freeName = cardName + " " + number++;
        return freeName;
    }
}
