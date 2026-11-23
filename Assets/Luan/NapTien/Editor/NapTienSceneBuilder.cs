using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Luan.NapTien.Editor
{
    public static class NapTienSceneBuilder
    {
        private const string SpritesRoot = "Assets/Luan/NapTien/Sprites/";
        private const string ShopSpritesRoot = "Assets/UI/Shop/Sprites/";
        private const string SongListSpritesRoot = "Assets/UI/SongList/Sprites/";
        private const string GameplaySpritesRoot = "Assets/Thuan/UI/Gameplay/Sprites/";
        private const string DailyCheckinSpritesRoot = "Assets/Thuan/UI/DailyCheckin/Sprites/";
        private const string ModularItemsRoot = "Assets/Luan/LuckyWheel/Sprites/ModularItems/";

        [MenuItem("Tools/Luan/Build NapTien Scene")]
        public static void Build()
        {
            ConfigureAllSprites();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.03f, 0.08f, 1f);
            camera.orthographic = true;
            cameraGo.transform.position = new Vector3(0, 0, -10);

            // 2. Canvas
            var canvasGo = new GameObject("NapTienCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var controller = canvasGo.AddComponent<NapTienController>();

            // 3. Fonts
            var fontBangers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Thuan/UI/Fonts/Bangers SDF.asset");
            var fontSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // 4. Background (Exact SongList Background)
            var bgGo = CreateUIObject("Background", canvasGo.transform);
            SetStretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "bg_songlist.png");
            bgImg.preserveAspect = false;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            // 5. Decor Layer (Identical to SongListDecor: Ambient glows, stickers, particles, equalizer)
            var decorGo = CreateUIObject("Decor", canvasGo.transform);
            SetStretch(decorGo);

            // 5a. Rhythmic Ambient Glows
            var glowsGo = CreateUIObject("Glows", decorGo.transform);
            SetStretch(glowsGo);

            var glowPinkGo = CreateUIObject("GlowPink", glowsGo.transform);
            var glowPinkRt = glowPinkGo.GetComponent<RectTransform>();
            glowPinkRt.anchoredPosition = new Vector2(-560, -120);
            glowPinkRt.sizeDelta = new Vector2(1100, 1000);
            var glowPinkImg = glowPinkGo.AddComponent<Image>();
            glowPinkImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySpritesRoot + "glow_soft.png");
            glowPinkImg.color = new Color(1f, 0.2f, 0.75f, 0.28f);
            glowPinkImg.raycastTarget = false;

            var glowCyanGo = CreateUIObject("GlowCyan", glowsGo.transform);
            var glowCyanRt = glowCyanGo.GetComponent<RectTransform>();
            glowCyanRt.anchoredPosition = new Vector2(600, -40);
            glowCyanRt.sizeDelta = new Vector2(1100, 1000);
            var glowCyanImg = glowCyanGo.AddComponent<Image>();
            glowCyanImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySpritesRoot + "glow_soft.png");
            glowCyanImg.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            glowCyanImg.raycastTarget = false;

            var glowPurpleGo = CreateUIObject("GlowPurple", glowsGo.transform);
            var glowPurpleRt = glowPurpleGo.GetComponent<RectTransform>();
            glowPurpleRt.anchoredPosition = new Vector2(0, 430);
            glowPurpleRt.sizeDelta = new Vector2(1600, 520);
            var glowPurpleImg = glowPurpleGo.AddComponent<Image>();
            glowPurpleImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySpritesRoot + "glow_soft.png");
            glowPurpleImg.color = new Color(0.6f, 0.3f, 1f, 0.25f);
            glowPurpleImg.raycastTarget = false;

            // 5b. Floating Sparks Container
            var sparksGo = CreateUIObject("Sparks", decorGo.transform);
            SetStretch(sparksGo);

            // 5c. Equalizer at bottom ("nhịp nhạc ở dưới")
            var eqGo = CreateUIObject("Equalizer", decorGo.transform);
            var eqRt = eqGo.GetComponent<RectTransform>();
            eqRt.anchorMin = new Vector2(0, 0);
            eqRt.anchorMax = new Vector2(1, 0);
            eqRt.pivot = new Vector2(0.5f, 0);
            eqRt.anchoredPosition = Vector2.zero;
            eqRt.sizeDelta = new Vector2(0, 140);

            // Pre-create 48 bars so they are immediately visible in Editor mode before Play!
            // Uses normalized anchors (normX) so it automatically spans 100% of any screen width on mobile!
            int barCount = 48;
            float totalWidth = 1920f;
            float w = totalWidth / barCount;
            Color barLeft = new Color(1f, 0.25f, 0.75f, 0.35f); // Hot Pink
            Color barRight = new Color(0.25f, 0.85f, 1f, 0.35f); // Cyan
            for (int i = 0; i < barCount; i++)
            {
                var barGo = CreateUIObject("Bar" + i, eqGo.transform);
                var barRt = barGo.GetComponent<RectTransform>();
                float normX = (i + 0.5f) / (float)barCount;
                barRt.anchorMin = new Vector2(normX, 0f);
                barRt.anchorMax = new Vector2(normX, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                barRt.anchoredPosition = Vector2.zero;
                float initialHgt = Mathf.Lerp(12f, 75f, Mathf.PerlinNoise(i * 0.3f, 0.5f));
                barRt.sizeDelta = new Vector2(w * 0.62f, initialHgt);
                var barImg = barGo.AddComponent<Image>();
                barImg.raycastTarget = false;
                barImg.color = Color.Lerp(barLeft, barRight, barCount > 1 ? i / (float)(barCount - 1) : 0f);
            }

            // 5d. Stickers & Splatters
            var splashTopGo = CreateUIObject("deco_splash_top", decorGo.transform);
            var splashTopRt = splashTopGo.GetComponent<RectTransform>();
            splashTopRt.anchoredPosition = new Vector2(470, 470);
            splashTopRt.sizeDelta = new Vector2(190, 75);
            var splashTopImg = splashTopGo.AddComponent<Image>();
            splashTopImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DailyCheckinSpritesRoot + "deco_splash.png");
            splashTopImg.color = new Color(1f, 1f, 1f, 0.55f);
            splashTopImg.raycastTarget = false;

            var boltGo = CreateUIObject("deco_bolt", decorGo.transform);
            var boltRt = boltGo.GetComponent<RectTransform>();
            boltRt.anchoredPosition = new Vector2(-780, 240);
            boltRt.sizeDelta = new Vector2(240, 140);
            var boltImg = boltGo.AddComponent<Image>();
            boltImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "deco_bolt.png");
            boltImg.color = new Color(1f, 1f, 1f, 0.85f);
            boltImg.raycastTarget = false;

            var splashAGo = CreateUIObject("splash_a", decorGo.transform);
            var splashARt = splashAGo.GetComponent<RectTransform>();
            splashARt.anchoredPosition = new Vector2(-860, -420);
            splashARt.sizeDelta = new Vector2(300, 170);
            var splashAImg = splashAGo.AddComponent<Image>();
            splashAImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "splash_a.png");
            splashAImg.color = new Color(1f, 1f, 1f, 0.55f);
            splashAImg.raycastTarget = false;

            var splashBGo = CreateUIObject("splash_b", decorGo.transform);
            var splashBRt = splashBGo.GetComponent<RectTransform>();
            splashBRt.anchoredPosition = new Vector2(870, -400);
            splashBRt.sizeDelta = new Vector2(240, 180);
            var splashBImg = splashBGo.AddComponent<Image>();
            splashBImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "splash_b.png");
            splashBImg.color = new Color(1f, 1f, 1f, 0.55f);
            splashBImg.raycastTarget = false;

            var crownLeftGo = CreateUIObject("deco_crown_left", decorGo.transform);
            var crownLeftRt = crownLeftGo.GetComponent<RectTransform>();
            crownLeftRt.anchoredPosition = new Vector2(-850, -60);
            crownLeftRt.sizeDelta = new Vector2(110, 105);
            var crownLeftImg = crownLeftGo.AddComponent<Image>();
            crownLeftImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "deco_crown.png");
            crownLeftImg.color = new Color(1f, 1f, 1f, 0.35f);
            crownLeftImg.raycastTarget = false;

            var crownRightGo = CreateUIObject("deco_crown_right", decorGo.transform);
            var crownRightRt = crownRightGo.GetComponent<RectTransform>();
            crownRightRt.anchoredPosition = new Vector2(860, 40);
            crownRightRt.sizeDelta = new Vector2(90, 86);
            var crownRightImg = crownRightGo.AddComponent<Image>();
            crownRightImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "deco_crown.png");
            crownRightImg.color = new Color(1f, 1f, 1f, 0.30f);
            crownRightImg.raycastTarget = false;

            // Beaters (pulsing to beat)
            var arrowsGo = CreateUIObject("deco_arrows", decorGo.transform);
            var arrowsRt = arrowsGo.GetComponent<RectTransform>();
            arrowsRt.anchoredPosition = new Vector2(820, 360);
            arrowsRt.sizeDelta = new Vector2(260, 90);
            var arrowsImg = arrowsGo.AddComponent<Image>();
            arrowsImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "deco_arrows.png");
            arrowsImg.color = new Color(1f, 1f, 1f, 0.85f);
            arrowsImg.raycastTarget = false;

            var crownTopGo = CreateUIObject("deco_crown_top", decorGo.transform);
            var crownTopRt = crownTopGo.GetComponent<RectTransform>();
            crownTopRt.anchoredPosition = new Vector2(330, 455);
            crownTopRt.sizeDelta = new Vector2(84, 84);
            var crownTopImg = crownTopGo.AddComponent<Image>();
            crownTopImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "deco_crown_pink.png");
            crownTopImg.color = new Color(1f, 1f, 1f, 0.90f);
            crownTopImg.raycastTarget = false;

            // Attach and wire SongListDecor
            var sld = decorGo.AddComponent<SongListDecor>();
            var sldSo = new SerializedObject(sld);
            sldSo.FindProperty("bpm").floatValue = 110f;

            var glowsProp = sldSo.FindProperty("glows");
            glowsProp.arraySize = 3;
            glowsProp.GetArrayElementAtIndex(0).objectReferenceValue = glowPinkImg;
            glowsProp.GetArrayElementAtIndex(1).objectReferenceValue = glowCyanImg;
            glowsProp.GetArrayElementAtIndex(2).objectReferenceValue = glowPurpleImg;

            var floatersProp = sldSo.FindProperty("floaters");
            floatersProp.arraySize = 6;
            floatersProp.GetArrayElementAtIndex(0).objectReferenceValue = splashTopRt;
            floatersProp.GetArrayElementAtIndex(1).objectReferenceValue = boltRt;
            floatersProp.GetArrayElementAtIndex(2).objectReferenceValue = splashARt;
            floatersProp.GetArrayElementAtIndex(3).objectReferenceValue = splashBRt;
            floatersProp.GetArrayElementAtIndex(4).objectReferenceValue = crownLeftRt;
            floatersProp.GetArrayElementAtIndex(5).objectReferenceValue = crownRightRt;

            var beatersProp = sldSo.FindProperty("beaters");
            beatersProp.arraySize = 2;
            beatersProp.GetArrayElementAtIndex(0).objectReferenceValue = arrowsRt;
            beatersProp.GetArrayElementAtIndex(1).objectReferenceValue = crownTopRt;

            sldSo.FindProperty("particleArea").objectReferenceValue = sparksGo.GetComponent<RectTransform>();
            sldSo.FindProperty("particleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(GameplaySpritesRoot + "glow_soft.png");
            sldSo.FindProperty("particleCount").intValue = 28;

            sldSo.FindProperty("equalizer").objectReferenceValue = eqRt;
            sldSo.FindProperty("barCount").intValue = 48;
            sldSo.FindProperty("barMaxHeight").floatValue = 110f;
            sldSo.FindProperty("barLeft").colorValue = barLeft;
            sldSo.FindProperty("barRight").colorValue = barRight;
            sldSo.ApplyModifiedProperties();

            // 6. Header Bar
            var headerGo = CreateUIObject("HeaderBar", canvasGo.transform);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0, 0);
            headerRt.sizeDelta = new Vector2(0, 120);

            // Back button (Exact aspect ratio of btn_back_shop)
            var backBtnGo = CreateUIObject("BackButton", headerGo.transform);
            var backBtnRt = backBtnGo.GetComponent<RectTransform>();
            backBtnRt.anchorMin = backBtnRt.anchorMax = new Vector2(0, 1);
            backBtnRt.pivot = new Vector2(0, 1);
            backBtnRt.anchoredPosition = new Vector2(36, -26);
            backBtnRt.sizeDelta = new Vector2(210, 54);
            var backBtnImg = backBtnGo.AddComponent<Image>();
            backBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "btn_back_shop.png");
            backBtnImg.preserveAspect = true;
            var backBtn = backBtnGo.AddComponent<Button>();
            SetButtonColors(backBtn);

            // Wallet Container at top right (compact S = 0.62)
            var walletBoxGo = CreateUIObject("WalletContainer", headerGo.transform);
            var walletBoxRt = walletBoxGo.GetComponent<RectTransform>();
            walletBoxRt.anchorMin = walletBoxRt.anchorMax = new Vector2(1, 1);
            walletBoxRt.pivot = new Vector2(1, 1);
            walletBoxRt.anchoredPosition = new Vector2(-40, -12);
            walletBoxRt.sizeDelta = new Vector2(430, 110);

            // Coin Bar (Balanced scale S = 0.62)
            var coinBarGo = CreateUIObject("CoinBar", walletBoxGo.transform);
            var coinBarRt = coinBarGo.GetComponent<RectTransform>();
            coinBarRt.anchorMin = coinBarRt.anchorMax = new Vector2(0, 1);
            coinBarRt.pivot = new Vector2(0, 1);
            coinBarRt.anchoredPosition = new Vector2(0, -15f);
            coinBarRt.sizeDelta = new Vector2(195, 107);
            var coinBarImg = coinBarGo.AddComponent<Image>();
            coinBarImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "bar_coin.png");
            coinBarImg.preserveAspect = true;
            var coinBarBtn = coinBarGo.AddComponent<Button>();
            SetButtonColors(coinBarBtn);

            var coinTextGo = CreateUIObject("CoinText", coinBarGo.transform);
            var coinTextRt = coinTextGo.GetComponent<RectTransform>();
            coinTextRt.anchorMin = new Vector2(0.38f, 0.32f);
            coinTextRt.anchorMax = new Vector2(0.96f, 0.81f);
            coinTextRt.offsetMin = coinTextRt.offsetMax = Vector2.zero;
            var coinTMP = coinTextGo.AddComponent<TextMeshProUGUI>();
            coinTMP.font = fontBangers != null ? fontBangers : fontSans;
            coinTMP.fontSize = 26;
            coinTMP.color = Color.white;
            coinTMP.text = "10,000";
            coinTMP.alignment = TextAlignmentOptions.Center;
            coinTMP.raycastTarget = false;

            // Gem Bar (Balanced scale S = 0.62)
            var gemBarGo = CreateUIObject("GemBar", walletBoxGo.transform);
            var gemBarRt = gemBarGo.GetComponent<RectTransform>();
            gemBarRt.anchorMin = gemBarRt.anchorMax = new Vector2(0, 1);
            gemBarRt.pivot = new Vector2(0, 1);
            gemBarRt.anchoredPosition = new Vector2(210, -14f);
            gemBarRt.sizeDelta = new Vector2(203, 92);
            var gemBarImg = gemBarGo.AddComponent<Image>();
            gemBarImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "bar_gem.png");
            gemBarImg.preserveAspect = true;
            var gemBarBtn = gemBarGo.AddComponent<Button>();
            SetButtonColors(gemBarBtn);

            var gemTextGo = CreateUIObject("GemText", gemBarGo.transform);
            var gemTextRt = gemTextGo.GetComponent<RectTransform>();
            gemTextRt.anchorMin = new Vector2(0.38f, 0.20f);
            gemTextRt.anchorMax = new Vector2(0.96f, 0.77f);
            gemTextRt.offsetMin = gemTextRt.offsetMax = Vector2.zero;
            var gemTMP = gemTextGo.AddComponent<TextMeshProUGUI>();
            gemTMP.font = fontBangers != null ? fontBangers : fontSans;
            gemTMP.fontSize = 26;
            gemTMP.color = Color.white;
            gemTMP.text = "1,000";
            gemTMP.alignment = TextAlignmentOptions.Center;
            gemTMP.raycastTarget = false;

            // 7. Category Tabs (Stylized Cyberpunk Frames with dedicated icons and clean neon borders)
            var tabsBoxGo = CreateUIObject("CategoryTabs", canvasGo.transform);
            var tabsBoxRt = tabsBoxGo.GetComponent<RectTransform>();
            tabsBoxRt.anchorMin = tabsBoxRt.anchorMax = new Vector2(0.5f, 1f);
            tabsBoxRt.pivot = new Vector2(0.5f, 1f);
            tabsBoxRt.anchoredPosition = new Vector2(0, -114);
            tabsBoxRt.sizeDelta = new Vector2(980, 54);

            var tabNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_normal.png");
            var tabActiveCyanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_active_cyan.png");
            var tabActiveGoldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_active_gold.png");
            var tabActivePinkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_active_pink.png");

            var gemIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "icon_gem.png");
            var coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "icon_coin.png");
            var specialIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "deco_crown_pink.png");

            var (tabGemsBtn, tabGemsBg, tabGemsTxt, tabGemsIcon) = CreateCategoryTab(
                "Tab_Gems", tabsBoxGo.transform, new Vector2(-320, 0), "GÓI KIM CƯƠNG", 
                tabActiveCyanSprite, gemIconSprite, new Vector2(34, 32), fontBangers);

            var (tabCoinsBtn, tabCoinsBg, tabCoinsTxt, tabCoinsIcon) = CreateCategoryTab(
                "Tab_Coins", tabsBoxGo.transform, new Vector2(0, 0), "GÓI TIỀN VÀNG", 
                tabNormalSprite, coinIconSprite, new Vector2(36, 34), fontBangers);

            var (tabSpecialBtn, tabSpecialBg, tabSpecialTxt, tabSpecialIcon) = CreateCategoryTab(
                "Tab_Special", tabsBoxGo.transform, new Vector2(320, 0), "ƯU ĐÃI ĐẶC BIỆT", 
                tabNormalSprite, specialIconSprite, new Vector2(34, 34), fontBangers);

            // 8. Main UI Frame / Pack Areas (UI Background: 9-sliced panel_list with cyber paint borders)
            var packAreaGo = CreateUIObject("PackArea", canvasGo.transform);
            var packAreaRt = packAreaGo.GetComponent<RectTransform>();
            packAreaRt.anchorMin = new Vector2(0.5f, 0.5f);
            packAreaRt.anchorMax = new Vector2(0.5f, 0.5f);
            packAreaRt.pivot = new Vector2(0.5f, 0.5f);
            packAreaRt.anchoredPosition = new Vector2(0, -70);
            packAreaRt.sizeDelta = new Vector2(1700, 770);

            // UI Background Panel (Exact panel_list from SongList screenshot)
            var uiBgGo = CreateUIObject("UIBackground", packAreaGo.transform);
            SetStretch(uiBgGo);
            var uiBgImg = uiBgGo.AddComponent<Image>();
            uiBgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "panel_list.png");
            uiBgImg.type = Image.Type.Sliced;
            uiBgImg.color = Color.white;
            uiBgImg.raycastTarget = false;

            // A. Gems Container (2 rows x 3 columns)
            var gemsContainerGo = CreateUIObject("GemsContainer", packAreaGo.transform);
            SetStretch(gemsContainerGo);

            var gemPacks = GetDefaultGemPacks();
            var gemCardViews = new List<NapTienCardView>();
            Vector2[] gridPositions = new Vector2[]
            {
                new Vector2(-520, 165),  new Vector2(0, 165),  new Vector2(520, 165),
                new Vector2(-520, -170), new Vector2(0, -170), new Vector2(520, -170)
            };

            for (int i = 0; i < gemPacks.Length; i++)
            {
                var cardView = CreatePackCard($"GemCard_{i}", gemsContainerGo.transform, gridPositions[i], gemPacks[i], fontBangers, fontSans);
                gemCardViews.Add(cardView);
            }

            // B. Coins Container (2 rows x 3 columns)
            var coinsContainerGo = CreateUIObject("CoinsContainer", packAreaGo.transform);
            SetStretch(coinsContainerGo);

            var coinPacks = GetDefaultCoinPacks();
            var coinCardViews = new List<NapTienCardView>();
            for (int i = 0; i < coinPacks.Length; i++)
            {
                var cardView = CreatePackCard($"CoinCard_{i}", coinsContainerGo.transform, gridPositions[i], coinPacks[i], fontBangers, fontSans);
                coinCardViews.Add(cardView);
            }

            // C. Special Container (3 Large Cards)
            var specialContainerGo = CreateUIObject("SpecialContainer", packAreaGo.transform);
            SetStretch(specialContainerGo);

            var specialPacks = GetDefaultSpecialPacks();
            var specialCardViews = new List<NapTienCardView>();
            Vector2[] specialPositions = new Vector2[]
            {
                new Vector2(-520, 0), new Vector2(0, 0), new Vector2(520, 0)
            };
            for (int i = 0; i < specialPacks.Length; i++)
            {
                var cardView = CreatePackCard($"SpecialCard_{i}", specialContainerGo.transform, specialPositions[i], specialPacks[i], fontBangers, fontSans, isLarge: true);
                specialCardViews.Add(cardView);
            }

            // 9. Payment Modal
            var modalBuilder = BuildPaymentModal(canvasGo.transform, fontBangers, fontSans);

            // 10. Success Toast
            var successToast = BuildSuccessToast(canvasGo.transform, fontBangers, fontSans);

            // 11. Copy Toast
            var (copyToastGo, copyToastTmp) = BuildCopyToast(canvasGo.transform, fontSans);

            // 12. Wire Serialized Fields on Controller
            var so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("returnSceneName").stringValue = "Thuan_Shop";
            so.FindProperty("coinText").objectReferenceValue = coinTMP;
            so.FindProperty("gemText").objectReferenceValue = gemTMP;
            so.FindProperty("coinBarButton").objectReferenceValue = coinBarBtn;
            so.FindProperty("gemBarButton").objectReferenceValue = gemBarBtn;

            so.FindProperty("tabGemsButton").objectReferenceValue = tabGemsBtn;
            so.FindProperty("tabCoinsButton").objectReferenceValue = tabCoinsBtn;
            so.FindProperty("tabSpecialButton").objectReferenceValue = tabSpecialBtn;

            so.FindProperty("tabGemsBg").objectReferenceValue = tabGemsBg;
            so.FindProperty("tabCoinsBg").objectReferenceValue = tabCoinsBg;
            so.FindProperty("tabSpecialBg").objectReferenceValue = tabSpecialBg;

            so.FindProperty("tabGemsText").objectReferenceValue = tabGemsTxt;
            so.FindProperty("tabCoinsText").objectReferenceValue = tabCoinsTxt;
            so.FindProperty("tabSpecialText").objectReferenceValue = tabSpecialTxt;

            so.FindProperty("tabGemsIcon").objectReferenceValue = tabGemsIcon;
            so.FindProperty("tabCoinsIcon").objectReferenceValue = tabCoinsIcon;
            so.FindProperty("tabSpecialIcon").objectReferenceValue = tabSpecialIcon;

            so.FindProperty("tabNormalSprite").objectReferenceValue = tabNormalSprite;
            so.FindProperty("tabActiveCyanSprite").objectReferenceValue = tabActiveCyanSprite;
            so.FindProperty("tabActiveGoldSprite").objectReferenceValue = tabActiveGoldSprite;
            so.FindProperty("tabActivePinkSprite").objectReferenceValue = tabActivePinkSprite;

            so.FindProperty("gemsContainer").objectReferenceValue = gemsContainerGo;
            so.FindProperty("coinsContainer").objectReferenceValue = coinsContainerGo;
            so.FindProperty("specialContainer").objectReferenceValue = specialContainerGo;

            SetArrayProperty(so.FindProperty("gemCardViews"), gemCardViews);
            SetArrayProperty(so.FindProperty("coinCardViews"), coinCardViews);
            SetArrayProperty(so.FindProperty("specialCardViews"), specialCardViews);

            SetPacksArrayProperty(so.FindProperty("gemPacks"), gemPacks);
            SetPacksArrayProperty(so.FindProperty("coinPacks"), coinPacks);
            SetPacksArrayProperty(so.FindProperty("specialPacks"), specialPacks);

            so.FindProperty("paymentModal").objectReferenceValue = modalBuilder.modalGo;
            so.FindProperty("paymentModalDialog").objectReferenceValue = modalBuilder.dialogRt;
            so.FindProperty("modalPackIcon").objectReferenceValue = modalBuilder.packIconImg;
            so.FindProperty("modalPackTitle").objectReferenceValue = modalBuilder.packTitleTmp;
            so.FindProperty("modalPackValue").objectReferenceValue = modalBuilder.packValueTmp;
            so.FindProperty("modalPackBonus").objectReferenceValue = modalBuilder.modalPackBonusTmp;
            so.FindProperty("modalPackPrice").objectReferenceValue = modalBuilder.packPriceTmp;

            so.FindProperty("btnVietQR").objectReferenceValue = modalBuilder.btnVietQR;
            so.FindProperty("btnZaloPay").objectReferenceValue = modalBuilder.btnZaloPay;
            so.FindProperty("btnMoMo").objectReferenceValue = modalBuilder.btnMoMo;
            so.FindProperty("bgVietQR").objectReferenceValue = modalBuilder.bgVietQR;
            so.FindProperty("bgZaloPay").objectReferenceValue = modalBuilder.bgZaloPay;
            so.FindProperty("bgMoMo").objectReferenceValue = modalBuilder.bgMoMo;
            so.FindProperty("imgVietQRSelect").objectReferenceValue = modalBuilder.imgVietQRSelect;
            so.FindProperty("imgZaloPaySelect").objectReferenceValue = modalBuilder.imgZaloPaySelect;
            so.FindProperty("imgMoMoSelect").objectReferenceValue = modalBuilder.imgMoMoSelect;

            so.FindProperty("tabChannelNormal").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_chan_inactive.png");
            so.FindProperty("tabChannelActiveCyan").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_chan_active_cyan.png");
            so.FindProperty("tabChannelActiveBlue").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_chan_active_blue.png");
            so.FindProperty("tabChannelActivePink").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_chan_active_pink.png");

            var paymentGatewayComp = controller.GetComponent<PaymentGateway>();
            if (paymentGatewayComp == null) paymentGatewayComp = controller.gameObject.AddComponent<PaymentGateway>();
            so.FindProperty("paymentGateway").objectReferenceValue = paymentGatewayComp;
            so.FindProperty("qrRawImage").objectReferenceValue = modalBuilder.qrRawImg;
            so.FindProperty("qrImage").objectReferenceValue = modalBuilder.qrImg;
            so.FindProperty("qrVietQRSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "qr_vietqr.png");
            so.FindProperty("qrZaloPaySprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "qr_zalopay.png");
            so.FindProperty("qrMoMoSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "qr_momo.png");

            so.FindProperty("bankNameText").objectReferenceValue = modalBuilder.bankNameTmp;
            so.FindProperty("accountNumText").objectReferenceValue = modalBuilder.accountNumTmp;
            so.FindProperty("accountNameText").objectReferenceValue = modalBuilder.accountNameTmp;
            so.FindProperty("transferContentText").objectReferenceValue = modalBuilder.transferContentTmp;
            so.FindProperty("amountToPayText").objectReferenceValue = modalBuilder.amountToPayTmp;
            so.FindProperty("timerCountdownText").objectReferenceValue = modalBuilder.timerCountdownTmp;

            so.FindProperty("copyAccountNumBtn").objectReferenceValue = modalBuilder.copyAccountNumBtn;
            so.FindProperty("copyContentBtn").objectReferenceValue = modalBuilder.copyContentBtn;
            so.FindProperty("confirmPaymentBtn").objectReferenceValue = modalBuilder.confirmPaymentBtn;
            so.FindProperty("closePaymentModalBtn").objectReferenceValue = modalBuilder.closePaymentModalBtn;

            so.FindProperty("successToast").objectReferenceValue = successToast.rootGo;
            so.FindProperty("successToastDialog").objectReferenceValue = successToast.dialogRt;
            so.FindProperty("successTitleText").objectReferenceValue = successToast.titleTmp;
            so.FindProperty("successIcon").objectReferenceValue = successToast.iconImg;
            so.FindProperty("successAura").objectReferenceValue = successToast.auraImg;
            so.FindProperty("successPackNameText").objectReferenceValue = successToast.packNameTmp;
            so.FindProperty("successAmountText").objectReferenceValue = successToast.amountTmp;
            so.FindProperty("successBonusText").objectReferenceValue = successToast.bonusTmp;
            so.FindProperty("successMessageText").objectReferenceValue = successToast.messageTmp;
            so.FindProperty("successOkButton").objectReferenceValue = successToast.okBtn;
            so.FindProperty("successOverlayButton").objectReferenceValue = successToast.overlayBtn;
            so.FindProperty("auraCyan").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "glow_aura_cyan.png");
            so.FindProperty("auraGold").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "glow_aura_gold.png");

            so.FindProperty("copyToast").objectReferenceValue = copyToastGo;
            so.FindProperty("copyToastText").objectReferenceValue = copyToastTmp;

            so.ApplyModifiedProperties();

            // Bind Cards at edit time
            for (int i = 0; i < gemCardViews.Count; i++)
            {
                gemCardViews[i].Bind(gemPacks[i], controller.OnPackSelected);
            }
            for (int i = 0; i < coinCardViews.Count; i++)
            {
                coinCardViews[i].Bind(coinPacks[i], controller.OnPackSelected);
            }
            for (int i = 0; i < specialCardViews.Count; i++)
            {
                specialCardViews[i].Bind(specialPacks[i], controller.OnPackSelected);
            }

            // 13. Event System
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 14. Save Scene
            string scenePath = "Assets/Luan/Scenes/NapTien.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"NapTien scene successfully created at: {scenePath}");
        }

        private static (Button, Image, TMP_Text, Image) CreateCategoryTab(
            string name, Transform parent, Vector2 pos, string label, Sprite bgSprite, Sprite iconSprite, Vector2 iconSize, TMP_FontAsset font)
        {
            var tabGo = CreateUIObject(name, parent);
            var tabRt = tabGo.GetComponent<RectTransform>();
            tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 0.5f);
            tabRt.pivot = new Vector2(0.5f, 0.5f);
            tabRt.anchoredPosition = pos;
            tabRt.sizeDelta = new Vector2(310, 54);

            var bgImg = tabGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
            var btn = tabGo.AddComponent<Button>();
            SetButtonColors(btn);

            // Icon on the left
            var iconGo = CreateUIObject("Icon", tabGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(38, 0);
            iconRt.sizeDelta = iconSize;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Text
            var textGo = CreateUIObject("Text", tabGo.transform);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.offsetMin = new Vector2(65, 0);
            textRt.offsetMax = new Vector2(-12, 0);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 25;
            tmp.text = label;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return (btn, bgImg, tmp, iconImg);
        }

        private static NapTienCardView CreatePackCard(string name, Transform parent, Vector2 pos, TopUpPackData pack, TMP_FontAsset fontBangers, TMP_FontAsset fontSans, bool isLarge = false)
        {
            var cardGo = CreateUIObject(name, parent);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = pos;
            cardRt.sizeDelta = isLarge ? new Vector2(460, 530) : new Vector2(430, 315);

            // 1. Card Background
            var bgImg = cardGo.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "card_cyber_panel.png");
            var cardFullBtn = cardGo.AddComponent<Button>();
            SetButtonColors(cardFullBtn);

            // 2. Hover Glowing Frame (Active on pointer hover)
            var hoverGo = CreateUIObject("HoverFrame", cardGo.transform);
            SetStretch(hoverGo);
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "card_cyber_frame_hover.png");
            hoverImg.raycastTarget = false;
            hoverGo.SetActive(false);

            // 3. Shimmer Sweep Mask & Streak
            var shimmerMaskGo = CreateUIObject("ShimmerMask", cardGo.transform);
            SetStretch(shimmerMaskGo);
            shimmerMaskGo.AddComponent<RectMask2D>();

            var shimmerStreakGo = CreateUIObject("ShimmerStreak", shimmerMaskGo.transform);
            var shimmerStreakRt = shimmerStreakGo.GetComponent<RectTransform>();
            shimmerStreakRt.anchorMin = shimmerStreakRt.anchorMax = new Vector2(0.5f, 0.5f);
            shimmerStreakRt.pivot = new Vector2(0.5f, 0.5f);
            shimmerStreakRt.sizeDelta = isLarge ? new Vector2(160, 560) : new Vector2(130, 360);
            shimmerStreakRt.anchoredPosition = new Vector2(-400, 0);
            var shimmerImg = shimmerStreakGo.AddComponent<Image>();
            shimmerImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "fx_shimmer_streak.png");
            shimmerImg.raycastTarget = false;
            shimmerStreakGo.SetActive(false);

            // 4. Pedestal Aura Glow (behind the icon)
            var auraGo = CreateUIObject("AuraGlow", cardGo.transform);
            var auraRt = auraGo.GetComponent<RectTransform>();
            auraRt.anchorMin = auraRt.anchorMax = new Vector2(0.5f, 0.5f);
            auraRt.pivot = new Vector2(0.5f, 0.5f);
            auraRt.anchoredPosition = isLarge ? new Vector2(0, 80) : new Vector2(0, 32);
            auraRt.sizeDelta = isLarge ? new Vector2(290, 290) : new Vector2(220, 220);
            var auraImg = auraGo.AddComponent<Image>();
            string auraName = pack.currencyType == CurrencyType.Coin ? "glow_aura_gold.png" 
                : (isLarge ? "glow_aura_purple.png" : "glow_aura_cyan.png");
            auraImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + auraName);
            auraImg.raycastTarget = false;

            // 5. Pack Icon (Floating bobbing)
            var iconGo = CreateUIObject("PackIcon", cardGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = isLarge ? new Vector2(0, 80) : new Vector2(0, 32);
            iconRt.sizeDelta = isLarge ? new Vector2(230, 230) : new Vector2(175, 175);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = pack.iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 6. Title (Safe layout avoiding collision with BadgeRibbon)
            var titleGo = CreateUIObject("TitleText", cardGo.transform);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            bool hasBadge = !string.IsNullOrEmpty(pack.badgeText);
            titleRt.offsetMin = new Vector2(18, -44);
            titleRt.offsetMax = new Vector2(hasBadge ? -138 : -18, -14);
            var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
            titleTMP.font = fontBangers != null ? fontBangers : fontSans;
            titleTMP.fontSize = isLarge ? 26 : 22;
            titleTMP.enableAutoSizing = true;
            titleTMP.fontSizeMin = 14;
            titleTMP.fontSizeMax = isLarge ? 26 : 22;
            titleTMP.text = pack.title;
            titleTMP.color = Color.white;
            titleTMP.alignment = hasBadge ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            titleTMP.enableWordWrapping = false;
            titleTMP.raycastTarget = false;

            // 7. Amount / Value Text
            var amountGo = CreateUIObject("AmountText", cardGo.transform);
            var amountRt = amountGo.GetComponent<RectTransform>();
            amountRt.anchorMin = amountRt.anchorMax = new Vector2(0.5f, 0.5f);
            amountRt.pivot = new Vector2(0.5f, 0.5f);
            amountRt.anchoredPosition = isLarge ? new Vector2(0, -78) : new Vector2(0, -50);
            amountRt.sizeDelta = new Vector2(380, 36);
            var amountTMP = amountGo.AddComponent<TextMeshProUGUI>();
            amountTMP.font = fontBangers != null ? fontBangers : fontSans;
            amountTMP.fontSize = isLarge ? 29 : 24;
            amountTMP.color = pack.currencyType == CurrencyType.Coin ? new Color(1f, 0.92f, 0.25f, 1f) : new Color(0f, 1f, 0.95f, 1f);
            string unit = pack.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
            amountTMP.text = $"{pack.mainAmount:N0} {unit}";
            amountTMP.alignment = TextAlignmentOptions.Center;
            amountTMP.raycastTarget = false;

            // 8. Bonus text
            var bonusGo = CreateUIObject("BonusText", cardGo.transform);
            var bonusRt = bonusGo.GetComponent<RectTransform>();
            bonusRt.anchorMin = bonusRt.anchorMax = new Vector2(0.5f, 0.5f);
            bonusRt.pivot = new Vector2(0.5f, 0.5f);
            bonusRt.anchoredPosition = isLarge ? new Vector2(0, -116) : new Vector2(0, -76);
            bonusRt.sizeDelta = new Vector2(300, 24);
            var bonusTMP = bonusGo.AddComponent<TextMeshProUGUI>();
            bonusTMP.font = fontSans;
            bonusTMP.fontSize = 15;
            bonusTMP.color = new Color(0.35f, 1f, 0.65f, 1f); // Neon Emerald Green
            bonusTMP.text = pack.bonusAmount > 0 ? $"+{pack.bonusAmount:N0} Thưởng" : "";
            bonusTMP.alignment = TextAlignmentOptions.Center;
            bonusTMP.raycastTarget = false;

            // 9. Price Tag Button
            var priceBtnGo = CreateUIObject("PriceTagButton", cardGo.transform);
            var priceBtnRt = priceBtnGo.GetComponent<RectTransform>();
            priceBtnRt.anchorMin = priceBtnRt.anchorMax = new Vector2(0.5f, 0f);
            priceBtnRt.pivot = new Vector2(0.5f, 0f);
            priceBtnRt.anchoredPosition = new Vector2(0, 16);
            priceBtnRt.sizeDelta = isLarge ? new Vector2(270, 56) : new Vector2(230, 48);
            var priceBtnImg = priceBtnGo.AddComponent<Image>();
            priceBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "btn_price_tag.png");
            var priceBtn = priceBtnGo.AddComponent<Button>();
            SetButtonColors(priceBtn);

            var priceTextGo = CreateUIObject("PriceText", priceBtnGo.transform);
            SetStretch(priceTextGo);
            var priceTMP = priceTextGo.AddComponent<TextMeshProUGUI>();
            priceTMP.font = fontBangers != null ? fontBangers : fontSans;
            priceTMP.fontSize = 24;
            priceTMP.color = Color.white;
            priceTMP.text = $"{pack.priceVnd:N0} đ";
            priceTMP.alignment = TextAlignmentOptions.Center;
            priceTMP.raycastTarget = false;

            // 10. Badge Ribbon
            var badgeGo = CreateUIObject("BadgeRibbon", cardGo.transform);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(6, 6);
            badgeRt.sizeDelta = new Vector2(140, 36);
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "badge_bonus.png");
            badgeImg.raycastTarget = false;

            var badgeTextGo = CreateUIObject("BadgeText", badgeGo.transform);
            SetStretch(badgeTextGo);
            var badgeTMP = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeTMP.font = fontBangers != null ? fontBangers : fontSans;
            badgeTMP.fontSize = 16;
            badgeTMP.color = Color.white;
            badgeTMP.text = pack.badgeText;
            badgeTMP.alignment = TextAlignmentOptions.Center;
            badgeTMP.raycastTarget = false;

            badgeGo.SetActive(!string.IsNullOrEmpty(pack.badgeText));

            // 11. Attach UICardAnimation
            var cardAnim = cardGo.AddComponent<UICardAnimation>();
            var animSo = new SerializedObject(cardAnim);
            animSo.FindProperty("iconTransform").objectReferenceValue = iconRt;
            animSo.FindProperty("auraTransform").objectReferenceValue = auraRt;
            animSo.FindProperty("hoverFrame").objectReferenceValue = hoverImg;
            animSo.FindProperty("shimmerStreak").objectReferenceValue = shimmerStreakRt;
            animSo.FindProperty("floatAmplitude").floatValue = 6.5f;
            animSo.FindProperty("floatSpeed").floatValue = 2.4f;
            animSo.FindProperty("auraPulseScale").floatValue = 0.12f;
            animSo.ApplyModifiedProperties();

            // 12. Card View Component
            var cardView = cardGo.AddComponent<NapTienCardView>();
            var viewSo = new SerializedObject(cardView);
            viewSo.FindProperty("bgImage").objectReferenceValue = bgImg;
            viewSo.FindProperty("iconImage").objectReferenceValue = iconImg;
            viewSo.FindProperty("titleText").objectReferenceValue = titleTMP;
            viewSo.FindProperty("amountText").objectReferenceValue = amountTMP;
            viewSo.FindProperty("bonusText").objectReferenceValue = bonusTMP;
            viewSo.FindProperty("buyButton").objectReferenceValue = priceBtn;
            viewSo.FindProperty("priceText").objectReferenceValue = priceTMP;
            viewSo.FindProperty("badgeObj").objectReferenceValue = badgeGo;
            viewSo.FindProperty("badgeText").objectReferenceValue = badgeTMP;
            viewSo.FindProperty("cardFullButton").objectReferenceValue = cardFullBtn;

            var dataProp = viewSo.FindProperty("_data");
            if (dataProp != null)
            {
                dataProp.FindPropertyRelative("packId").stringValue = pack.packId;
                dataProp.FindPropertyRelative("title").stringValue = pack.title;
                dataProp.FindPropertyRelative("mainAmount").intValue = pack.mainAmount;
                dataProp.FindPropertyRelative("bonusAmount").intValue = pack.bonusAmount;
                dataProp.FindPropertyRelative("priceVnd").intValue = pack.priceVnd;
                dataProp.FindPropertyRelative("badgeText").stringValue = pack.badgeText;
                dataProp.FindPropertyRelative("iconSprite").objectReferenceValue = pack.iconSprite;
                dataProp.FindPropertyRelative("currencyType").enumValueIndex = (int)pack.currencyType;
                dataProp.FindPropertyRelative("bonusTickets").intValue = pack.bonusTickets;
            }

            viewSo.ApplyModifiedProperties();
            cardView.SetDataOnly(pack);
            EditorUtility.SetDirty(cardView);

            return cardView;
        }

        private struct ModalBuildResult
        {
            public GameObject modalGo;
            public RectTransform dialogRt;
            public Image packIconImg;
            public TMP_Text packTitleTmp;
            public TMP_Text packValueTmp;
            public TMP_Text modalPackBonusTmp;
            public TMP_Text packPriceTmp;
            public Button btnVietQR;
            public Button btnZaloPay;
            public Button btnMoMo;
            public Image bgVietQR;
            public Image bgZaloPay;
            public Image bgMoMo;
            public Image imgVietQRSelect;
            public Image imgZaloPaySelect;
            public Image imgMoMoSelect;
            public RawImage qrRawImg;
            public Image qrImg;
            public TMP_Text bankNameTmp;
            public TMP_Text accountNumTmp;
            public TMP_Text accountNameTmp;
            public TMP_Text transferContentTmp;
            public TMP_Text amountToPayTmp;
            public TMP_Text timerCountdownTmp;
            public Button copyAccountNumBtn;
            public Button copyContentBtn;
            public Button confirmPaymentBtn;
            public Button closePaymentModalBtn;
        }

        private static ModalBuildResult BuildPaymentModal(Transform parent, TMP_FontAsset fontBangers, TMP_FontAsset fontSans)
        {
            var modalGo = CreateUIObject("PaymentModal", parent);
            SetStretch(modalGo);
            var overlayImg = modalGo.AddComponent<Image>();
            overlayImg.color = new Color(0.02f, 0.02f, 0.07f, 0.92f);

            // Dialog Window (Using authentic panel_detail with graffiti ink & neon splatters)
            var dialogGo = CreateUIObject("DialogWindow", modalGo.transform);
            var dialogRt = dialogGo.GetComponent<RectTransform>();
            dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.pivot = new Vector2(0.5f, 0.5f);
            dialogRt.sizeDelta = new Vector2(980, 670);
            var dialogImg = dialogGo.AddComponent<Image>();
            dialogImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SongListSpritesRoot + "panel_detail.png");
            dialogImg.type = Image.Type.Sliced;
            dialogImg.color = Color.white;

            // Spray Decal Decor: Top-Left Dripping Pink Crown
            var crownPinkGo = CreateUIObject("CrownPinkDecal", dialogGo.transform);
            var crownPinkRt = crownPinkGo.GetComponent<RectTransform>();
            crownPinkRt.anchorMin = crownPinkRt.anchorMax = new Vector2(0, 1);
            crownPinkRt.pivot = new Vector2(0, 1);
            crownPinkRt.anchoredPosition = new Vector2(16, 16);
            crownPinkRt.sizeDelta = new Vector2(90, 90);
            crownPinkRt.localRotation = Quaternion.Euler(0, 0, -14f);
            var crownPinkImg = crownPinkGo.AddComponent<Image>();
            crownPinkImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "deco_crown_pink.png");
            crownPinkImg.color = new Color(1f, 1f, 1f, 0.90f);
            crownPinkImg.raycastTarget = false;

            // Spray Decal Decor: Bottom-Right Dripping Blue Crown
            var crownBlueGo = CreateUIObject("CrownBlueDecal", dialogGo.transform);
            var crownBlueRt = crownBlueGo.GetComponent<RectTransform>();
            crownBlueRt.anchorMin = crownBlueRt.anchorMax = new Vector2(1, 0);
            crownBlueRt.pivot = new Vector2(1, 0);
            crownBlueRt.anchoredPosition = new Vector2(-16, -14);
            crownBlueRt.sizeDelta = new Vector2(80, 80);
            crownBlueRt.localRotation = Quaternion.Euler(0, 0, 12f);
            var crownBlueImg = crownBlueGo.AddComponent<Image>();
            crownBlueImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "deco_crown_blue.png");
            crownBlueImg.color = new Color(1f, 1f, 1f, 0.75f);
            crownBlueImg.raycastTarget = false;

            // Top Header Ribbon Plate
            var ribbonGo = CreateUIObject("HeaderRibbon", dialogGo.transform);
            var ribbonRt = ribbonGo.GetComponent<RectTransform>();
            ribbonRt.anchorMin = ribbonRt.anchorMax = new Vector2(0.5f, 1f);
            ribbonRt.pivot = new Vector2(0.5f, 1f);
            ribbonRt.anchoredPosition = new Vector2(0, -12);
            ribbonRt.sizeDelta = new Vector2(520, 56);
            var ribbonImg = ribbonGo.AddComponent<Image>();
            ribbonImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "header_graffiti_plate.png");
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.raycastTarget = false;

            // Header Title inside Ribbon
            var headerTitleGo = CreateUIObject("HeaderTitle", ribbonGo.transform);
            var headerTitleRt = headerTitleGo.GetComponent<RectTransform>();
            headerTitleRt.anchorMin = Vector2.zero;
            headerTitleRt.anchorMax = Vector2.one;
            headerTitleRt.offsetMin = headerTitleRt.offsetMax = Vector2.zero;
            var headerTitleTmp = headerTitleGo.AddComponent<TextMeshProUGUI>();
            headerTitleTmp.font = fontBangers != null ? fontBangers : fontSans;
            headerTitleTmp.fontSize = 32;
            headerTitleTmp.text = "THANH TOÁN ĐƠN HÀNG";
            headerTitleTmp.color = Color.white;
            headerTitleTmp.alignment = TextAlignmentOptions.Center;
            headerTitleTmp.raycastTarget = false;

            // Close Button (Stylized Neon Circle with 'X')
            var closeBtnGo = CreateUIObject("CloseButton", dialogGo.transform);
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1, 1);
            closeRt.pivot = new Vector2(1, 1);
            closeRt.anchoredPosition = new Vector2(-24, -18);
            closeRt.sizeDelta = new Vector2(46, 46);
            var closeImg = closeBtnGo.AddComponent<Image>();
            closeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "btn_close_modal.png");
            closeImg.preserveAspect = true;
            var closeBtn = closeBtnGo.AddComponent<Button>();
            SetButtonColors(closeBtn);

            // Left Side: Pack Summary Preview (Cyberpunk Framed Glass Panel)
            var summaryGo = CreateUIObject("PackSummaryBox", dialogGo.transform);
            var summaryRt = summaryGo.GetComponent<RectTransform>();
            summaryRt.anchorMin = summaryRt.anchorMax = new Vector2(0, 0.5f);
            summaryRt.pivot = new Vector2(0, 0.5f);
            summaryRt.anchoredPosition = new Vector2(42, 10);
            summaryRt.sizeDelta = new Vector2(330, 530);
            var summaryBg = summaryGo.AddComponent<Image>();
            summaryBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "card_select_frame.png");
            summaryBg.type = Image.Type.Sliced;
            summaryBg.color = Color.white;
            summaryBg.raycastTarget = false;

            // Radiant Anime Burst FX behind pack icon
            var iconAuraGo = CreateUIObject("IconAura", summaryGo.transform);
            var iconAuraRt = iconAuraGo.GetComponent<RectTransform>();
            iconAuraRt.anchorMin = iconAuraRt.anchorMax = new Vector2(0.5f, 1f);
            iconAuraRt.pivot = new Vector2(0.5f, 1f);
            iconAuraRt.anchoredPosition = new Vector2(0, -18);
            iconAuraRt.sizeDelta = new Vector2(230, 230);
            var auraImg = iconAuraGo.AddComponent<Image>();
            auraImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritesRoot + "art_fx_burst.png");
            auraImg.color = new Color(0f, 0.95f, 1f, 0.45f);
            auraImg.raycastTarget = false;

            var iconGo = CreateUIObject("PackIcon", summaryGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0, -32);
            iconRt.sizeDelta = new Vector2(170, 170);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var pTitleGo = CreateUIObject("PackTitle", summaryGo.transform);
            var pTitleRt = pTitleGo.GetComponent<RectTransform>();
            pTitleRt.anchorMin = pTitleRt.anchorMax = new Vector2(0.5f, 1f);
            pTitleRt.pivot = new Vector2(0.5f, 1f);
            pTitleRt.anchoredPosition = new Vector2(0, -214);
            pTitleRt.sizeDelta = new Vector2(300, 32);
            var pTitleTmp = pTitleGo.AddComponent<TextMeshProUGUI>();
            pTitleTmp.font = fontBangers != null ? fontBangers : fontSans;
            pTitleTmp.fontSize = 24;
            pTitleTmp.text = "Gói Kim Cương";
            pTitleTmp.color = Color.white;
            pTitleTmp.alignment = TextAlignmentOptions.Center;
            pTitleTmp.raycastTarget = false;

            var pValGo = CreateUIObject("PackValue", summaryGo.transform);
            var pValRt = pValGo.GetComponent<RectTransform>();
            pValRt.anchorMin = pValRt.anchorMax = new Vector2(0.5f, 1f);
            pValRt.pivot = new Vector2(0.5f, 1f);
            pValRt.anchoredPosition = new Vector2(0, -250);
            pValRt.sizeDelta = new Vector2(300, 36);
            var pValTmp = pValGo.AddComponent<TextMeshProUGUI>();
            pValTmp.font = fontBangers != null ? fontBangers : fontSans;
            pValTmp.fontSize = 28;
            pValTmp.text = "+6,800 Kim Cương";
            pValTmp.color = new Color(0f, 1f, 0.95f, 1f);
            pValTmp.alignment = TextAlignmentOptions.Center;
            pValTmp.raycastTarget = false;

            // Bonus badge pill
            var pBonusGo = CreateUIObject("PackBonusBadge", summaryGo.transform);
            var pBonusRt = pBonusGo.GetComponent<RectTransform>();
            pBonusRt.anchorMin = pBonusRt.anchorMax = new Vector2(0.5f, 1f);
            pBonusRt.pivot = new Vector2(0.5f, 1f);
            pBonusRt.anchoredPosition = new Vector2(0, -288);
            pBonusRt.sizeDelta = new Vector2(165, 32);
            var pBonusBg = pBonusGo.AddComponent<Image>();
            pBonusBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "badge_bonus.png");
            pBonusBg.raycastTarget = false;

            var pBonusTxtGo = CreateUIObject("Txt", pBonusGo.transform);
            SetStretch(pBonusTxtGo);
            var pBonusTmp = pBonusTxtGo.AddComponent<TextMeshProUGUI>();
            pBonusTmp.font = fontBangers != null ? fontBangers : fontSans;
            pBonusTmp.fontSize = 15;
            pBonusTmp.text = "+2,000 Thưởng";
            pBonusTmp.color = Color.white;
            pBonusTmp.alignment = TextAlignmentOptions.Center;
            pBonusTmp.raycastTarget = false;

            // In-game Perk Stamp
            var perkStampGo = CreateUIObject("PerkStamp", summaryGo.transform);
            var perkStampRt = perkStampGo.GetComponent<RectTransform>();
            perkStampRt.anchorMin = perkStampRt.anchorMax = new Vector2(0.5f, 1f);
            perkStampRt.pivot = new Vector2(0.5f, 1f);
            perkStampRt.anchoredPosition = new Vector2(0, -336);
            perkStampRt.sizeDelta = new Vector2(290, 48);
            var perkStampTmp = perkStampGo.AddComponent<TextMeshProUGUI>();
            perkStampTmp.font = fontSans;
            perkStampTmp.fontSize = 12;
            perkStampTmp.text = "<color=#00F5FF>GIAO DỊCH TỰ ĐỘNG 24/7</color>\nCộng trực tiếp vào tài khoản tức thì";
            perkStampTmp.color = new Color(0.72f, 0.84f, 0.98f, 0.95f);
            perkStampTmp.alignment = TextAlignmentOptions.Center;
            perkStampTmp.raycastTarget = false;

            // Price Container at bottom of left panel
            var pPriceBoxGo = CreateUIObject("PriceContainer", summaryGo.transform);
            var pPriceBoxRt = pPriceBoxGo.GetComponent<RectTransform>();
            pPriceBoxRt.anchorMin = pPriceBoxRt.anchorMax = new Vector2(0.5f, 0f);
            pPriceBoxRt.pivot = new Vector2(0.5f, 0f);
            pPriceBoxRt.anchoredPosition = new Vector2(0, 16);
            pPriceBoxRt.sizeDelta = new Vector2(290, 70);

            var pPriceLblGo = CreateUIObject("Label", pPriceBoxGo.transform);
            var pPriceLblRt = pPriceLblGo.GetComponent<RectTransform>();
            pPriceLblRt.anchorMin = pPriceLblRt.anchorMax = new Vector2(0.5f, 1f);
            pPriceLblRt.pivot = new Vector2(0.5f, 1f);
            pPriceLblRt.anchoredPosition = new Vector2(0, 0);
            pPriceLblRt.sizeDelta = new Vector2(270, 22);
            var pPriceLblTmp = pPriceLblGo.AddComponent<TextMeshProUGUI>();
            pPriceLblTmp.font = fontSans;
            pPriceLblTmp.fontSize = 12;
            pPriceLblTmp.text = "TỔNG TIỀN THANH TOÁN:";
            pPriceLblTmp.color = new Color(1f, 0.85f, 0.45f, 0.95f);
            pPriceLblTmp.alignment = TextAlignmentOptions.Center;
            pPriceLblTmp.raycastTarget = false;

            var pPriceGo = CreateUIObject("PackPrice", pPriceBoxGo.transform);
            var pPriceRt = pPriceGo.GetComponent<RectTransform>();
            pPriceRt.anchorMin = pPriceRt.anchorMax = new Vector2(0.5f, 0f);
            pPriceRt.pivot = new Vector2(0.5f, 0f);
            pPriceRt.anchoredPosition = new Vector2(0, 2);
            pPriceRt.sizeDelta = new Vector2(270, 42);
            var pPriceTmp = pPriceGo.AddComponent<TextMeshProUGUI>();
            pPriceTmp.font = fontBangers != null ? fontBangers : fontSans;
            pPriceTmp.fontSize = 36;
            pPriceTmp.text = "500,000 đ";
            pPriceTmp.color = new Color(1f, 0.92f, 0.2f, 1f);
            pPriceTmp.alignment = TextAlignmentOptions.Center;
            pPriceTmp.raycastTarget = false;

            // Right Side: Payment Info & QR
            var rightBoxGo = CreateUIObject("PaymentRightBox", dialogGo.transform);
            var rightBoxRt = rightBoxGo.GetComponent<RectTransform>();
            rightBoxRt.anchorMin = rightBoxRt.anchorMax = new Vector2(1, 0.5f);
            rightBoxRt.pivot = new Vector2(1, 0.5f);
            rightBoxRt.anchoredPosition = new Vector2(-42, 10);
            rightBoxRt.sizeDelta = new Vector2(520, 530);

            // Channel Tabs
            var channelsGo = CreateUIObject("ChannelsBar", rightBoxGo.transform);
            var channelsRt = channelsGo.GetComponent<RectTransform>();
            channelsRt.anchorMin = channelsRt.anchorMax = new Vector2(0.5f, 1f);
            channelsRt.pivot = new Vector2(0.5f, 1f);
            channelsRt.anchoredPosition = new Vector2(0, 0);
            channelsRt.sizeDelta = new Vector2(520, 46);

            var tabChannelNormal = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "tab_chan_inactive.png");

            var (btnVietQR, bgVietQR, imgVietQRSelect) = CreateChannelTab("Btn_VietQR", channelsGo.transform, new Vector2(-174, 0), "VIETQR", SpritesRoot + "icon_vietqr.png", tabChannelNormal, fontBangers);
            var (btnZaloPay, bgZaloPay, imgZaloPaySelect) = CreateChannelTab("Btn_ZaloPay", channelsGo.transform, new Vector2(0, 0), "ZALOPAY", SpritesRoot + "icon_zalopay.png", tabChannelNormal, fontBangers);
            var (btnMoMo, bgMoMo, imgMoMoSelect) = CreateChannelTab("Btn_MoMo", channelsGo.transform, new Vector2(174, 0), "MOMO", SpritesRoot + "icon_momo.png", tabChannelNormal, fontBangers);

            // QR Cyber HUD Container (Replaces white grocery paper stand)
            var qrBoxGo = CreateUIObject("QRContainer", rightBoxGo.transform);
            var qrBoxRt = qrBoxGo.GetComponent<RectTransform>();
            qrBoxRt.anchorMin = qrBoxRt.anchorMax = new Vector2(0.5f, 1f);
            qrBoxRt.pivot = new Vector2(0.5f, 1f);
            qrBoxRt.anchoredPosition = new Vector2(0, -50);
            qrBoxRt.sizeDelta = new Vector2(270, 260);
            var qrBoxBg = qrBoxGo.AddComponent<Image>();
            qrBoxBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "qr_cyber_hud_plate.png");
            qrBoxBg.type = Image.Type.Sliced;

            // HUD Top Title
            var standTitleGo = CreateUIObject("StandTitle", qrBoxGo.transform);
            var standTitleRt = standTitleGo.GetComponent<RectTransform>();
            standTitleRt.anchorMin = standTitleRt.anchorMax = new Vector2(0.5f, 1f);
            standTitleRt.pivot = new Vector2(0.5f, 1f);
            standTitleRt.anchoredPosition = new Vector2(0, -9);
            standTitleRt.sizeDelta = new Vector2(250, 22);
            var standTitleTmp = standTitleGo.AddComponent<TextMeshProUGUI>();
            standTitleTmp.font = fontBangers != null ? fontBangers : fontSans;
            standTitleTmp.fontSize = 15;
            standTitleTmp.text = "QUET MA THANH TOAN";
            standTitleTmp.color = new Color(0f, 0.95f, 1f, 1f);
            standTitleTmp.alignment = TextAlignmentOptions.Center;
            standTitleTmp.raycastTarget = false;

            // White QR Matrix Base Card (Pure white rounded square for perfect camera scanning)
            var qrWhiteGo = CreateUIObject("QRWhiteBase", qrBoxGo.transform);
            var qrWhiteRt = qrWhiteGo.GetComponent<RectTransform>();
            qrWhiteRt.anchorMin = qrWhiteRt.anchorMax = new Vector2(0.5f, 0.5f);
            qrWhiteRt.pivot = new Vector2(0.5f, 0.5f);
            qrWhiteRt.anchoredPosition = new Vector2(0, -6);
            qrWhiteRt.sizeDelta = new Vector2(174, 174);
            var qrWhiteImg = qrWhiteGo.AddComponent<Image>();
            qrWhiteImg.color = Color.white;
            qrWhiteImg.raycastTarget = false;

            // QR RawImage (ZXing procedural texture)
            var qrRawGo = CreateUIObject("QRRawImage", qrWhiteGo.transform);
            var qrRawRt = qrRawGo.GetComponent<RectTransform>();
            qrRawRt.anchorMin = qrRawRt.anchorMax = new Vector2(0.5f, 0.5f);
            qrRawRt.pivot = new Vector2(0.5f, 0.5f);
            qrRawRt.anchoredPosition = Vector2.zero;
            qrRawRt.sizeDelta = new Vector2(166, 166);
            var qrRawImg = qrRawGo.AddComponent<RawImage>();
            qrRawImg.raycastTarget = false;

            // QR Fallback Image
            var qrImgGo = CreateUIObject("QRImageFallback", qrWhiteGo.transform);
            var qrImgRt = qrImgGo.GetComponent<RectTransform>();
            qrImgRt.anchorMin = qrImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            qrImgRt.pivot = new Vector2(0.5f, 0.5f);
            qrImgRt.anchoredPosition = Vector2.zero;
            qrImgRt.sizeDelta = new Vector2(166, 166);
            var qrImg = qrImgGo.AddComponent<Image>();
            qrImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "qr_vietqr.png");
            qrImg.preserveAspect = true;
            qrImg.raycastTarget = false;
            qrImgGo.SetActive(false); // Default to RawImage

            // Stand Subtext
            var standSubGo = CreateUIObject("StandSub", qrBoxGo.transform);
            var standSubRt = standSubGo.GetComponent<RectTransform>();
            standSubRt.anchorMin = standSubRt.anchorMax = new Vector2(0.5f, 0f);
            standSubRt.pivot = new Vector2(0.5f, 0f);
            standSubRt.anchoredPosition = new Vector2(0, 7);
            standSubRt.sizeDelta = new Vector2(260, 18);
            var standSubTmp = standSubGo.AddComponent<TextMeshProUGUI>();
            standSubTmp.font = fontSans;
            standSubTmp.fontSize = 11;
            standSubTmp.text = "Quet bang App Ngan hang hoac Vi dien tu";
            standSubTmp.color = new Color(0.45f, 0.75f, 0.98f, 1f);
            standSubTmp.alignment = TextAlignmentOptions.Center;
            standSubTmp.raycastTarget = false;

            // Timer countdown badge (Well spaced below QR container)
            var timerGo = CreateUIObject("TimerText", rightBoxGo.transform);
            var timerRt = timerGo.GetComponent<RectTransform>();
            timerRt.anchorMin = timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.pivot = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0, -320);
            timerRt.sizeDelta = new Vector2(360, 24);
            var timerTmp = timerGo.AddComponent<TextMeshProUGUI>();
            timerTmp.font = fontBangers != null ? fontBangers : fontSans;
            timerTmp.fontSize = 16;
            timerTmp.text = "Hiệu lực giao dịch: 05:00";
            timerTmp.color = new Color(1f, 0.88f, 0.35f, 1f);
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.raycastTarget = false;

            // Detail Rows Container Card
            var infoBoxGo = CreateUIObject("DetailRows", rightBoxGo.transform);
            var infoBoxRt = infoBoxGo.GetComponent<RectTransform>();
            infoBoxRt.anchorMin = infoBoxRt.anchorMax = new Vector2(0.5f, 0f);
            infoBoxRt.pivot = new Vector2(0.5f, 0f);
            infoBoxRt.anchoredPosition = new Vector2(0, 10);
            infoBoxRt.sizeDelta = new Vector2(515, 160);
            var infoBoxBg = infoBoxGo.AddComponent<Image>();
            infoBoxBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "detail_card_plate.png");
            infoBoxBg.type = Image.Type.Sliced;
            infoBoxBg.color = Color.white;
            infoBoxBg.raycastTarget = false;

            var copyBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "btn_copy_comic.png");

            var (bankTmp, _) = CreateDetailRow("BankRow", infoBoxGo.transform, new Vector2(0, 56), "Ngân Hàng:", "MB Bank (Quân Đội)", null, copyBtnSprite, fontSans, false);
            var (accNumTmp, copyAccBtn) = CreateDetailRow("AccNumRow", infoBoxGo.transform, new Vector2(0, 28), "Số Tài Khoản:", "0988 776 655", null, copyBtnSprite, fontSans, true);
            var (accNameTmp, _) = CreateDetailRow("AccNameRow", infoBoxGo.transform, new Vector2(0, 0), "Chủ TK:", "RHYTHM GAME STUDIO", null, copyBtnSprite, fontSans, false);
            var (contentTmp, copyContentBtn) = CreateDetailRow("ContentRow", infoBoxGo.transform, new Vector2(0, -28), "Nội Dung CK:", "NAP79410", null, copyBtnSprite, fontSans, true, valueColor: new Color(0f, 1f, 0.95f), isValueBangers: true, fontBangers: fontBangers);
            var (amountPayTmp, _) = CreateDetailRow("AmountRow", infoBoxGo.transform, new Vector2(0, -56), "Số Tiền:", "500,000 đ", null, copyBtnSprite, fontSans, false, valueColor: new Color(1f, 0.92f, 0.25f), isValueBangers: true, fontBangers: fontBangers);

            // Bottom Confirm Button (Authentic Comic Slanted Hot-Pink / Violet Pill)
            var confirmBtnGo = CreateUIObject("ConfirmPaymentButton", dialogGo.transform);
            var confirmRt = confirmBtnGo.GetComponent<RectTransform>();
            confirmRt.anchorMin = confirmRt.anchorMax = new Vector2(0.5f, 0);
            confirmRt.pivot = new Vector2(0.5f, 0);
            confirmRt.anchoredPosition = new Vector2(0, 22);
            confirmRt.sizeDelta = new Vector2(500, 58);
            var confirmImg = confirmBtnGo.AddComponent<Image>();
            confirmImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "btn_confirm_comic.png");
            confirmImg.type = Image.Type.Sliced;
            var confirmBtn = confirmBtnGo.AddComponent<Button>();
            SetButtonColors(confirmBtn);

            var confirmTxtGo = CreateUIObject("Text", confirmBtnGo.transform);
            SetStretch(confirmTxtGo);
            var confirmTmp = confirmTxtGo.AddComponent<TextMeshProUGUI>();
            confirmTmp.font = fontBangers != null ? fontBangers : fontSans;
            confirmTmp.fontSize = 24;
            confirmTmp.text = "XÁC NHẬN ĐÃ CHUYỂN KHOẢN (TEST)";
            confirmTmp.color = Color.white;
            confirmTmp.alignment = TextAlignmentOptions.Center;
            confirmTmp.raycastTarget = false;

            return new ModalBuildResult
            {
                modalGo = modalGo,
                dialogRt = dialogRt,
                packIconImg = iconImg,
                packTitleTmp = pTitleTmp,
                packValueTmp = pValTmp,
                modalPackBonusTmp = pBonusTmp,
                packPriceTmp = pPriceTmp,
                btnVietQR = btnVietQR,
                btnZaloPay = btnZaloPay,
                btnMoMo = btnMoMo,
                bgVietQR = bgVietQR,
                bgZaloPay = bgZaloPay,
                bgMoMo = bgMoMo,
                imgVietQRSelect = imgVietQRSelect,
                imgZaloPaySelect = imgZaloPaySelect,
                imgMoMoSelect = imgMoMoSelect,
                qrRawImg = qrRawImg,
                qrImg = qrImg,
                bankNameTmp = bankTmp,
                accountNumTmp = accNumTmp,
                accountNameTmp = accNameTmp,
                transferContentTmp = contentTmp,
                amountToPayTmp = amountPayTmp,
                timerCountdownTmp = timerTmp,
                copyAccountNumBtn = copyAccBtn,
                copyContentBtn = copyContentBtn,
                confirmPaymentBtn = confirmBtn,
                closePaymentModalBtn = closeBtn
            };
        }

        private static (Button, Image, Image) CreateChannelTab(string name, Transform parent, Vector2 pos, string label, string iconPath, Sprite bgNormal, TMP_FontAsset font)
        {
            var btnGo = CreateUIObject(name, parent);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = new Vector2(166, 44);

            var bgImg = btnGo.AddComponent<Image>();
            bgImg.sprite = bgNormal;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            SetButtonColors(btn);

            // Channel Icon
            var iconGo = CreateUIObject("Icon", btnGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(24, 0);
            iconRt.sizeDelta = new Vector2(28, 28);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var textGo = CreateUIObject("Text", btnGo.transform);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.32f, 0);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 17;
            tmp.text = label;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            // Highlight border
            var hlGo = CreateUIObject("HighlightBorder", btnGo.transform);
            SetStretch(hlGo);
            var hlImg = hlGo.AddComponent<Image>();
            hlImg.color = new Color(0f, 1f, 0.9f, 0.15f);
            hlImg.raycastTarget = false;
            hlGo.SetActive(false);

            return (btn, bgImg, hlImg);
        }

        private static (TMP_Text, Button) CreateDetailRow(string name, Transform parent, Vector2 pos, string label, string val, Sprite rowBgSprite, Sprite copyBtnSprite, TMP_FontAsset font, bool hasCopy, Color? valueColor = null, bool isValueBangers = false, TMP_FontAsset fontBangers = null)
        {
            var rowGo = CreateUIObject(name, parent);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(490, 26);

            if (rowBgSprite != null)
            {
                var bg = rowGo.AddComponent<Image>();
                bg.sprite = rowBgSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
                bg.raycastTarget = false;
            }

            var labelGo = CreateUIObject("Label", rowGo.transform);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0.32f, 1);
            labelRt.offsetMin = new Vector2(14, 0);
            labelRt.offsetMax = Vector2.zero;
            var lblTmp = labelGo.AddComponent<TextMeshProUGUI>();
            lblTmp.font = font;
            lblTmp.fontSize = 13;
            lblTmp.text = label;
            lblTmp.color = new Color(0.60f, 0.76f, 0.98f, 1f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;

            var valGo = CreateUIObject("Value", rowGo.transform);
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.33f, 0);
            valRt.anchorMax = hasCopy ? new Vector2(0.80f, 1) : new Vector2(0.98f, 1);
            valRt.offsetMin = valRt.offsetMax = Vector2.zero;
            var valTmp = valGo.AddComponent<TextMeshProUGUI>();
            valTmp.font = (isValueBangers && fontBangers != null) ? fontBangers : font;
            valTmp.fontSize = isValueBangers ? 17 : 14;
            valTmp.text = val;
            valTmp.color = valueColor ?? Color.white;
            valTmp.alignment = TextAlignmentOptions.MidlineLeft;
            valTmp.raycastTarget = false;

            Button copyBtn = null;
            if (hasCopy)
            {
                var copyGo = CreateUIObject("CopyBtn", rowGo.transform);
                var copyRt = copyGo.GetComponent<RectTransform>();
                copyRt.anchorMin = copyRt.anchorMax = new Vector2(1, 0.5f);
                copyRt.pivot = new Vector2(1, 0.5f);
                copyRt.anchoredPosition = new Vector2(-6, 0);
                copyRt.sizeDelta = new Vector2(86, 25);
                var copyImg = copyGo.AddComponent<Image>();
                copyImg.sprite = copyBtnSprite;
                copyImg.type = Image.Type.Sliced;
                copyImg.color = Color.white;
                copyBtn = copyGo.AddComponent<Button>();
                SetButtonColors(copyBtn);

                var cTxtGo = CreateUIObject("Txt", copyGo.transform);
                SetStretch(cTxtGo);
                var cTmp = cTxtGo.AddComponent<TextMeshProUGUI>();
                cTmp.font = fontBangers != null ? fontBangers : font;
                cTmp.fontSize = 13;
                cTmp.text = "SAO CHÉP";
                cTmp.color = Color.white;
                cTmp.alignment = TextAlignmentOptions.Center;
                cTmp.raycastTarget = false;
            }

            return (valTmp, copyBtn);
        }

        public struct SuccessToastRefs
        {
            public GameObject rootGo;
            public RectTransform dialogRt;
            public TMP_Text titleTmp;
            public Image iconImg;
            public Image auraImg;
            public TMP_Text packNameTmp;
            public TMP_Text amountTmp;
            public TMP_Text bonusTmp;
            public TMP_Text messageTmp;
            public Button okBtn;
            public Button overlayBtn;
        }

        private static SuccessToastRefs BuildSuccessToast(Transform parent, TMP_FontAsset fontBangers, TMP_FontAsset fontSans)
        {
            var toastGo = CreateUIObject("SuccessPopup", parent);
            SetStretch(toastGo);
            var ovImg = toastGo.AddComponent<Image>();
            ovImg.color = new Color(0.015f, 0.008f, 0.05f, 0.88f);
            var ovBtn = toastGo.AddComponent<Button>();
            ovBtn.transition = Selectable.Transition.None;

            var dialogGo = CreateUIObject("Dialog", toastGo.transform);
            var dialogRt = dialogGo.GetComponent<RectTransform>();
            dialogRt.anchorMin = dialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRt.pivot = new Vector2(0.5f, 0.5f);
            dialogRt.sizeDelta = new Vector2(800, 620);
            var dImg = dialogGo.AddComponent<Image>();
            dImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/LuckyWheel/Sprites/RewardPopupFrame_Neon.png");
            dImg.color = Color.white;

            // Title
            var titleGo = CreateUIObject("Title", dialogGo.transform);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0, 138);
            titleRt.sizeDelta = new Vector2(560, 48);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.font = fontBangers != null ? fontBangers : fontSans;
            titleTmp.fontSize = 38;
            titleTmp.text = "GIAO DỊCH THÀNH CÔNG!";
            titleTmp.color = new Color(0f, 1f, 0.9f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Subtitle
            var subGo = CreateUIObject("Subtitle", dialogGo.transform);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = new Vector2(0, 102);
            subRt.sizeDelta = new Vector2(500, 26);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.font = fontBangers != null ? fontBangers : fontSans;
            subTmp.fontSize = 16;
            subTmp.text = "// GIAO DỊCH HOÀN TẤT //";
            subTmp.color = new Color(1f, 0.23f, 0.63f, 1f);
            subTmp.alignment = TextAlignmentOptions.Center;

            // Item Aura / Glow
            var auraGo = CreateUIObject("ItemAura", dialogGo.transform);
            var auraRt = auraGo.GetComponent<RectTransform>();
            auraRt.anchorMin = auraRt.anchorMax = new Vector2(0.5f, 0.5f);
            auraRt.pivot = new Vector2(0.5f, 0.5f);
            auraRt.anchoredPosition = new Vector2(0, 10);
            auraRt.sizeDelta = new Vector2(180, 180);
            var auraImg = auraGo.AddComponent<Image>();
            auraImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "glow_aura_cyan.png");
            auraImg.color = new Color(0f, 0.85f, 1f, 0.35f);
            auraImg.raycastTarget = false;

            // Item Frame
            var itemFrameGo = CreateUIObject("ItemFrame", dialogGo.transform);
            var itemFrameRt = itemFrameGo.GetComponent<RectTransform>();
            itemFrameRt.anchorMin = itemFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
            itemFrameRt.pivot = new Vector2(0.5f, 0.5f);
            itemFrameRt.anchoredPosition = new Vector2(0, 10);
            itemFrameRt.sizeDelta = new Vector2(145, 145);
            var itemFrameImg = itemFrameGo.AddComponent<Image>();
            itemFrameImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/LuckyWheel/Sprites/RewardItemFrame_Neon.png");
            itemFrameImg.color = Color.white;
            itemFrameImg.raycastTarget = false;

            // Item Icon
            var iconGo = CreateUIObject("ItemIcon", itemFrameGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(100, 100);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier2.png");
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Pack Name
            var packNameGo = CreateUIObject("PackName", dialogGo.transform);
            var packNameRt = packNameGo.GetComponent<RectTransform>();
            packNameRt.anchorMin = packNameRt.anchorMax = new Vector2(0.5f, 0.5f);
            packNameRt.pivot = new Vector2(0.5f, 0.5f);
            packNameRt.anchoredPosition = new Vector2(0, -80);
            packNameRt.sizeDelta = new Vector2(500, 32);
            var packNameTmp = packNameGo.AddComponent<TextMeshProUGUI>();
            packNameTmp.font = fontBangers != null ? fontBangers : fontSans;
            packNameTmp.fontSize = 24;
            packNameTmp.text = "TÚI KIM CƯƠNG VỪA";
            packNameTmp.color = new Color(0.88f, 0.93f, 1f, 1f);
            packNameTmp.alignment = TextAlignmentOptions.Center;

            // Amount
            var amtGo = CreateUIObject("Amount", dialogGo.transform);
            var amtRt = amtGo.GetComponent<RectTransform>();
            amtRt.anchorMin = amtRt.anchorMax = new Vector2(0.5f, 0.5f);
            amtRt.pivot = new Vector2(0.5f, 0.5f);
            amtRt.anchoredPosition = new Vector2(0, -118);
            amtRt.sizeDelta = new Vector2(500, 48);
            var amtTmp = amtGo.AddComponent<TextMeshProUGUI>();
            amtTmp.font = fontBangers != null ? fontBangers : fontSans;
            amtTmp.fontSize = 42;
            amtTmp.text = "+250 KIM CƯƠNG";
            amtTmp.color = new Color(0f, 1f, 0.9f, 1f);
            amtTmp.alignment = TextAlignmentOptions.Center;

            // Bonus text
            var bonusGo = CreateUIObject("Bonus", dialogGo.transform);
            var bonusRt = bonusGo.GetComponent<RectTransform>();
            bonusRt.anchorMin = bonusRt.anchorMax = new Vector2(0.5f, 0.5f);
            bonusRt.pivot = new Vector2(0.5f, 0.5f);
            bonusRt.anchoredPosition = new Vector2(0, -156);
            bonusRt.sizeDelta = new Vector2(500, 26);
            var bonusTmp = bonusGo.AddComponent<TextMeshProUGUI>();
            bonusTmp.font = fontBangers != null ? fontBangers : fontSans;
            bonusTmp.fontSize = 18;
            bonusTmp.text = "";
            bonusTmp.color = new Color(1f, 0.85f, 0.1f, 1f);
            bonusTmp.alignment = TextAlignmentOptions.Center;
            bonusGo.SetActive(false);

            // Message / Note
            var msgGo = CreateUIObject("Message", dialogGo.transform);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = msgRt.anchorMax = new Vector2(0.5f, 0.5f);
            msgRt.pivot = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = new Vector2(0, -156);
            msgRt.sizeDelta = new Vector2(500, 26);
            var msgTmp = msgGo.AddComponent<TextMeshProUGUI>();
            msgTmp.font = fontSans;
            msgTmp.fontSize = 17;
            msgTmp.text = "Đã cộng trực tiếp vào ví tài khoản";
            msgTmp.color = new Color(0.4f, 1f, 0.65f, 1f);
            msgTmp.alignment = TextAlignmentOptions.Center;

            // Claim / Ok Button
            var okBtnGo = CreateUIObject("OkButton", dialogGo.transform);
            var okRt = okBtnGo.GetComponent<RectTransform>();
            okRt.anchorMin = okRt.anchorMax = new Vector2(0.5f, 0.5f);
            okRt.pivot = new Vector2(0.5f, 0.5f);
            okRt.anchoredPosition = new Vector2(0, -228);
            okRt.sizeDelta = new Vector2(340, 76);
            var okImg = okBtnGo.AddComponent<Image>();
            okImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/LuckyWheel/Sprites/RewardClaimButton_Neon.png");
            var okBtn = okBtnGo.AddComponent<Button>();
            SetButtonColors(okBtn);

            var okTxtGo = CreateUIObject("Txt", okBtnGo.transform);
            SetStretch(okTxtGo);
            var okTxtRt = okTxtGo.GetComponent<RectTransform>();
            okTxtRt.anchoredPosition = new Vector2(0, 2);
            var okTmp = okTxtGo.AddComponent<TextMeshProUGUI>();
            okTmp.font = fontBangers != null ? fontBangers : fontSans;
            okTmp.fontSize = 30;
            okTmp.text = "XÁC NHẬN";
            okTmp.color = Color.white;
            okTmp.alignment = TextAlignmentOptions.Center;

            return new SuccessToastRefs
            {
                rootGo = toastGo,
                dialogRt = dialogRt,
                titleTmp = titleTmp,
                iconImg = iconImg,
                auraImg = auraImg,
                packNameTmp = packNameTmp,
                amountTmp = amtTmp,
                bonusTmp = bonusTmp,
                messageTmp = msgTmp,
                okBtn = okBtn,
                overlayBtn = ovBtn
            };
        }

        private static (GameObject, TMP_Text) BuildCopyToast(Transform parent, TMP_FontAsset font)
        {
            var toastGo = CreateUIObject("CopyToast", parent);
            var toastRt = toastGo.GetComponent<RectTransform>();
            toastRt.anchorMin = toastRt.anchorMax = new Vector2(0.5f, 0.15f);
            toastRt.pivot = new Vector2(0.5f, 0.5f);
            toastRt.sizeDelta = new Vector2(340, 50);
            var img = toastGo.AddComponent<Image>();
            img.color = new Color(0.05f, 0.2f, 0.35f, 0.95f);

            var txtGo = CreateUIObject("Txt", toastGo.transform);
            SetStretch(txtGo);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 18;
            tmp.text = "Đã sao chép!";
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return (toastGo, tmp);
        }

        private static TopUpPackData[] GetDefaultGemPacks()
        {
            return new TopUpPackData[]
            {
                new TopUpPackData { packId = "gem_10k",  title = "Túi Kim Cương Nhỏ",  mainAmount = 100,   bonusAmount = 10,   priceVnd = 10000,  badgeText = "+10%",      currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier1.png") },
                new TopUpPackData { packId = "gem_20k",  title = "Túi Kim Cương Vừa",  mainAmount = 220,   bonusAmount = 30,   priceVnd = 20000,  badgeText = "+15%",      currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier2.png") },
                new TopUpPackData { packId = "gem_50k",  title = "Rương Kim Cương",    mainAmount = 580,   bonusAmount = 100,  priceVnd = 50000,  badgeText = "PHỔ BIẾN",  currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier3.png") },
                new TopUpPackData { packId = "gem_100k", title = "Hòm Kim Cương Lớn",  mainAmount = 1250,  bonusAmount = 250,  priceVnd = 100000, badgeText = "HOT DEAL",  currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier4.png") },
                new TopUpPackData { packId = "gem_200k", title = "Kho Báu Kim Cương",  mainAmount = 2600,  bonusAmount = 600,  priceVnd = 200000, badgeText = "SIÊU LỜI",   currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier5.png") },
                new TopUpPackData { packId = "gem_500k", title = "Vương Miện Kim Cương", mainAmount = 6800, bonusAmount = 2000, priceVnd = 500000, badgeText = "VIP KING", currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_gem_tier6.png") }
            };
        }

        private static TopUpPackData[] GetDefaultCoinPacks()
        {
            return new TopUpPackData[]
            {
                new TopUpPackData { packId = "coin_10k",  title = "Túi Xu Đồng",        mainAmount = 10000,   bonusAmount = 0,      priceVnd = 10000,  badgeText = "",          currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier1.png") },
                new TopUpPackData { packId = "coin_20k",  title = "Túi Xu Bạc",         mainAmount = 25000,   bonusAmount = 3000,   priceVnd = 20000,  badgeText = "+12%",      currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier2.png") },
                new TopUpPackData { packId = "coin_50k",  title = "Đống Vàng Thịnh Vượng", mainAmount = 70000, bonusAmount = 15000,  priceVnd = 50000,  badgeText = "BÁN CHẠY",  currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier3.png") },
                new TopUpPackData { packId = "coin_100k", title = "Rương Tiền Vàng",    mainAmount = 150000,  bonusAmount = 40000,  priceVnd = 100000, badgeText = "HOT",      currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier4.png") },
                new TopUpPackData { packId = "coin_200k", title = "Két Vàng Hoàng Gia",  mainAmount = 350000,  bonusAmount = 100000, priceVnd = 200000, badgeText = "+30%",      currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier5.png") },
                new TopUpPackData { packId = "coin_500k", title = "Kho Vàng Bất Tận",   mainAmount = 1000000, bonusAmount = 350000, priceVnd = 500000, badgeText = "ĐẠI GIA",  currencyType = CurrencyType.Coin, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_coin_tier6.png") }
            };
        }

        private static TopUpPackData[] GetDefaultSpecialPacks()
        {
            return new TopUpPackData[]
            {
                new TopUpPackData { packId = "pass_week",    title = "Vé Tuần Năng Động",         mainAmount = 650,  bonusAmount = 0,   priceVnd = 45000,  badgeText = "THẺ TUẦN",   currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_pass_week.png") },
                new TopUpPackData { packId = "pass_month",   title = "Vé Tháng Siêu Cấp VIP",     mainAmount = 4000, bonusAmount = 500, priceVnd = 129000, badgeText = "SIÊU GIÁ TRỊ", currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_pass_month.png") },
                new TopUpPackData { packId = "pack_starter", title = "Gói Quà Khởi Đầu Tân Thủ", mainAmount = 500, bonusAmount = 50000, bonusTickets = 5, priceVnd = 29000, badgeText = "X3 GIÁ TRỊ", currencyType = CurrencyType.Gem, iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesRoot + "pack_starter.png") }
            };
        }

        private static void ConfigureAllSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
            }
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetButtonColors(Button btn)
        {
            var cb = btn.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;
        }

        private static void SetArrayProperty(SerializedProperty prop, List<NapTienCardView> views)
        {
            prop.arraySize = views.Count;
            for (int i = 0; i < views.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }
        }

        private static void SetPacksArrayProperty(SerializedProperty prop, TopUpPackData[] packs)
        {
            if (prop == null || packs == null) return;
            prop.arraySize = packs.Length;
            for (int i = 0; i < packs.Length; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                var p = packs[i];
                element.FindPropertyRelative("packId").stringValue = p.packId;
                element.FindPropertyRelative("title").stringValue = p.title;
                element.FindPropertyRelative("mainAmount").intValue = p.mainAmount;
                element.FindPropertyRelative("bonusAmount").intValue = p.bonusAmount;
                element.FindPropertyRelative("priceVnd").intValue = p.priceVnd;
                element.FindPropertyRelative("badgeText").stringValue = p.badgeText;
                element.FindPropertyRelative("iconSprite").objectReferenceValue = p.iconSprite;
                element.FindPropertyRelative("currencyType").enumValueIndex = (int)p.currencyType;
                element.FindPropertyRelative("bonusTickets").intValue = p.bonusTickets;
            }
        }
    }
}
