using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Luan.MainMenu
{
    /// <summary>
    /// Script quản lý ẩn / hiện mật khẩu bằng 2 nút Button riêng biệt (HienMatKhau và AnMatKhau).
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/UI Password Toggle (Ẩn Hiện Mật Khẩu)")]
    public class UIPasswordToggle : MonoBehaviour
    {
        [Header("--- 1. Ô Nhập Mật Khẩu (TMP Input Field) ---")]
        [Tooltip("Kéo ô InputPassword vào đây")]
        [SerializeField] private TMP_InputField passwordInput;

        [Header("--- 2. Hai Nút Bấm Của Bạn (Buttons) ---")]
        [Tooltip("Kéo Button 'HienMatKhau' vào đây")]
        [SerializeField] private Button showPasswordButton;

        [Tooltip("Kéo Button 'AnMatKhau' vào đây")]
        [SerializeField] private Button hidePasswordButton;

        [Header("--- 3. Trạng Thái Mặc Định ---")]
        [Tooltip("Mặc định ban đầu có hiện mật khẩu không? (Để False để mật khẩu ban đầu là dấu ••••)")]
        [SerializeField] private bool isPasswordVisible = false;

        private void Awake()
        {
            // Tự gán sự kiện click cho 2 nút
            if (showPasswordButton != null)
            {
                showPasswordButton.onClick.AddListener(ShowPassword);
            }

            if (hidePasswordButton != null)
            {
                hidePasswordButton.onClick.AddListener(HidePassword);
            }
        }

        private void Start()
        {
            ApplyVisibilityState();
        }

        /// <summary>
        /// Bấm nút Hiện Mật Khẩu
        /// </summary>
        public void ShowPassword()
        {
            isPasswordVisible = true;
            ApplyVisibilityState();
        }

        /// <summary>
        /// Bấm nút Ẩn Mật Khẩu
        /// </summary>
        public void HidePassword()
        {
            isPasswordVisible = false;
            ApplyVisibilityState();
        }

        private void ApplyVisibilityState()
        {
            // 1. Cập nhật dạng hiển thị của ô mật khẩu
            if (passwordInput != null)
            {
                passwordInput.contentType = isPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;

                passwordInput.ForceLabelUpdate();
            }

            // 2. Tự động bật/tắt giữa 2 nút HienMatKhau và AnMatKhau
            if (showPasswordButton != null)
            {
                // Khi đang ẩn mật khẩu -> Hiện nút HienMatKhau để người chơi bấm
                showPasswordButton.gameObject.SetActive(!isPasswordVisible);
            }

            if (hidePasswordButton != null)
            {
                // Khi đang hiện mật khẩu -> Hiện nút AnMatKhau để người chơi bấm
                hidePasswordButton.gameObject.SetActive(isPasswordVisible);
            }
        }
    }
}
