using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Luan.LuckyWheel
{
    public sealed class LuckyWheelController : MonoBehaviour
    {
        [Serializable]
        public class Prize
        {
            public string id;
            public string displayName;
            public Sprite icon;
            [Min(1)] public int amount = 1;
            [Min(0f)] public float weight = 1f;
        }

        [Header("Canvas references")]
        [SerializeField] private RectTransform wheel;
        [SerializeField] private Button spinCenterButton;
        [SerializeField] private Button spinX1Button;
        [SerializeField] private Button spinX10Button;
        [SerializeField] private LuckyWheelPurchaseUI purchaseUI;
        [SerializeField] private LuckyWheelEditablePanels panels;

        [Header("Spin animation")]
        [SerializeField, Min(0.25f)] private float spinDuration = 5.5f;
        [SerializeField, Min(1)] private int minimumTurns = 4;

        [Header("Prize configuration (clockwise, starting at the top)")]
        [SerializeField] private List<Prize> prizes = new List<Prize>();

        [Header("Gacha & Pity System (Bảo Hiểm)")]
        [Tooltip("Tỉ lệ trúng item hiếm / nhân vật huyền thoại ở mỗi lượt quay thường (0.05% = 0.0005).")]
        [SerializeField, Range(0f, 1f)] private float rareItemDropRate = 0.0005f;
        [Tooltip("Số lượt quay tối đa để kích hoạt bảo hiểm 100% nhận item hiếm (mặc định 80).")]
        [SerializeField, Min(1)] private int pityTarget = 80;
        [SerializeField] private string rareItemId = "rare_item";

        [Header("Runtime item layout")]
        [SerializeField] private float slotRadius = 245f;
        [SerializeField] private float iconOffsetY = 36f;
        [SerializeField] private float labelOffsetY = -36f;
        [SerializeField] private Vector2 iconSize = new Vector2(80f, 80f);
        [SerializeField] private Vector2 labelSize = new Vector2(82f, 32f);
        [SerializeField] private TMP_FontAsset itemFont;
        [SerializeField] private Material itemFontMaterial;

        [Header("Item orientation")]
        [Tooltip("Nếu bật: Các icon và chữ luôn giữ hướng thẳng đứng khi mâm quay (chuyển động kiểu cabin đu quay). Nếu tắt: Các icon và chữ quay dính liền theo nan quạt (căn giữa hoàn hảo, chuẩn phong cách game).")]
        [SerializeField] private bool keepItemsUpright = false;

        [Header("Audio settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip spinSound;
        [SerializeField] private AudioClip tickSound;
        [SerializeField] private AudioClip winSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip backClickSound;

        private AudioSource spinAudioSource;

        [Header("Events")]
        public UnityEvent<Prize> onSinglePrizeWon = new UnityEvent<Prize>();
        public UnityEvent<List<Prize>> onTenPrizesWon = new UnityEvent<List<Prize>>();
        public UnityEvent<string> onPrizeWon = new UnityEvent<string>();
        public UnityEvent onSpinSequenceFinished = new UnityEvent();

        public bool IsSpinning { get; private set; }
        public int CurrentSpinCount { get; private set; }
        public IReadOnlyList<Prize> Prizes => prizes;
        public float RareItemDropRate => rareItemDropRate;
        public int PityTarget => pityTarget;
        public string RareItemId => rareItemId;
        public bool KeepItemsUpright
        {
            get => keepItemsUpright;
            set
            {
                keepItemsUpright = value;
                if (wheel != null && !IsSpinning)
                    KeepItemViewsUpright(wheel.localEulerAngles.z);
            }
        }

        public void ApplyReadablePresentation()
        {
            spinDuration = 5.5f;
            minimumTurns = 4;
            slotRadius = 245f;
            iconOffsetY = 36f;
            labelOffsetY = -36f;
            iconSize = new Vector2(80f, 80f);
            labelSize = new Vector2(82f, 32f);
            keepItemsUpright = false;
        }

        private void Awake()
        {
            ResolveReferences();
            InitAudio();
            SetupAllButtonSounds();
            if (wheel != null) wheel.localRotation = Quaternion.identity;

            // Giữ nguyên thiết kế UI trong Scene của bạn, không tự ý xóa và tạo lại khi Play Mode!
            var runtimeItems = wheel != null ? wheel.Find("RuntimeItems") : null;
            if (runtimeItems == null || runtimeItems.childCount == 0)
            {
                RebuildItemViews();
            }

            spinCenterButton?.onClick.AddListener(SpinOnce);
            spinX1Button?.onClick.AddListener(SpinOnce);
            spinX10Button?.onClick.AddListener(SpinTenTimes);

            var backBtn = GameObject.Find("BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveListener(OnBackToMainMenuClicked);
                backBtn.onClick.AddListener(OnBackToMainMenuClicked);
            }

            BindBackButton();
        }

        private void Start()
        {
            EnsureAudioListener();
            InitAudio();
            SetupAllButtonSounds();

            // Đảm bảo nhạc nền BGM tiếp tục phát trong LuckyWheel theo đúng âm lượng đã lưu
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayBGM();
            }
        }

        private void OnDestroy()
        {
            spinCenterButton?.onClick.RemoveListener(SpinOnce);
            spinX1Button?.onClick.RemoveListener(SpinOnce);
            spinX10Button?.onClick.RemoveListener(SpinTenTimes);

            UnbindBackButton();
        }

        private void BindBackButton()
        {
            // Hỗ trợ cả nút mũi tên BackButton và badge chữ "QUAY LẠI" (PageTitle)
            var candidates = new[] { "BackButton", "PageTitle", "BtnBack", "ButtonBack", "QuayLai" };
            foreach (var name in candidates)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    var topBar = GameObject.Find("TopBar");
                    if (topBar != null)
                    {
                        var child = topBar.transform.Find(name);
                        if (child != null) go = child.gameObject;
                    }
                }

                if (go != null)
                {
                    var img = go.GetComponent<Image>();
                    if (img != null) img.raycastTarget = true;

                    var btn = go.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = go.AddComponent<Button>();
                        if (img != null)
                        {
                            btn.targetGraphic = img;
                            btn.transition = Selectable.Transition.ColorTint;
                            var colors = btn.colors;
                            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
                            btn.colors = colors;
                        }
                    }

                    btn.onClick.RemoveListener(OnBackToMainMenuClicked);
                    btn.onClick.AddListener(OnBackToMainMenuClicked);
                }
            }
        }

        private void UnbindBackButton()
        {
            var candidates = new[] { "BackButton", "PageTitle", "BtnBack", "ButtonBack", "QuayLai" };
            foreach (var name in candidates)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    var topBar = GameObject.Find("TopBar");
                    if (topBar != null)
                    {
                        var child = topBar.transform.Find(name);
                        if (child != null) go = child.gameObject;
                    }
                }

                if (go != null)
                {
                    var btn = go.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveListener(OnBackToMainMenuClicked);
                    }
                }
            }
        }

        public void OnBackToMainMenuClicked()
        {
            PlayBackClickSound();
            StartCoroutine(DelayLoadMainMenu());
        }

        private IEnumerator DelayLoadMainMenu()
        {
            yield return new WaitForSecondsRealtime(0.12f);
            SceneManager.LoadScene("MainMenu");
        }

        public void SpinOnce()
        {
            if (IsSpinning) return;

            ResolveReferences();
            bool isFree = panels != null && panels.HasFreeSpinForOne;
            int cost = purchaseUI != null ? purchaseUI.PriceForOne : 50;

            if (!isFree)
            {
                if (panels != null && !panels.HasEnoughDiamonds(cost))
                {
                    Debug.LogWarning($"[LuckyWheel] Không đủ kim cương để quay x1! Cần {cost}, hiện có {panels.DiamondBalance}");
                    return;
                }

                if (panels != null)
                {
                    panels.TrySpendDiamonds(cost);
                }
            }
            else
            {
                if (panels != null)
                {
                    panels.ConsumeFreeSpins(1);
                }
            }

            if (purchaseUI != null)
            {
                purchaseUI.Refresh();
            }

            StartCoroutine(SpinOnceRoutine());
        }

        public void SpinTenTimes()
        {
            if (IsSpinning) return;

            ResolveReferences();
            bool isFree = panels != null && panels.HasFreeSpinForTen;
            int cost = purchaseUI != null ? purchaseUI.CurrentPriceForTen : 400;

            if (!isFree)
            {
                if (panels != null && !panels.HasEnoughDiamonds(cost))
                {
                    Debug.LogWarning($"[LuckyWheel] Không đủ kim cương để quay x10! Cần {cost}, hiện có {panels.DiamondBalance}");
                    return;
                }

                if (panels != null)
                {
                    panels.TrySpendDiamonds(cost);
                }

                if (purchaseUI != null && !purchaseUI.IsFirstTenDiscountUsed)
                {
                    purchaseUI.ConsumeFirstTenDiscount();
                }
            }
            else
            {
                if (panels != null)
                {
                    panels.ConsumeFreeSpins(10);
                }
            }

            if (purchaseUI != null)
            {
                purchaseUI.Refresh();
            }

            StartCoroutine(SpinTenTimesRoutine());
        }

        private IEnumerator SpinOnceRoutine()
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            CurrentSpinCount = 1;
            SetButtonsInteractable(false);

            int currentPity = panels != null ? panels.BonusProgress : PlayerPrefs.GetInt("LUCKY_WHEEL_BONUS_PROGRESS", 0);
            bool wonRare;
            var prizeIndex = PickPrizeWithPity(ref currentPity, out wonRare);
            var prizeWon = prizes[prizeIndex];

            yield return SpinToPrize(prizeIndex);

            if (panels != null)
            {
                panels.SetBonusProgress(currentPity);
            }
            else
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", currentPity);
                PlayerPrefs.Save();
            }

            onSinglePrizeWon.Invoke(prizeWon);
            onPrizeWon.Invoke(prizeWon.id);
            onSpinSequenceFinished.Invoke();

            SetButtonsInteractable(true);
            IsSpinning = false;
        }

        private IEnumerator SpinTenTimesRoutine()
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            CurrentSpinCount = 10;
            SetButtonsInteractable(false);

            int currentPity = panels != null ? panels.BonusProgress : PlayerPrefs.GetInt("LUCKY_WHEEL_BONUS_PROGRESS", 0);
            var wonPrizes = new List<Prize>(10);
            int displayPrizeIndex = -1;

            for (var i = 0; i < 10; i++)
            {
                bool wonRare;
                var idx = PickPrizeWithPity(ref currentPity, out wonRare);
                wonPrizes.Add(prizes[idx]);

                // Nếu trong 10 lượt có trúng nhân vật huyền thoại, mâm quay sẽ dừng ngay ô đó
                if (wonRare && displayPrizeIndex < 0)
                {
                    displayPrizeIndex = idx;
                }
            }

            if (displayPrizeIndex < 0)
            {
                displayPrizeIndex = prizes.IndexOf(wonPrizes[0]);
                if (displayPrizeIndex < 0) displayPrizeIndex = 0;
            }

            yield return SpinToPrize(displayPrizeIndex);

            if (panels != null)
            {
                panels.SetBonusProgress(currentPity);
            }
            else
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", currentPity);
                PlayerPrefs.Save();
            }

            onTenPrizesWon.Invoke(wonPrizes);
            onSpinSequenceFinished.Invoke();

            SetButtonsInteractable(true);
            IsSpinning = false;
        }

        public void ClaimPityRewardDirectly()
        {
            if (IsSpinning) return;
            StartCoroutine(SpinPityRewardRoutine());
        }

        private IEnumerator SpinPityRewardRoutine()
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            CurrentSpinCount = 1;
            SetButtonsInteractable(false);

            int rareIndex = FindPrizeIndex(rareItemId);
            if (rareIndex < 0) rareIndex = 0;
            var prizeWon = prizes[rareIndex];

            if (panels != null)
            {
                panels.SetBonusProgress(0);
            }
            else
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", 0);
                PlayerPrefs.Save();
            }

            yield return SpinToPrize(rareIndex);

            onSinglePrizeWon.Invoke(prizeWon);
            onPrizeWon.Invoke(prizeWon.id);
            onSpinSequenceFinished.Invoke();

            SetButtonsInteractable(true);
            IsSpinning = false;
        }

        private IEnumerator SpinToPrize(int prizeIndex)
        {
            var segmentAngle = 360f / prizes.Count;
            var current = wheel.localEulerAngles.z;
            var normalizedCurrent = Mathf.Repeat(current, 360f);
            var desired = Mathf.Repeat(prizeIndex * segmentAngle, 360f);
            var delta = Mathf.Repeat(desired - normalizedCurrent, 360f);
            var target = current + minimumTurns * 360f + delta;

            var elapsed = 0f;
            float lastAngle = current;
            float accumulatedAngle = 0f;

            while (elapsed < spinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / spinDuration);
                var eased = t * t * t * (t * (t * 6f - 15f) + 10f);
                var angle = Mathf.LerpUnclamped(current, target, eased);
                wheel.localRotation = Quaternion.Euler(0f, 0f, angle);
                if (keepItemsUpright) KeepItemViewsUpright(angle);

                float angleDelta = Mathf.Abs(angle - lastAngle);
                accumulatedAngle += angleDelta;
                if (accumulatedAngle >= segmentAngle)
                {
                    PlayTickSound();
                    accumulatedAngle = Mathf.Repeat(accumulatedAngle, segmentAngle);
                }
                lastAngle = angle;

                yield return null;
            }
            wheel.localRotation = Quaternion.Euler(0f, 0f, target);
            if (keepItemsUpright) KeepItemViewsUpright(target);

            // Final tick right on the target winning slot
            PlayTickSound();
            yield return new WaitForSecondsRealtime(0.12f);

            // Play celebratory win sound
            PlayWinSound();
        }

        #region Audio Setup & Helpers

        private void EnsureAudioListener()
        {
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null)
            {
                var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
                if (cam != null)
                {
                    listener = cam.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    listener = gameObject.AddComponent<AudioListener>();
                }
            }
            if (listener != null)
            {
                listener.enabled = true;
            }

            AudioListener.pause = false;
            float masterVol = Luan.MainMenu.AudioManager.Instance != null
                ? Luan.MainMenu.AudioManager.Instance.MasterVolume
                : PlayerPrefs.GetFloat("SETTINGS_MASTER_VOL", 1f);
            AudioListener.volume = Mathf.Clamp01(masterVol);
        }

        private void InitAudio()
        {
            EnsureAudioListener();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f; // 100% 2D cho UI sound
                audioSource.mute = false;
            }

            if (spinAudioSource == null)
            {
                var spinObj = transform.Find("SpinAudioSource");
                if (spinObj == null)
                {
                    var go = new GameObject("SpinAudioSource");
                    go.transform.SetParent(transform);
                    spinAudioSource = go.AddComponent<AudioSource>();
                }
                else
                {
                    spinAudioSource = spinObj.GetComponent<AudioSource>();
                }
            }
            if (spinAudioSource != null)
            {
                spinAudioSource.playOnAwake = false;
                spinAudioSource.loop = false;
                spinAudioSource.spatialBlend = 0f; // 100% 2D
                spinAudioSource.mute = false;
            }

            if (spinSound == null)
            {
                spinSound = Resources.Load<AudioClip>("Audio/wheel_spin");
#if UNITY_EDITOR
                if (spinSound == null)
                    spinSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/wheel_spin.wav");
#endif
            }

            if (tickSound == null)
            {
                tickSound = Resources.Load<AudioClip>("Audio/wheel_tick");
#if UNITY_EDITOR
                if (tickSound == null)
                    tickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/wheel_tick.wav");
#endif
            }

            if (winSound == null)
            {
                winSound = Resources.Load<AudioClip>("Audio/wheel_win");
#if UNITY_EDITOR
                if (winSound == null)
                    winSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/wheel_win.wav");
#endif
                if (winSound == null)
                {
                    winSound = Resources.Load<AudioClip>("Audio/daily_claim");
#if UNITY_EDITOR
                    if (winSound == null)
                        winSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/daily_claim.wav");
#endif
                }
            }

            if (buttonClickSound == null)
            {
                buttonClickSound = Resources.Load<AudioClip>("Audio/select_001");
#if UNITY_EDITOR
                if (buttonClickSound == null)
                    buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/select_001.wav");
#endif
            }

            if (backClickSound == null)
            {
                backClickSound = Resources.Load<AudioClip>("Audio/rollover2");
#if UNITY_EDITOR
                if (backClickSound == null)
                    backClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/rollover2.wav");
#endif
            }
        }

        private void SetupAllButtonSounds()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                string btnName = btn.gameObject.name.ToLowerInvariant();

                // Gỡ các listener cũ nếu có để tránh phát âm thanh trùng lặp
                btn.onClick.RemoveListener(PlayBackClickSound);
                btn.onClick.RemoveListener(PlayConfirmClickSound);
                btn.onClick.RemoveListener(PlayButtonClickSound);

                // Gắn component UIButtonSound duy nhất để phát âm thanh chuẩn theo hệ thống AudioManager
                var sound = btn.GetComponent<Luan.MainMenu.UIButtonSound>();
                if (sound == null)
                {
                    sound = btn.gameObject.AddComponent<Luan.MainMenu.UIButtonSound>();
                    if (btnName.Contains("back") || btnName.Contains("quaylai") || btnName.Contains("close"))
                    {
                        sound.SetSoundType(Luan.MainMenu.ButtonSoundType.Close);
                    }
                    else if (btnName.Contains("spin") || btnName.Contains("claim") || btnName.Contains("confirm"))
                    {
                        sound.SetSoundType(Luan.MainMenu.ButtonSoundType.Confirm);
                    }
                    else
                    {
                        sound.SetSoundType(Luan.MainMenu.ButtonSoundType.Select);
                    }
                }
            }
        }

        private void PlayAudio(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            float sfxVol = Luan.MainMenu.AudioManager.Instance != null
                ? Luan.MainMenu.AudioManager.Instance.SFXVolume
                : PlayerPrefs.GetFloat("SETTINGS_SFX_VOL", 1f);

            if (sfxVol <= 0.001f) return;

            if (audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVol * volume));
            }
            else if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySFX(clip, volume);
            }
        }

        public void PlayButtonClickSound()
        {
            if (buttonClickSound != null)
                PlayAudio(buttonClickSound, 0.9f);
            else if (Luan.MainMenu.AudioManager.Instance != null)
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
        }

        public void PlayConfirmClickSound()
        {
            AudioClip clip = null;
            if (Luan.MainMenu.AudioManager.Instance != null && Luan.MainMenu.AudioManager.Instance.ButtonConfirmClip != null)
            {
                clip = Luan.MainMenu.AudioManager.Instance.ButtonConfirmClip;
            }
            if (clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/confirmation_001");
            }
            if (clip != null)
            {
                PlayAudio(clip, 0.95f);
            }
            else
            {
                PlayButtonClickSound();
            }
        }

        public void PlayBackClickSound()
        {
            if (backClickSound != null)
                PlayAudio(backClickSound, 0.9f);
            else if (Luan.MainMenu.AudioManager.Instance != null)
                Luan.MainMenu.AudioManager.Instance.PlayCloseSound();
        }

        public void PlaySpinSound()
        {
            // Âm thanh vòng quay được thể hiện qua nhịp kim gõ PlayTickSound() chân thực,
            // không phát âm thanh nền thừa hay tiếng mô tô lên ga.
        }

        public void StopSpinSound()
        {
            if (spinAudioSource != null && spinAudioSource.isPlaying)
            {
                spinAudioSource.Stop();
            }
        }

        public void PlayTickSound()
        {
            float sfxVol = Luan.MainMenu.AudioManager.Instance != null
                ? Luan.MainMenu.AudioManager.Instance.SFXVolume
                : PlayerPrefs.GetFloat("SETTINGS_SFX_VOL", 1f);

            if (sfxVol <= 0.001f) return;

            if (tickSound != null && audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(tickSound, Mathf.Clamp01(sfxVol * 0.8f));
            }
            else if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayWheelTick(0.8f);
            }
        }

        public void PlayWinSound()
        {
            float sfxVol = Luan.MainMenu.AudioManager.Instance != null
                ? Luan.MainMenu.AudioManager.Instance.SFXVolume
                : PlayerPrefs.GetFloat("SETTINGS_SFX_VOL", 1f);

            if (sfxVol <= 0.001f) return;

            if (winSound != null && audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(winSound, Mathf.Clamp01(sfxVol * 0.95f));
            }
            else if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayWheelWin();
            }
        }

        #endregion

        public int FindPrizeIndex(string id)
        {
            if (prizes == null) return -1;
            for (var i = 0; i < prizes.Count; i++)
            {
                if (prizes[i] != null && prizes[i].id == id)
                    return i;
            }
            return -1;
        }

        public int PickPrizeWithPity(ref int currentPity, out bool wonRare)
        {
            wonRare = false;
            int rareIndex = FindPrizeIndex(rareItemId);

            // Tăng số lượt quay hiện tại trong chu kỳ bảo hiểm
            currentPity++;

            // Điều kiện bảo hiểm: Đạt hoặc vượt mốc 80 lượt sẽ 100% nhận nhân vật huyền thoại
            bool isGuaranteed = currentPity >= pityTarget;

            // Điều kiện may mắn: Tỉ lệ 0.05% (0.0005f) xuất hiện ở mỗi lượt quay từ 0 - 80
            bool isLuckyDrop = UnityEngine.Random.value < rareItemDropRate;

            if (isGuaranteed || isLuckyDrop)
            {
                wonRare = true;
                currentPity = 0; // Đặt lại bảo hiểm về 0 ngay sau khi nhận được
                if (rareIndex >= 0) return rareIndex;
            }

            // Nếu chưa trúng item hiếm, quay trong các phần thưởng còn lại theo trọng số
            return PickWeightedNonRarePrize(rareIndex);
        }

        private int PickWeightedNonRarePrize(int excludeIndex)
        {
            var total = 0f;
            for (var i = 0; i < prizes.Count; i++)
            {
                if (i == excludeIndex) continue;
                total += Mathf.Max(0f, prizes[i].weight);
            }

            if (total <= 0f)
            {
                int fallback = UnityEngine.Random.Range(0, prizes.Count);
                if (fallback == excludeIndex && prizes.Count > 1)
                    fallback = (fallback + 1) % prizes.Count;
                return fallback;
            }

            var roll = UnityEngine.Random.value * total;
            for (var i = 0; i < prizes.Count; i++)
            {
                if (i == excludeIndex) continue;
                roll -= Mathf.Max(0f, prizes[i].weight);
                if (roll <= 0f) return i;
            }

            for (var i = prizes.Count - 1; i >= 0; i--)
            {
                if (i != excludeIndex) return i;
            }
            return 0;
        }

        private int PickWeightedPrize()
        {
            var total = 0f;
            foreach (var prize in prizes) total += Mathf.Max(0f, prize.weight);
            if (total <= 0f) return UnityEngine.Random.Range(0, prizes.Count);

            var roll = UnityEngine.Random.value * total;
            for (var i = 0; i < prizes.Count; i++)
            {
                roll -= Mathf.Max(0f, prizes[i].weight);
                if (roll <= 0f) return i;
            }
            return prizes.Count - 1;
        }

        private void SetButtonsInteractable(bool value)
        {
            if (spinCenterButton != null) spinCenterButton.interactable = value;
            if (spinX1Button != null) spinX1Button.interactable = value;
            if (spinX10Button != null) spinX10Button.interactable = value;
        }

        public void ResolveReferences()
        {
            if (wheel == null) wheel = transform.Find("Wheel") as RectTransform;
            if (spinCenterButton == null) spinCenterButton = transform.Find("SpinCenterButton")?.GetComponent<Button>();
            if (spinX1Button == null)
            {
                var btn = transform.root.Find("RightPanel/SpinX1Button");
                if (btn == null && transform.parent != null)
                    btn = transform.parent.Find("RightPanel/SpinX1Button");
                if (btn != null) spinX1Button = btn.GetComponent<Button>();
            }
            if (spinX10Button == null)
            {
                var btn = transform.root.Find("RightPanel/SpinX10Button");
                if (btn == null && transform.parent != null)
                    btn = transform.parent.Find("RightPanel/SpinX10Button");
                if (btn != null) spinX10Button = btn.GetComponent<Button>();
            }
            if (purchaseUI == null) purchaseUI = FindFirstObjectByType<LuckyWheelPurchaseUI>();
            if (panels == null) panels = FindFirstObjectByType<LuckyWheelEditablePanels>();
        }

        public void SetDefaultPrizes(Sprite[] icons, TMP_FontAsset font, Material fontMaterial = null)
        {
            itemFont = font;
            itemFontMaterial = fontMaterial;
            prizes = new List<Prize>
            {
                NewPrize("gold_1000", "Vàng\nx1.000", 1000, icons, 0),
                NewPrize("diamond_100", "Kim cương\nx100", 100, icons, 1),
                NewPrize("character_blue", "Nhân vật", 1, icons, 2),
                NewPrize("skin_jacket", "Skin", 1, icons, 3),
                NewPrize("premium_chest", "Rương quà", 1, icons, 4),
                NewPrize("ticket_1", "Vé quay\nx1", 1, icons, 5),
                NewPrize("energy_50", "Năng lượng\nx50", 50, icons, 6),
                NewPrize("rare_item", "Nhân vật\nÁnh Sao", 1, icons, 7),
                NewPrize("gold_5000", "Vàng\nx5.000", 5000, icons, 8),
                NewPrize("character_pink", "Nhân vật", 1, icons, 9),
                NewPrize("ticket_2", "Vé quay\nx2", 2, icons, 10),
                NewPrize("weapon_skin", "Skin", 1, icons, 11)
            };
        }

        [ContextMenu("Rebuild Item Views")]
        public void RebuildItemViews()
        {
            if (wheel == null) ResolveReferences();
            if (wheel == null) return;

            var old = wheel.Find("RuntimeItems");
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            var container = new GameObject("RuntimeItems", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(wheel, false);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = Vector2.zero;

            for (var i = 0; i < prizes.Count; i++) CreateItemView(container, prizes[i], i);
            KeepItemViewsUpright(wheel.localEulerAngles.z);
        }

        private void OnValidate()
        {
            if (wheel != null && !IsSpinning && keepItemsUpright)
            {
                KeepItemViewsUpright(wheel.localEulerAngles.z);
            }
        }

        private void KeepItemViewsUpright(float wheelAngle)
        {
            if (!keepItemsUpright) return;
            var container = wheel == null ? null : wheel.Find("RuntimeItems");
            if (container == null) return;

            for (var i = 0; i < container.childCount; i++)
            {
                var slot = container.GetChild(i);
                slot.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);
            }
        }

        private void CreateItemView(RectTransform container, Prize prize, int index)
        {
            var angle = index * 360f / Mathf.Max(1, prizes.Count);
            var radians = angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));

            var slot = new GameObject($"Item_{index + 1:00}_{prize.id}", typeof(RectTransform)).GetComponent<RectTransform>();
            slot.SetParent(container, false);
            slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = direction * slotRadius;
            slot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            slot.sizeDelta = Vector2.zero;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(slot, false);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, iconOffsetY);
            iconRect.sizeDelta = iconSize;
            iconRect.localRotation = Quaternion.identity;
            var image = iconGo.GetComponent<Image>();
            image.sprite = prize.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(slot, false);
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, labelOffsetY);
            labelRect.sizeDelta = labelSize;
            labelRect.localRotation = Quaternion.identity;
            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = prize.displayName;
            text.font = itemFont;

            var outlineMat = itemFontMaterial != null ? itemFontMaterial : Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
            if (outlineMat != null)
            {
                text.fontSharedMaterial = outlineMat;
            }

            if (text.fontMaterial != null)
            {
                text.fontMaterial.EnableKeyword("OUTLINE_ON");
                text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.04f, 0.01f, 0.08f, 1f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.32f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                text.fontMaterial.EnableKeyword("UNDERLAY_ON");
                text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            }

            text.fontSize = 15f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.lineSpacing = -10f;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
            text.extraPadding = true;
            text.raycastTarget = false;
        }

        private static Prize NewPrize(string id, string displayName, int amount, Sprite[] icons, int iconIndex) => new Prize
        {
            id = id,
            displayName = displayName,
            amount = amount,
            icon = icons != null && iconIndex < icons.Length ? icons[iconIndex] : null,
            weight = 1f
        };
    }
}
