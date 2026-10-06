using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Hop xac nhan truoc khi mua vat pham
public class ShopConfirmDialog : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button overlayButton;
    [SerializeField] private Image artImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite gemSprite;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirm;
    private Coroutine popRoutine;

    public bool IsOpen { get { return root.activeSelf; } }

    private void Awake()
    {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Hide);
        if (overlayButton != null) overlayButton.onClick.AddListener(Hide);
        root.SetActive(false);
    }

    public void Show(ShopItemData item, Action confirmAction)
    {
        onConfirm = confirmAction;
        artImage.sprite = item.artSprite;
        nameText.text = item.displayName;
        priceText.text = item.price.ToString("N0");
        currencyIcon.sprite = item.currency == CurrencyType.Coin ? coinSprite : gemSprite;

        root.SetActive(true);
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(Pop());
    }

    public void Hide()
    {
        onConfirm = null;
        root.SetActive(false);
    }

    private void Confirm()
    {
        var action = onConfirm;
        Hide();
        if (action != null) action();
    }

    private IEnumerator Pop()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.14f;
            float s = Mathf.Lerp(0.85f, 1f, 1f - (1f - t) * (1f - t));
            panel.localScale = Vector3.one * s;
            yield return null;
        }
        panel.localScale = Vector3.one;
    }
}
