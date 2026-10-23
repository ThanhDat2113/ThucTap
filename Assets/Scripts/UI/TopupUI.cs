using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

[System.Serializable] public class CreateOrderReq { public string packageId; public string method; }
[System.Serializable] public class CreateOrderRes {
    public string orderId;
    public string payUrl;          // dùng cho ZaloPay
    public string qrUrl;           // dùng cho ngân hàng
    public string bankName, bankAccount, accountName, transferContent;
    public long amount;
}
[System.Serializable] public class OrderStatus { public string status; } // pending | paid | failed

public class TopupUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelChonGoi, panelPhuongThuc, panelQR, panelKetQua, panelLoading;

    [Header("Panel Phương thức")]
    public TMP_Text txtGoiDangChon;

    [Header("Panel QR")]
    public RawImage qrImage;
    public TMP_Text txtNganHang, txtSoTK, txtChuTK, txtSoTien, txtNoiDung, txtDemNguoc;

    [Header("Kết quả")]
    public TMP_Text txtKetQua;

    const string API = "https://api.tenbancuaban.com";   // đổi thành địa chỉ server của bạn
    string selectedPackage;
    string currentOrderId;
    string currentTransferContent;
    Coroutine pollCo;

    void Start() { ShowOnly(panelChonGoi); }

    void ShowOnly(GameObject p)
    {
        panelChonGoi.SetActive(p == panelChonGoi);
        panelPhuongThuc.SetActive(p == panelPhuongThuc);
        panelQR.SetActive(p == panelQR);
        panelKetQua.SetActive(p == panelKetQua);
    }

    // ---- Gắn các hàm này vào OnClick của nút ----

    // Nút gói nạp: truyền id gói, ví dụ "goi_50k"
    public void ChonGoi(string packageId)
    {
        selectedPackage = packageId;
        txtGoiDangChon.text = "Bạn đang nạp: " + packageId;
        ShowOnly(panelPhuongThuc);
    }

    public void ChonZaloPay() { StartCoroutine(TaoDon("zalopay")); }
    public void ChonNganHang() { StartCoroutine(TaoDon("bank")); }
    public void QuayLai() { ShowOnly(panelChonGoi); }

    public void SaoChepNoiDung()
    {
        GUIUtility.systemCopyBuffer = currentTransferContent;
    }

    public void Huy()
    {
        if (pollCo != null) StopCoroutine(pollCo);
        ShowOnly(panelChonGoi);
    }

    // ---- Gọi server ----

    IEnumerator TaoDon(string method)
    {
        panelLoading.SetActive(true);

        var body = JsonUtility.ToJson(new CreateOrderReq { packageId = selectedPackage, method = method });
        var req = new UnityWebRequest(API + "/topup/create", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + PlayerSession.Token); // token đăng nhập của bạn
        yield return req.SendWebRequest();

        panelLoading.SetActive(false);

        if (req.result != UnityWebRequest.Result.Success)
        {
            HienKetQua("Không tạo được đơn. Vui lòng thử lại.");
            yield break;
        }

        var res = JsonUtility.FromJson<CreateOrderRes>(req.downloadHandler.text);
        currentOrderId = res.orderId;
        currentTransferContent = res.transferContent;

        if (method == "zalopay")
        {
            Application.OpenURL(res.payUrl);   // mở app ZaloPay hoặc trình duyệt
            HienKetQua("Đang chờ thanh toán trên ZaloPay...");
        }
        else
        {
            txtNganHang.text = res.bankName;
            txtSoTK.text = res.bankAccount;
            txtChuTK.text = res.accountName;
            txtSoTien.text = res.amount.ToString("N0") + "đ";
            txtNoiDung.text = res.transferContent;
            ShowOnly(panelQR);
            StartCoroutine(TaiQR(res.qrUrl));
        }

        pollCo = StartCoroutine(HoiTrangThai());
    }

    IEnumerator TaiQR(string url)
    {
        var tex = UnityWebRequestTexture.GetTexture(url);
        yield return tex.SendWebRequest();
        if (tex.result == UnityWebRequest.Result.Success)
            qrImage.texture = DownloadHandlerTexture.GetContent(tex);
    }

    // Cứ 3 giây hỏi server "đơn này trả tiền chưa?", tối đa 10 phút
    IEnumerator HoiTrangThai()
    {
        for (int i = 0; i < 200; i++)
        {
            yield return new WaitForSeconds(3);

            var r = UnityWebRequest.Get(API + "/topup/status?orderId=" + currentOrderId);
            r.SetRequestHeader("Authorization", "Bearer " + PlayerSession.Token);
            yield return r.SendWebRequest();
            if (r.result != UnityWebRequest.Result.Success) continue;

            var s = JsonUtility.FromJson<OrderStatus>(r.downloadHandler.text);
            if (s.status == "paid")
            {
                HienKetQua("Nạp thành công! Xu đã được cộng vào tài khoản.");
                // TODO: gọi hàm làm mới số dư xu ở đây
                yield break;
            }
            if (s.status == "failed")
            {
                HienKetQua("Giao dịch thất bại hoặc đã hết hạn.");
                yield break;
            }
        }
        HienKetQua("Hết thời gian chờ. Nếu bạn đã chuyển tiền, xu sẽ được cộng sau ít phút.");
    }

    void HienKetQua(string msg)
    {
        txtKetQua.text = msg;
        ShowOnly(panelKetQua);
    }
}