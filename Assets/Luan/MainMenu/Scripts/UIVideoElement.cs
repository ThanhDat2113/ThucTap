using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Luan.MainMenu
{
    /// <summary>
    /// Component hỗ trợ phát Video hoạt họa lặp (Seamless Loop Video) trực tiếp trên Unity UI Canvas.
    /// Tự động tạo RenderTexture chuẩn ARGB32 hỗ trợ độ trong suốt (Alpha Transparency) cho file .webm.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("UI/Luan MainMenu/UI Video Element")]
    public class UIVideoElement : MonoBehaviour
    {
        [Header("Video Settings")]
        [Tooltip("Kéo file .webm hoặc .mp4 trong thư mục Video vào đây")]
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool isLooping = true;
        [Tooltip("Bật để giữ độ trong suốt Alpha Channel (áp dụng tốt nhất cho .webm)")]
        [SerializeField] private bool transparentBackground = true;

        [Header("Playback")]
        [Range(0.2f, 3.0f)]
        [SerializeField] private float playbackSpeed = 1.0f;

        private RawImage _rawImage;
        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        public VideoClip VideoClip
        {
            get => videoClip;
            set
            {
                videoClip = value;
                SetupVideo();
            }
        }

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            SetupVideo();
        }

        private void OnEnable()
        {
            if (playOnAwake && _videoPlayer != null && _videoPlayer.clip != null)
            {
                _videoPlayer.Play();
            }
        }

        private void OnDisable()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
        }

        private void SetupVideo()
        {
            if (videoClip == null) return;

            int width = (int)videoClip.width;
            int height = (int)videoClip.height;
            if (width <= 0) width = 256;
            if (height <= 0) height = 256;

            // Dọn dẹp RenderTexture cũ nếu có
            CleanupRenderTexture();

            // Tạo RenderTexture hỗ trợ Alpha (ARGB32)
            RenderTextureFormat format = transparentBackground ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
            _renderTexture = new RenderTexture(width, height, 0, format)
            {
                name = $"RT_{gameObject.name}_{videoClip.name}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _renderTexture.Create();

            // Cấu hình RawImage
            _rawImage.texture = _renderTexture;
            _rawImage.color = Color.white;

            // Cấu hình VideoPlayer
            _videoPlayer.playOnAwake = playOnAwake;
            _videoPlayer.isLooping = isLooping;
            _videoPlayer.playbackSpeed = playbackSpeed;
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip = videoClip;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (playOnAwake && isActiveAndEnabled)
            {
                _videoPlayer.Play();
            }
        }

        public void Play()
        {
            if (_videoPlayer != null) _videoPlayer.Play();
        }

        public void Pause()
        {
            if (_videoPlayer != null) _videoPlayer.Pause();
        }

        public void Stop()
        {
            if (_videoPlayer != null) _videoPlayer.Stop();
        }

        public void SetSpeed(float speed)
        {
            playbackSpeed = Mathf.Max(0.1f, speed);
            if (_videoPlayer != null) _videoPlayer.playbackSpeed = playbackSpeed;
        }

        private void CleanupRenderTexture()
        {
            if (_renderTexture != null)
            {
                if (_videoPlayer != null && _videoPlayer.targetTexture == _renderTexture)
                {
                    _videoPlayer.targetTexture = null;
                }
                if (_rawImage != null && _rawImage.texture == _renderTexture)
                {
                    _rawImage.texture = null;
                }

                _renderTexture.Release();
                DestroyImmediate(_renderTexture);
                _renderTexture = null;
            }
        }

        private void OnDestroy()
        {
            CleanupRenderTexture();
        }
    }
}

