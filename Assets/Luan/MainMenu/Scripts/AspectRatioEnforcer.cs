using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Luan.MainMenu
{
    /// <summary>
    /// Khóa cứng tỉ lệ khung hình 16:9 (Letterbox / Pillarbox).
    /// Đảm bảo game hiển thị chính xác 100% y hệt như trong Unity Editor trên MỌI điện thoại (Android, iPhone) và iPad.
    /// Giữ nguyên tuyệt đối vị trí các nút, khoảng cách và độ sắc nét ban đầu.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("UI/Aspect Ratio Enforcer")]
    public class AspectRatioEnforcer : MonoBehaviour
    {
        [Tooltip("Tỉ lệ khung hình mục tiêu (Chuẩn 16:9 = 1.777778)")]
        [SerializeField] private float targetAspect = 16f / 9f;

        [Tooltip("Tự động gán Canvas trong Scene vào Camera này")]
        [SerializeField] private bool autoAssignCanvas = true;

        private Camera _cam;
        private Camera _bgCam;
        private float _lastWidth;
        private float _lastHeight;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            CreateBackgroundCamera();
            ApplyLetterbox();
            SetupCanvas();
        }

        private void Start()
        {
            ApplyLetterbox();
            SetupCanvas();
        }

        private void OnEnable()
        {
            _cam = GetComponent<Camera>();
            CreateBackgroundCamera();
            ApplyLetterbox();
            SetupCanvas();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= OnEditorUpdate;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                {
                    ApplyLetterbox();
                    SetupCanvas();
                }
            }
        }

        private void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (camera == _cam)
            {
                if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                {
                    ApplyLetterbox();
                    SetupCanvas();
                }
            }
        }
#endif

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                ApplyLetterbox();
                SetupCanvas();
            }
        }

        private void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            ApplyLetterbox();
        }

        public void ApplyLetterbox()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (_lastWidth <= 0 || _lastHeight <= 0) return;

            float windowAspect = _lastWidth / _lastHeight;
            float scaleHeight = windowAspect / targetAspect;

            Rect rect = _cam.rect;

            if (scaleHeight < 1.0f)
            {
                // Màn hình vuông hơn 16:9 (ví dụ iPad 4:3) -> Viền đen trên & dưới (Letterbox)
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) * 0.5f;
            }
            else
            {
                // Màn hình dài hơn 16:9 (ví dụ Android/iPhone 20:9) -> Viền đen 2 bên trái & phải (Pillarbox)
                float scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) * 0.5f;
                rect.y = 0;
            }

            _cam.rect = rect;
        }

        private void CreateBackgroundCamera()
        {
            if (_cam == null) _cam = GetComponent<Camera>();

            Transform bgCamTransform = transform.Find("_LetterboxBackgroundCamera");
            if (bgCamTransform != null)
            {
                _bgCam = bgCamTransform.GetComponent<Camera>();
            }

            if (_bgCam == null)
            {
                GameObject bgCamObj = new GameObject("_LetterboxBackgroundCamera");
                bgCamObj.hideFlags = HideFlags.DontSave;
                bgCamObj.transform.SetParent(transform);
                bgCamObj.transform.localPosition = Vector3.zero;
                bgCamObj.transform.localRotation = Quaternion.identity;

                _bgCam = bgCamObj.AddComponent<Camera>();
                _bgCam.depth = _cam.depth - 10;
                _bgCam.cullingMask = 0; // Không render bất kỳ object nào
                _bgCam.clearFlags = CameraClearFlags.SolidColor;
                _bgCam.backgroundColor = Color.black;
                _bgCam.rect = new Rect(0, 0, 1, 1);
            }
        }

        private void SetupCanvas()
        {
            if (!autoAssignCanvas) return;
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            float windowAspect = (_lastHeight > 0) ? (_lastWidth / _lastHeight) : (16f / 9f);
            float scaleHeight = windowAspect / targetAspect;

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                {
                    if (c.renderMode != RenderMode.ScreenSpaceCamera)
                    {
                        c.renderMode = RenderMode.ScreenSpaceCamera;
                    }
                    if (c.worldCamera != _cam)
                    {
                        c.worldCamera = _cam;
                    }
                    c.planeDistance = 100f;

                    CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                    if (scaler != null)
                    {
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        // Khóa tỉ lệ để Canvas luôn đạt chuẩn 1920x1080
                        scaler.matchWidthOrHeight = (scaleHeight < 1.0f) ? 0f : 1f;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_bgCam != null && _bgCam.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(_bgCam.gameObject);
                else
                    DestroyImmediate(_bgCam.gameObject);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitRuntimeAutoEnforce()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                EnforceOnMainCamera();
            };
            EnforceOnMainCamera();
        }

        private static void EnforceOnMainCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (!mainCam.TryGetComponent<AspectRatioEnforcer>(out _))
                {
                    mainCam.gameObject.AddComponent<AspectRatioEnforcer>();
                }
            }
        }
    }
}
