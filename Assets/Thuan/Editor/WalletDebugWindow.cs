using System;
using UnityEditor;
using UnityEngine;

namespace Thuan.Editor
{
    /// <summary>
    /// Cửa sổ Tool hỗ trợ thêm/bớt tiền tệ (Coin, Diamond, Ticket) và đồng bộ PlayFab khi test trong Editor.
    /// </summary>
    public class WalletDebugWindow : EditorWindow
    {
        [MenuItem("Window/Shop & Wallet Debug Window", priority = 200)]
        [MenuItem("Tools/Tiền Tệ (Test)/Bảng Điều Khiển Ví (Wallet Tool) %#w")]
        [MenuItem("Thuan/Shop & Wallet Debug Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<WalletDebugWindow>("Wallet Tool");
            window.minSize = new Vector2(340, 420);
            window.Show();
        }

        [MenuItem("Tools/Tiền Tệ (Test)/+10,000 Coins (Vàng)")]
        public static void QuickAdd10kCoins()
        {
            Wallet.Add(CurrencyType.Coin, 10000);
            Debug.Log($"[Wallet Tool] Đã thêm 10,000 Vàng. Số dư mới: {Wallet.Coins:N0}");
        }

        [MenuItem("Tools/Tiền Tệ (Test)/+1,000 Gems (Kim Cương)")]
        public static void QuickAdd1kGems()
        {
            Wallet.Add(CurrencyType.Gem, 1000);
            Debug.Log($"[Wallet Tool] Đã thêm 1,000 Kim Cương. Số dư mới: {Wallet.Gems:N0}");
        }

        [MenuItem("Tools/Tiền Tệ (Test)/+10 Tickets (Vé Quay)")]
        public static void QuickAdd10Tickets()
        {
            Wallet.AddTickets(10);
            Debug.Log($"[Wallet Tool] Đã thêm 10 Vé Quay. Số dư mới: {Wallet.Tickets:N0}");
        }

        [MenuItem("Tools/Tiền Tệ (Test)/Reset Ví Về 0")]
        public static void QuickResetWallet()
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc muốn đặt lại toàn bộ Vàng, Kim Cương, Vé Quay về 0?", "Đồng ý", "Hủy"))
            {
                Wallet.SetCurrencies(0, 0, 0);
                Debug.Log("[Wallet Tool] Đã reset số dư về 0.");
            }
        }

        private int customCoinsToAdd = 5000;
        private int customGemsToAdd = 500;
        private int customTicketsToAdd = 5;

        private Vector2 scrollPos;

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("💎 BẢNG ĐIỀU KHIỂN VÍ TIỀN TỆ (WALLET TOOL)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Dùng để cộng/trừ số dư phục vụ việc test game và kiểm tra PlayFab.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6);

            // 1. Hiển thị số dư hiện tại
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("📊 SỐ DƯ HIỆN TẠI (LOCAL & PLAYFAB)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawCurrencyRow("🟡 Vàng (Coins):", $"{Wallet.Coins:N0}", Color.yellow);
            DrawCurrencyRow("💎 Kim Cương (Gems):", $"{Wallet.Gems:N0}", new Color(0.2f, 0.8f, 1f));
            DrawCurrencyRow("🎫 Vé Quay (Tickets):", $"{Wallet.Tickets:N0}", new Color(1f, 0.6f, 0.2f));

            string loggedInStatus = Wallet.IsLoggedIn ? "🟢 Đã đăng nhập PlayFab (Sẵn sàng đồng bộ)" : "⚪ Chưa đăng nhập PlayFab (Lưu trên máy)";
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Trạng thái:", loggedInStatus, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 2. Thao tác nhanh (Quick Add)
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("⚡ CỘNG NHANH TIỀN TỆ", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Vàng
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Vàng:", GUILayout.Width(60));
            if (GUILayout.Button("+1,000")) { Wallet.Add(CurrencyType.Coin, 1000); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+10,000")) { Wallet.Add(CurrencyType.Coin, 10000); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+50,000")) { Wallet.Add(CurrencyType.Coin, 50000); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            // Kim Cương
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("K.Cương:", GUILayout.Width(60));
            if (GUILayout.Button("+100")) { Wallet.Add(CurrencyType.Gem, 100); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+1,000")) { Wallet.Add(CurrencyType.Gem, 1000); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+5,000")) { Wallet.Add(CurrencyType.Gem, 5000); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            // Vé Quay
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Vé quay:", GUILayout.Width(60));
            if (GUILayout.Button("+1")) { Wallet.AddTickets(1); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+5")) { Wallet.AddTickets(5); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("+10")) { Wallet.AddTickets(10); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 3. Tùy chỉnh số lượng
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎯 TÙY CHỌN SỐ LƯỢNG", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Coins custom
            EditorGUILayout.BeginHorizontal();
            customCoinsToAdd = EditorGUILayout.IntField("Vàng cần cộng:", customCoinsToAdd);
            if (GUILayout.Button("+ Cộng", GUILayout.Width(60))) { Wallet.Add(CurrencyType.Coin, customCoinsToAdd); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("Gán =", GUILayout.Width(55))) { Wallet.SetCoins(customCoinsToAdd); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            // Gems custom
            EditorGUILayout.BeginHorizontal();
            customGemsToAdd = EditorGUILayout.IntField("K.Cương cần cộng:", customGemsToAdd);
            if (GUILayout.Button("+ Cộng", GUILayout.Width(60))) { Wallet.Add(CurrencyType.Gem, customGemsToAdd); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("Gán =", GUILayout.Width(55))) { Wallet.SetGems(customGemsToAdd); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            // Tickets custom
            EditorGUILayout.BeginHorizontal();
            customTicketsToAdd = EditorGUILayout.IntField("Vé cần cộng:", customTicketsToAdd);
            if (GUILayout.Button("+ Cộng", GUILayout.Width(60))) { Wallet.AddTickets(customTicketsToAdd); TryAutoSyncIfInPlayMode(); }
            if (GUILayout.Button("Gán =", GUILayout.Width(55))) { Wallet.SetTickets(customTicketsToAdd); TryAutoSyncIfInPlayMode(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 4. Đồng bộ PlayFab thủ công & Reset
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🌐 ĐỒNG BỘ PLAYFAB & ĐẶT LẠI", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = Application.isPlaying && Wallet.IsLoggedIn;
            if (GUILayout.Button("📤 Đồng bộ ngay lên PlayFab"))
            {
                Wallet.SyncToPlayFab((success) =>
                {
                    if (success) EditorUtility.DisplayDialog("Thành công", "Đã đồng bộ số dư lên PlayFab!", "OK");
                    else EditorUtility.DisplayDialog("Thất bại", "Không thể đồng bộ. Vui lòng kiểm tra kết nối!", "OK");
                }, force: true);
            }
            if (GUILayout.Button("📥 Tải số dư từ PlayFab"))
            {
                Wallet.FetchFromPlayFab((success) =>
                {
                    if (success) EditorUtility.DisplayDialog("Thành công", "Đã tải số dư mới nhất từ PlayFab!", "OK");
                    else EditorUtility.DisplayDialog("Thất bại", "Không thể lấy số dư từ PlayFab!", "OK");
                });
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Lưu ý: Để kiểm tra đồng bộ trực tiếp lên server PlayFab qua mạng, hãy bật Play Mode trong Unity.", MessageType.Info);
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("🔄 Đặt lại khuyến mãi 20% Vòng Quay (Reset 20% Discount)"))
            {
                PlayerPrefs.DeleteKey("LUCKY_WHEEL_FIRST_TEN_DISCOUNT_USED");
                string username = PlayerPrefs.GetString("ACCOUNT_USERNAME", string.Empty);
                if (!string.IsNullOrEmpty(username))
                {
                    PlayerPrefs.DeleteKey($"LUCKY_WHEEL_FIRST_TEN_DISCOUNT_USED_{username}");
                }
                PlayerPrefs.Save();
                var purchaseUI = UnityEngine.Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelPurchaseUI>();
                if (purchaseUI != null) purchaseUI.Refresh();
                EditorUtility.DisplayDialog("Thành công", "Đã reset trạng thái khuyến mãi 20% cho lần quay x10 đầu tiên!", "OK");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎰 DEBUG BẢO HIỂM VÒNG QUAY (GACHA PITY 80)", EditorStyles.boldLabel);
            int currentPity = PlayerPrefs.GetInt("LUCKY_WHEEL_BONUS_PROGRESS", 0);
            EditorGUILayout.LabelField("Tiến độ hiện tại:", $"{currentPity} / 80 lượt", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Đặt 79/80 (Lượt kế tiếp nổ)"))
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", 79);
                PlayerPrefs.Save();
                var p = UnityEngine.Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelEditablePanels>();
                if (p != null) p.SetBonusProgress(79);
            }
            if (GUILayout.Button("Đặt 80/80 (Đã chạm bảo hiểm)"))
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", 80);
                PlayerPrefs.Save();
                var p = UnityEngine.Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelEditablePanels>();
                if (p != null) p.SetBonusProgress(80);
            }
            if (GUILayout.Button("Reset về 0/80"))
            {
                PlayerPrefs.SetInt("LUCKY_WHEEL_BONUS_PROGRESS", 0);
                PlayerPrefs.Save();
                var p = UnityEngine.Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelEditablePanels>();
                if (p != null) p.SetBonusProgress(0);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ Đặt lại toàn bộ số dư về 0 (Reset Wallet)"))
            {
                QuickResetWallet();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private static void TryAutoSyncIfInPlayMode()
        {
            if (Application.isPlaying && Wallet.IsLoggedIn)
            {
                Wallet.SyncToPlayFab(force: true);
            }
        }

        private void DrawCurrencyRow(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = valueColor;
            EditorGUILayout.LabelField(value, style);
            EditorGUILayout.EndHorizontal();
        }
    }
}

