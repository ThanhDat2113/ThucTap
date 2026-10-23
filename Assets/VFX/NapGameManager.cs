using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý scene NapGame:
/// PanelChonGoi (6 gói) -> PanelPhuongThuc (bấm ZaloPay / Ngân hàng thì hiện luôn hình QR) -> cộng xu.
/// Gắn script này vào object Canvas.
/// </summary>
public class NapGameManager : MonoBehaviour
{
    [System.Serializable]
    public class GoiNap
    {
        public Button nut;      // Button của gói
        public int soTien;      // Số tiền VNĐ
        public int soXu;        // Số xu nhận được
    }

    [Header("Panel")]
    public GameObject panelChonGoi;
    public GameObject panelPhuongThuc;

    [Header("6 gói nạp (kéo 6 Button vào)")]
    public GoiNap[] cacGoi = new GoiNap[6]
    {
        new GoiNap { soTien = 10000,  soXu = 100  },
        new GoiNap { soTien = 20000,  soXu = 200  },
        new GoiNap { soTien = 50000,  soXu = 500  },
        new GoiNap { soTien = 100000, soXu = 1000 },
        new GoiNap { soTien = 200000, soXu = 2000 },
        new GoiNap { soTien = 500000, soXu = 5000 },
    };

    [Header("Trong PanelPhuongThuc")]
    public Button nutZaloPay;           // object ZaloPay
    public Button nutNganHang;          // object Nganhang
    public GameObject qr;               // object QR (hình mã QR, ban đầu để tắt)
    [Tooltip("Ảnh QR test của bạn (tuỳ chọn). Trống = dùng ảnh sẵn có trên object QR, nếu cũng trống thì tự tạo QR giả")]
    public Sprite spriteQRTest;

    [Header("Xác nhận thanh toán (test)")]
    [Tooltip("Nút 'Đã thanh toán' (tuỳ chọn)")]
    public Button nutXacNhan;
    [Tooltip("Tự cộng xu sau X giây khi hiện QR (0 = tắt). Dùng để test khi chưa có nút xác nhận")]
    public float tuDongXacNhanSauGiay = 5f;

    [Header("Nút thoát")]
    public Button nutExitChonGoi;
    public Button nutExitPhuongThuc;
    [Tooltip("Tên scene quay về khi Exit ở PanelChonGoi (trống = chỉ ẩn)")]
    public string sceneQuayVe = "";

    [Header("Hiển thị (tuỳ chọn)")]
    public TMP_Text textXuHienTai;
    public TMP_Text textThongBao;

    private const string KEY_XU = "TongXu";
    private GoiNap goiDangChon;
    private string phuongThucDangChon;
    private Coroutine coTuDong;

    void Start()
    {
        foreach (var goi in cacGoi)
        {
            if (goi.nut == null) continue;
            GoiNap g = goi;
            g.nut.onClick.AddListener(() => ChonGoi(g));
        }

        if (nutZaloPay)  nutZaloPay.onClick.AddListener(() => HienQR("ZaloPay"));
        if (nutNganHang) nutNganHang.onClick.AddListener(() => HienQR("Ngân hàng"));

        if (nutXacNhan) nutXacNhan.onClick.AddListener(XacNhanThanhToan);

        if (nutExitChonGoi)    nutExitChonGoi.onClick.AddListener(ThoatKhoiNap);
        if (nutExitPhuongThuc) nutExitPhuongThuc.onClick.AddListener(ExitPhuongThuc);

        HienChonGoi();
        CapNhatXu();
        HienThongBao("");
    }

    // ---------- Chuyển panel ----------
    void HienChonGoi()
    {
        DungTuDong();
        panelChonGoi.SetActive(true);
        panelPhuongThuc.SetActive(false);
        if (qr) qr.SetActive(false);
        if (nutXacNhan) nutXacNhan.gameObject.SetActive(false);
    }

    void HienPhuongThuc()
    {
        panelChonGoi.SetActive(false);
        panelPhuongThuc.SetActive(true);
        if (qr) qr.SetActive(false);
        if (nutXacNhan) nutXacNhan.gameObject.SetActive(false);
    }

    // Exit trong PanelPhuongThuc: đang hiện QR thì tắt QR, không thì về chọn gói
    void ExitPhuongThuc()
    {
        if (qr != null && qr.activeSelf)
        {
            DungTuDong();
            qr.SetActive(false);
            if (nutXacNhan) nutXacNhan.gameObject.SetActive(false);
        }
        else
        {
            goiDangChon = null;
            HienChonGoi();
        }
    }

    void ThoatKhoiNap()
    {
        if (!string.IsNullOrEmpty(sceneQuayVe))
            SceneManager.LoadScene(sceneQuayVe);
        else
            gameObject.SetActive(false);
    }

    // ---------- Logic nạp ----------
    void ChonGoi(GoiNap goi)
    {
        goiDangChon = goi;
        HienThongBao($"Đã chọn gói {goi.soTien:N0}đ - {goi.soXu} xu");
        HienPhuongThuc();
    }

    // Bấm ZaloPay / Ngân hàng -> hiện luôn QR
    void HienQR(string phuongThuc)
    {
        if (goiDangChon == null || qr == null) return;
        phuongThucDangChon = phuongThuc;

        var img = qr.GetComponent<Image>();
        if (img != null)
        {
            if (spriteQRTest != null)
                img.sprite = spriteQRTest;
            else if (img.sprite == null)
                img.sprite = TaoQRGia($"{phuongThuc}-{goiDangChon.soTien}");
            img.preserveAspect = true;
        }

        qr.SetActive(true);
        if (nutXacNhan) nutXacNhan.gameObject.SetActive(true);

        HienThongBao($"{phuongThuc}: quét mã để thanh toán {goiDangChon.soTien:N0}đ");

        DungTuDong();
        if (tuDongXacNhanSauGiay > 0)
            coTuDong = StartCoroutine(TuDongXacNhan());
    }

    IEnumerator TuDongXacNhan()
    {
        yield return new WaitForSeconds(tuDongXacNhanSauGiay);
        coTuDong = null;
        XacNhanThanhToan();
    }

    void DungTuDong()
    {
        if (coTuDong != null)
        {
            StopCoroutine(coTuDong);
            coTuDong = null;
        }
    }

    void XacNhanThanhToan()
    {
        if (goiDangChon == null) return;
        DungTuDong();

        // TODO: Khi làm thật, hãy kiểm tra giao dịch với server/API cổng thanh toán rồi mới cộng xu.
        int xuMoi = LayXu() + goiDangChon.soXu;
        PlayerPrefs.SetInt(KEY_XU, xuMoi);
        PlayerPrefs.Save();

        CapNhatXu();
        HienThongBao($"Nạp thành công {goiDangChon.soXu} xu qua {phuongThucDangChon}!");

        goiDangChon = null;
        HienChonGoi();
    }

    // ---------- Tạo hình QR giả để test (KHÔNG quét được) ----------
    Sprite TaoQRGia(string seedText)
    {
        const int modules = 25;
        const int scale = 10;
        const int quiet = 2;
        int size = (modules + quiet * 2) * scale;

        var rng = new System.Random(seedText.GetHashCode());
        bool[,] m = new bool[modules, modules];
        for (int x = 0; x < modules; x++)
            for (int y = 0; y < modules; y++)
                m[x, y] = rng.Next(2) == 0;

        VeDinhVi(m, 0, 0);
        VeDinhVi(m, modules - 7, 0);
        VeDinhVi(m, 0, modules - 7);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                int mx = px / scale - quiet;
                int my = py / scale - quiet;
                bool den = mx >= 0 && my >= 0 && mx < modules && my < modules && m[mx, my];
                tex.SetPixel(px, py, den ? Color.black : Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void VeDinhVi(bool[,] m, int ox, int oy)
    {
        for (int x = -1; x <= 7; x++)
        {
            for (int y = -1; y <= 7; y++)
            {
                int gx = ox + x, gy = oy + y;
                if (gx < 0 || gy < 0 || gx >= m.GetLength(0) || gy >= m.GetLength(1)) continue;

                bool vien = x == 0 || x == 6 || y == 0 || y == 6;
                bool tam = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                bool trongRanh = x == -1 || x == 7 || y == -1 || y == 7;
                m[gx, gy] = !trongRanh && (vien || tam);
            }
        }
    }

    // ---------- Tiện ích ----------
    public static int LayXu() => PlayerPrefs.GetInt(KEY_XU, 0);

    void CapNhatXu()
    {
        if (textXuHienTai) textXuHienTai.text = $"Xu: {LayXu():N0}";
    }

    void HienThongBao(string noiDung)
    {
        if (textThongBao) textThongBao.text = noiDung;
        Debug.Log(noiDung);
    }
}