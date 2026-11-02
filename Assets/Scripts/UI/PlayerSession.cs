/// <summary>
/// Giữ phiên đăng nhập hiện tại của người chơi. TopupUI dùng PlayerSession.Token để gửi lên server.
///
/// CÁCH DÙNG: sau khi người chơi đăng nhập thành công, gán token server trả về:
///     PlayerSession.Token = tokenTuServer;
/// Khi đăng xuất:
///     PlayerSession.Clear();
///
/// Token chỉ giữ trong bộ nhớ (tắt game là mất, phải đăng nhập lại). Không nên lưu token thanh toán
/// vào PlayerPrefs vì PlayerPrefs là văn bản thường, ai cũng đọc được.
/// </summary>
public static class PlayerSession
{
    static string token = "";

    public static string Token
    {
        get { return token; }
        set { token = value ?? ""; }
    }

    public static bool IsLoggedIn { get { return !string.IsNullOrEmpty(token); } }

    public static void Clear() { token = ""; }
}
