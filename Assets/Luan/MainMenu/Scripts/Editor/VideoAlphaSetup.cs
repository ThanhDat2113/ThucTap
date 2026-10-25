using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class VideoAlphaSetup
{
    private static readonly string[] VideoPaths = new string[]
    {
        "Assets/Luan/MainMenu/Video/Video/title_logo_xoaphong.webm",
        "Assets/Luan/MainMenu/Video/Video/MainMenuTT_xoaphong.webm",
        "Assets/Luan/MainMenu/Video/Video/Button_BatDau_Effect_XoaPhong.webm",
        "Assets/Luan/MainMenu/Video/Video/Button_CaiDat_Effect_XoaPhong.webm",
        "Assets/Luan/MainMenu/Video/Video/Button_ThanhTich_Effect_XoaPhong.webm"
    };

    private const string MaterialPath = "Assets/Luan/MainMenu/Shaders/TransparentVideoMat.mat";

    [MenuItem("Tools/Cấu hình Video Alpha cho Android (Transcode + Keep Alpha)")]
    public static void SetupVideoAlpha()
    {
        EditorUtility.DisplayProgressBar("Video Alpha Setup", "Đang kiểm tra và khởi tạo Material...", 0.1f);

        // 1. Tạo hoặc lấy Material TransparentVideoMat
        Material transMat = GetOrCreateTransparentMaterial();

        int count = 0;
        try
        {
            for (int i = 0; i < VideoPaths.Length; i++)
            {
                string path = VideoPaths[i];
                float progress = 0.2f + 0.6f * ((float)i / VideoPaths.Length);
                EditorUtility.DisplayProgressBar("Video Alpha Setup", $"Đang cấu hình: {System.IO.Path.GetFileName(path)}", progress);

                var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
                if (importer != null)
                {
                    importer.keepAlpha = true;

                    // Configure default settings
                    var defaultSettings = importer.defaultTargetSettings;
                    if (defaultSettings == null)
                    {
                        defaultSettings = new VideoImporterTargetSettings();
                    }
                    defaultSettings.enableTranscoding = true;
                    defaultSettings.codec = VideoCodec.VP8;
                    importer.defaultTargetSettings = defaultSettings;

                    // Configure Android settings
                    var androidSettings = importer.GetTargetSettings("Android");
                    if (androidSettings == null)
                    {
                        androidSettings = new VideoImporterTargetSettings();
                    }
                    androidSettings.enableTranscoding = true;
                    androidSettings.codec = VideoCodec.VP8;
                    importer.SetTargetSettings("Android", androidSettings);

                    importer.SaveAndReimport();
                    count++;
                    Debug.Log($"[VideoAlphaSetup] Đã bật Transcode & Keep Alpha cho: {path}");
                }
                else
                {
                    Debug.LogWarning($"[VideoAlphaSetup] Không tìm thấy file: {path}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VideoAlphaSetup] Lỗi trong quá trình cấu hình: {ex}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 2. Gán Material vào các RawImage trong Scene MainMenu nếu Scene đang mở
        ApplyMaterialToOpenSceneRawImages(transMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VideoAlphaSetup] Hoàn tất! Đã xử lý {count} video.");
        EditorUtility.DisplayDialog(
            "Video Alpha Setup Thành Công",
            $"Đã cấu hình thành công {count} video sang chế độ Transcode & Keep Alpha (Codec VP8)!\n\n" +
            "Đồng thời đã tạo Material 'TransparentVideoMat' và gắn vào các RawImage để loại bỏ hoàn toàn viền đen trên mọi máy Android.",
            "Tuyệt vời"
        );
    }

    private static Material GetOrCreateTransparentMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("UI/TransparentVideo");
            if (shader == null)
            {
                Debug.LogWarning("[VideoAlphaSetup] Chưa tìm thấy Shader 'UI/TransparentVideo', sử dụng Shader UI/Default");
                shader = Shader.Find("UI/Default");
            }

            mat = new Material(shader);
            mat.SetFloat("_BlackThreshold", 0.04f);
            mat.SetFloat("_Smoothness", 0.04f);

            // Đảm bảo thư mục tồn tại
            if (!AssetDatabase.IsValidFolder("Assets/Luan/MainMenu/Shaders"))
            {
                AssetDatabase.CreateFolder("Assets/Luan/MainMenu", "Shaders");
            }

            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VideoAlphaSetup] Đã tạo Material mới tại: {MaterialPath}");
        }
        return mat;
    }

    private static void ApplyMaterialToOpenSceneRawImages(Material mat)
    {
        if (mat == null) return;

        string[] targetRawImageNames = new string[]
        {
            "Title_logo_RawImage",
            "CharMain_RawImage",
            "ButtonBatDau_RawImage",
            "Button_CaiDat_RawImage",
            "Button_ThanhTich_RawImage"
        };

        int applied = 0;
        foreach (var rawImage in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var name in targetRawImageNames)
            {
                if (rawImage.gameObject.name == name)
                {
                    Undo.RecordObject(rawImage, "Apply Transparent Video Material");
                    rawImage.material = mat;
                    EditorUtility.SetDirty(rawImage);
                    applied++;
                    Debug.Log($"[VideoAlphaSetup] Đã gán TransparentVideoMat cho RawImage: {rawImage.gameObject.name}");
                    break;
                }
            }
        }

        if (applied > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
