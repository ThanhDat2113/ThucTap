using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateChibiAnimationClips
{
    [MenuItem("Tools/Create Chibi Boss Clips")]
    public static void CreateAll()
    {
        CreateBoss("Velvet", "Assets/Phu/Sprite asset/Velvet Chibi Animations");
        CreateBoss("Brax", "Assets/Phu/Sprite asset/Brax Chibi Animations");
        CreateBoss("Maestro", "Assets/Phu/Sprite asset/Maestro Chibi Animations");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created chibi Dance, Idle and Miss clips for Boss 3-5.");
    }

    static void CreateBoss(string name, string root)
    {
        string clipDir = root + "/Clips";
        Directory.CreateDirectory(clipDir);
        CreateClip(name + "_Chibi_Dance", root + "/Dance/Frames", 120, 60f, true, clipDir);
        CreateClip(name + "_Chibi_Idle", root + "/Idle/Frames", 24, 20f, true, clipDir);
        CreateClip(name + "_Chibi_Miss", root + "/Miss/Frames", 24, 24f, false, clipDir);
    }

    static void CreateClip(string clipName, string folder, int count, float fps, bool loop, string clipDir)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < count; i++)
        {
            string path = $"{folder}/{(folder.Contains("Dance") ? "dance" : folder.Contains("Idle") ? "idle" : "miss")}_{i + 1:000}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 350;
                importer.SaveAndReimport();
            }
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = AssetDatabase.LoadAssetAtPath<Sprite>(path) };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        if (loop) {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        AssetDatabase.CreateAsset(clip, clipDir + "/" + clipName + ".anim");
    }
}
