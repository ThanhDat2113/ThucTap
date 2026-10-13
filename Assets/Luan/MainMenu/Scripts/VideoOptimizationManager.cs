using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Luan.MainMenu
{
    /// <summary>
    /// Tối ưu hóa hiệu năng video, tần số quét và loại bỏ hiện tượng chớp đen khi khởi động:
    /// 1. Tự động xóa sạch RenderTexture về trong suốt (Color.clear) trước khi video phát, không bao giờ bị chớp đen.
    /// 2. Fade-in mượt mà các RawImage video khi khung hình đầu tiên đã sẵn sàng.
    /// 3. Khớp FPS với tần số quét, giới hạn tải giao diện trên mobile.
    /// 4. Đảm bảo tất cả VideoPlayer luôn ở trạng thái Loop gốc mượt mà (Native Loop).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("UI/Luan MainMenu/Video Optimization Manager")]
    public class VideoOptimizationManager : MonoBehaviour
    {
        [Header("--- Cấu hình Tần Số Quét (FPS & Refresh Rate) ---")]
        [Tooltip("Tự động nhận diện tần số quét màn hình của thiết bị (60Hz, 90Hz, 120Hz, 144Hz...) để khóa FPS tương ứng, tránh khựng giật.")]
        [SerializeField] private bool matchScreenRefreshRate = true;

        [Tooltip("FPS trần tối đa khi tự động nhận diện (khuyên dùng 120 hoặc 144 để mượt mà nhất mà không quá nóng máy)")]
        [Range(60, 165)]
        [SerializeField] private int maxAllowedFPS = 144;

        [Tooltip("FPS thủ công cố định khi tắt chế độ tự động nhận diện")]
        [Range(30, 165)]
        [SerializeField] private int manualTargetFPS = 60;

        [Header("--- Cấu hình Hiệu Ứng Video ---")]
        [Tooltip("Thời gian fade-in mượt mà khi video bắt đầu (giây)")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float fadeInDuration = 0.2f;

        [Header("--- Android Playback ---")]
        [Tooltip("Bật để bỏ frame khi decoder chậm. Tắt để thử tránh seek/catch-up gây khựng trên Android; có thể làm video chậm hơn nếu thiết bị không theo kịp.")]
        [SerializeField] private bool androidSkipOnDrop = true;

        /// <summary>
        /// FPS mục tiêu hiện tại đang được áp dụng.
        /// </summary>
        public static int CurrentTargetFPS { get; private set; } = 60;

        /// <summary>
        /// Tần số quét màn hình phần cứng phát hiện được từ thiết bị.
        /// </summary>
        public static int DeviceRefreshRate { get; private set; } = 60;

        private VideoPlayer[] _videoPlayers;
        private readonly Dictionary<VideoPlayer, float> _stoppedAtEndSince = new Dictionary<VideoPlayer, float>();

        private void Awake()
        {
            _videoPlayers = FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ApplyOptimalFrameRate();
            ClearAllRenderTextures();
            EnsureVideoPlayersLoop();
        }

        private void Start()
        {
            StartCoroutine(PrepareAndFadeInVideos());
            StartCoroutine(CheckAndLoopVideos());
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            StartCoroutine(LogVideoProgress());
#endif
        }

        /// <summary>
        /// Bật native loop và áp dụng cấu hình bỏ frame cho từng nền tảng.
        /// </summary>
        private void EnsureVideoPlayersLoop()
        {
            VideoPlayer[] videoPlayers = _videoPlayers;
            foreach (var vp in videoPlayers)
            {
                if (vp == null || vp.gameObject.scene != gameObject.scene) continue;
                ConfigureFrameSkipping(vp);
                vp.prepareCompleted -= OnVideoPrepared;
                vp.prepareCompleted += OnVideoPrepared;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                vp.loopPointReached -= LogLoopPoint;
                vp.loopPointReached += LogLoopPoint;
#endif
                vp.isLooping = true;
                vp.audioOutputMode = VideoAudioOutputMode.None;
                vp.errorReceived -= OnVideoError;
                vp.errorReceived += OnVideoError;
            }
        }

        private void ConfigureFrameSkipping(VideoPlayer player)
        {
            if (!player.canSetSkipOnDrop) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            player.skipOnDrop = androidSkipOnDrop;
#else
            player.skipOnDrop = true;
#endif
        }

        private void OnVideoPrepared(VideoPlayer player)
        {
            // Một số backend chỉ cho thay đổi Skip On Drop sau khi Prepare hoàn tất.
            ConfigureFrameSkipping(player);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[VideoOptimizationManager] Prepared '{player.name}': " +
                $"{player.width}x{player.height}, {player.frameRate:F2} fps, " +
                $"length={player.length:F3}s, skipOnDrop={player.skipOnDrop}", player);
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void LogLoopPoint(VideoPlayer player)
        {
            Debug.Log($"[VideoLoopDiagnostic] END '{player.name}': " +
                $"frame={player.frame}/{player.frameCount}, time={player.time:F3}, " +
                $"playing={player.isPlaying}, paused={player.isPaused}", player);
        }

        private IEnumerator LogVideoProgress()
        {
            var wait = new WaitForSecondsRealtime(2f);
            // Chỉ ghi 30 giây đầu của bản Development để đối chiếu nhiều vòng.
            for (int sample = 0; sample < 15; sample++)
            {
                yield return wait;
                foreach (var player in _videoPlayers)
                {
                    if (player == null || player.gameObject.scene != gameObject.scene ||
                        !player.isActiveAndEnabled) continue;
                    Debug.Log($"[VideoLoopDiagnostic] SAMPLE '{player.name}': " +
                        $"frame={player.frame}/{player.frameCount}, time={player.time:F3}/{player.length:F3}, " +
                        $"playing={player.isPlaying}, paused={player.isPaused}, " +
                        $"prepared={player.isPrepared}, looping={player.isLooping}, skip={player.skipOnDrop}", player);
                }
            }
        }
#endif

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogError($"[VideoOptimizationManager] Video '{source.name}' " +
                $"(clip: {source.clip?.name}, platform: {Application.platform}): {message}", source);
        }

        // Giữ native loop; chỉ phục hồi player đã dừng ở cuối clip.
        // Không seek trong loopPointReached vì native loop cũng đang tua về đầu.
        private IEnumerator CheckAndLoopVideos()
        {
            var wait = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                yield return wait;
                VideoPlayer[] videoPlayers = _videoPlayers;
                foreach (var vp in videoPlayers)
                {
                    if (vp != null && vp.gameObject.scene == gameObject.scene &&
                        vp.isActiveAndEnabled && vp.isLooping && !vp.isPlaying &&
                        !vp.isPaused && vp.isPrepared && HasReachedEnd(vp))
                    {
                        if (!_stoppedAtEndSince.TryGetValue(vp, out float stoppedSince))
                        {
                            _stoppedAtEndSince[vp] = Time.realtimeSinceStartup;
                            continue;
                        }

                        // Cho native loop thời gian chuyển vòng, tránh reset decoder lúc chuyển tiếp.
                        if (Time.realtimeSinceStartup - stoppedSince < 1f) continue;

                        Debug.LogWarning($"[VideoOptimizationManager] Recover video stuck at end: {vp.name}", vp);
                        if (vp.canSetTime) vp.time = 0;
                        else vp.Stop();
                        vp.Play();
                        _stoppedAtEndSince.Remove(vp);
                    }
                    else if (vp != null)
                    {
                        _stoppedAtEndSince.Remove(vp);
                    }
                }
            }
        }

        private static bool HasReachedEnd(VideoPlayer player)
        {
            if (player.frameCount > 0 && player.frame >= 0)
            {
                return (ulong)player.frame >= player.frameCount - 1;
            }

            // Một số decoder không cung cấp frameCount; dùng thời gian gần cuối.
            double tolerance = player.frameRate > 0 ? 1.0 / player.frameRate : 0.04;
            return player.length > 0 && player.time >= player.length - tolerance;
        }

        private void OnDestroy()
        {
            VideoPlayer[] videoPlayers = _videoPlayers;
            if (videoPlayers == null) return;
            foreach (var vp in videoPlayers)
            {
                if (vp != null)
                {
                    vp.errorReceived -= OnVideoError;
                    vp.prepareCompleted -= OnVideoPrepared;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    vp.loopPointReached -= LogLoopPoint;
#endif
                }
            }
        }

        /// <summary>
        /// Dò refresh rate đang sử dụng trên mobile, các chế độ hỗ trợ trên desktop
        /// (Hỗ trợ Unity 6, Android Native Display Modes, iOS ProMotion, PC/Mac).
        /// </summary>
        public static int DetectDeviceRefreshRate()
        {
            double detectedHz = 60.0;

#if UNITY_2022_2_OR_NEWER
            // 1. Tần số quét của độ phân giải hiện tại
            if (Screen.currentResolution.refreshRateRatio.value > 0)
            {
                detectedHz = Screen.currentResolution.refreshRateRatio.value;
            }

            // 2. Tần số quét từ DisplayInfo cửa sổ chính
            try
            {
                var displayInfo = Screen.mainWindowDisplayInfo;
                if (displayInfo.refreshRate.value > detectedHz)
                {
                    detectedHz = displayInfo.refreshRate.value;
                }
            }
            catch { }

            // 3. Quét danh sách các chế độ hiển thị phần cứng hỗ trợ (Screen.resolutions)
            if (Screen.resolutions != null && Screen.resolutions.Length > 0)
            {
                foreach (var res in Screen.resolutions)
                {
                    if (res.refreshRateRatio.value > detectedHz)
                    {
                        detectedHz = res.refreshRateRatio.value;
                    }
                }
            }
#else
            if (Screen.currentResolution.refreshRate > 0)
            {
                detectedHz = Screen.currentResolution.refreshRate;
            }

            if (Screen.resolutions != null && Screen.resolutions.Length > 0)
            {
                foreach (var res in Screen.resolutions)
                {
                    if (res.refreshRate > detectedHz)
                    {
                        detectedHz = res.refreshRate;
                    }
                }
            }
#endif

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            detectedHz = Screen.currentResolution.refreshRateRatio.value;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            // 4. Đọc tần số quét đang sử dụng trên Android.
            // Đảm bảo nhận diện chính xác 90Hz, 120Hz, 144Hz trên Samsung, Xiaomi, ROG, Pixel...
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var windowManager = activity.Call<AndroidJavaObject>("getWindowManager"))
                using (var display = windowManager.Call<AndroidJavaObject>("getDefaultDisplay"))
                {
                    float currentRate = display.Call<float>("getRefreshRate");
                    if (currentRate > 0) detectedHz = currentRate;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[VideoOptimizationManager] Không thể đọc refresh rate qua Android Java API: " + ex.Message);
            }
#endif

            int finalHz = Mathf.RoundToInt((float)detectedHz);
            if (finalHz < 50) finalHz = 60;
            return finalHz;
        }

        /// <summary>
        /// Khóa FPS game khớp chính xác với tần số quét màn hình của điện thoại.
        /// </summary>
        public void ApplyOptimalFrameRate()
        {
            QualitySettings.vSyncCount = 0;

            DeviceRefreshRate = DetectDeviceRefreshRate();

            int targetFPS = matchScreenRefreshRate ? DeviceRefreshRate : manualTargetFPS;
            targetFPS = Mathf.Clamp(targetFPS, 30, maxAllowedFPS);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Dành ngân sách CPU/GPU cho sáu video; FPS phải chia hết refresh rate hiện tại.
            targetFPS = Mathf.Min(targetFPS, 60);
            int divisor = Mathf.Max(1, Mathf.CeilToInt((float)DeviceRefreshRate / targetFPS));
            targetFPS = Mathf.Max(1, Mathf.RoundToInt((float)DeviceRefreshRate / divisor));
#endif

            Application.targetFrameRate = targetFPS;
            CurrentTargetFPS = targetFPS;

            Debug.Log($"[VideoOptimizationManager] Tần số quét màn hình thiết bị: {DeviceRefreshRate}Hz -> Đã khóa Application.targetFrameRate = {CurrentTargetFPS} FPS");
        }

        /// <summary>
        /// Xóa sạch mọi RenderTexture về màu trong suốt tuyệt đối (Color.clear).
        /// Ngăn chặn 100% việc GPU hiển thị khung đen rác trước khi video kịp giải mã.
        /// </summary>
        public static void ClearAllRenderTextures()
        {
            RenderTexture[] allRTs = Resources.FindObjectsOfTypeAll<RenderTexture>();
            foreach (var rt in allRTs)
            {
                if (rt != null && rt.IsCreated())
                {
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    GL.Clear(true, true, Color.clear);
                    RenderTexture.active = prev;
                }
            }
        }



        /// <summary>
        /// Ẩn tạm thời các RawImage và fade-in mượt mà khi video đã sẵn sàng khung hình đầu tiên.
        /// </summary>
        private IEnumerator PrepareAndFadeInVideos()
        {
            RawImage[] rawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Dictionary<RawImage, VideoPlayer> pairs = new Dictionary<RawImage, VideoPlayer>();

            VideoPlayer[] videoPlayers = _videoPlayers;

            foreach (var raw in rawImages)
            {
                if (raw == null || raw.texture == null) continue;

                // Tìm VideoPlayer xuất ra texture này
                foreach (var vp in videoPlayers)
                {
                    if (vp != null && vp.targetTexture != null && vp.targetTexture == raw.texture)
                    {
                        pairs[raw] = vp;
                        // Ẩn tạm thời để không bị chớp đen
                        Color c = raw.color;
                        c.a = 0f;
                        raw.color = c;
                        break;
                    }
                }
            }

            // Chờ các VideoPlayer giải mã khung hình đầu tiên rồi fade-in nhẹ nhàng
            foreach (var pair in pairs)
            {
                StartCoroutine(FadeInWhenReady(pair.Key, pair.Value));
            }

            yield return null;
        }

        private IEnumerator FadeInWhenReady(RawImage raw, VideoPlayer vp)
        {
            if (raw == null || vp == null) yield break;

            // Chờ video bắt đầu chạy hoặc chuẩn bị xong
            float timeout = 2.0f;
            while (!vp.isPlaying && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Chờ thêm 1 frame để đảm bảo texture đã có dữ liệu hình ảnh
            yield return new WaitForEndOfFrame();

            // Fade-in mượt mà từ 0 -> 1
            float elapsed = 0f;
            Color c = raw.color;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (raw != null)
                {
                    c.a = Mathf.Clamp01(elapsed / fadeInDuration);
                    raw.color = c;
                }
                yield return null;
            }

            if (raw != null)
            {
                c.a = 1f;
                raw.color = c;
            }
        }
    }
}
