using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private int lockedSlots = 3;
    [SerializeField] private Sprite[] lockedSprites;

    [Header("Grid")]
    [SerializeField] private CharacterCardUI cardTemplate;
    [SerializeField] private RectTransform grid;
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 cellSize = new Vector2(188, 225);
    [SerializeField] private Vector2 spacing = new Vector2(170, 190);

    [Header("Showcase")]
    [SerializeField] private Image fullArt;
    [SerializeField] private Image poster;

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text[] tagTexts = new TMP_Text[3];
    [SerializeField] private Image[] statFills = new Image[4];
    [SerializeField] private TMP_Text[] statValues = new TMP_Text[4];
    [SerializeField] private float statMaxWidth = 180f;
    [SerializeField] private TMP_Text quoteText;

    [Header("Buttons")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Image selectImage;
    [SerializeField] private Sprite selectSprite;
    [SerializeField] private Sprite selectDisabledSprite;
    [SerializeField] private Button randomButton;
    [SerializeField] private Button previewButton;

    [Header("Wallet / Toast")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private TMP_Text toastText;

    [Header("Audio")]
    [SerializeField] private AudioClip focusClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip lockedClip;

    private readonly List<CharacterCardUI> cards = new List<CharacterCardUI>();
    private CharacterCardUI focused;
    private Coroutine showRoutine, statRoutine, toastRoutine, randomRoutine;
    private Vector2 fullArtBasePos, posterBasePos;

    private void Awake()
    {
        if (selectButton != null) selectButton.onClick.AddListener(ConfirmSelect);
        if (randomButton != null) randomButton.onClick.AddListener(PickRandom);
        if (previewButton != null) previewButton.onClick.AddListener(PlayPreview);
        if (fullArt != null) fullArtBasePos = fullArt.rectTransform.anchoredPosition;
        if (poster != null) posterBasePos = poster.rectTransform.anchoredPosition;
    }

    private void OnEnable() { Wallet.Changed += RefreshWallet; }
    private void OnDisable() { Wallet.Changed -= RefreshWallet; }

    private void Start()
    {
        if (toastText != null) toastText.gameObject.SetActive(false);
        BuildGrid();
        RefreshWallet();

        CharacterCardUI start = cards.Find(c => !c.IsLocked && c.Data.Id == CharacterSelection.SelectedId);
        if (start == null) start = cards.Find(c => !c.IsLocked);
        RefreshSelected();
        if (start != null) Focus(start, false);
    }

    private void BuildGrid()
    {
        cardTemplate.gameObject.SetActive(false);
        int total = characters.Count + lockedSlots;
        for (int i = 0; i < total; i++)
        {
            var card = Instantiate(cardTemplate, grid);
            card.gameObject.SetActive(true);
            card.name = "Card_" + i;
            var rt = (RectTransform)card.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cellSize;
            int col = i % columns, row = i / columns;
            rt.anchoredPosition = new Vector2(cellSize.x / 2f + col * spacing.x, -cellSize.y / 2f - row * spacing.y);

            CharacterData data = i < characters.Count ? characters[i] : null;
            Sprite locked = lockedSprites != null && lockedSprites.Length > 0 ? lockedSprites[(i - characters.Count + lockedSprites.Length * 10) % lockedSprites.Length] : null;
            card.Setup(data, locked, OnCardClicked);
            cards.Add(card);
        }
    }

    private void OnCardClicked(CharacterCardUI card)
    {
        if (randomRoutine != null) return;
        if (card.IsLocked)
        {
            Play(lockedClip);
            ShowToast("COMING SOON");
            return;
        }
        if (card == focused) return;
        Play(focusClip);
        Focus(card, true);
    }

    private void Focus(CharacterCardUI card, bool animate)
    {
        if (focused != null) focused.SetFocused(false);
        focused = card;
        focused.SetFocused(true);
        ShowInfo(card.Data, animate);
        RefreshSelectButton();
    }

    private void ShowInfo(CharacterData d, bool animate)
    {
        nameText.text = d.displayName;
        titleText.text = d.title;
        descriptionText.text = d.description;
        for (int i = 0; i < tagTexts.Length; i++)
        {
            if (tagTexts[i] == null) continue;
            bool has = d.tags != null && i < d.tags.Length && !string.IsNullOrEmpty(d.tags[i]);
            tagTexts[i].transform.parent.gameObject.SetActive(true);
            tagTexts[i].text = has ? d.tags[i] : "-";
        }
        if (quoteText != null) quoteText.text = d.quote;

        int[] values = { d.vocal, d.style, d.power, d.speed };
        for (int i = 0; i < statValues.Length; i++) if (statValues[i] != null) statValues[i].text = values[i].ToString();
        if (statRoutine != null) StopCoroutine(statRoutine);
        statRoutine = StartCoroutine(AnimateStats(values, animate ? 0.35f : 0f));

        bool hasFull = d.fullArt != null;
        fullArt.gameObject.SetActive(hasFull);
        poster.gameObject.SetActive(!hasFull);
        if (hasFull) { fullArt.sprite = d.fullArt; }
        else { poster.sprite = d.posterSprite != null ? d.posterSprite : d.cardSprite; poster.preserveAspect = true; }

        if (showRoutine != null) StopCoroutine(showRoutine);
        if (animate) showRoutine = StartCoroutine(SlideIn(hasFull ? fullArt : poster, hasFull ? fullArtBasePos : posterBasePos));
        else
        {
            fullArt.rectTransform.anchoredPosition = fullArtBasePos; fullArt.color = Color.white;
            poster.rectTransform.anchoredPosition = posterBasePos; poster.color = Color.white;
        }
    }

    private IEnumerator SlideIn(Image img, Vector2 basePos)
    {
        var rt = img.rectTransform;
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = 1f - (1f - k) * (1f - k);
            rt.anchoredPosition = basePos + new Vector2(-70f * (1f - e), 0f);
            img.color = new Color(1f, 1f, 1f, e);
            yield return null;
        }
        rt.anchoredPosition = basePos;
        img.color = Color.white;
        showRoutine = null;
    }

    private IEnumerator AnimateStats(int[] values, float dur)
    {
        float t = 0f;
        do
        {
            t += Time.unscaledDeltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);
            float e = 1f - (1f - k) * (1f - k);
            for (int i = 0; i < statFills.Length; i++)
            {
                if (statFills[i] == null) continue;
                var rt = statFills[i].rectTransform;
                rt.sizeDelta = new Vector2(Mathf.Max(8f, statMaxWidth * values[i] / 10f * e), rt.sizeDelta.y);
            }
            yield return null;
        } while (t < dur);
        statRoutine = null;
    }

    private void RefreshSelected()
    {
        string id = CharacterSelection.SelectedId;
        foreach (var c in cards) c.SetSelected(!c.IsLocked && c.Data.Id == id);
    }

    private void RefreshSelectButton()
    {
        // Nut SELECT luon sang (giong mockup); bam lai nhan vat dang chon se bao ALREADY SELECTED
        selectButton.interactable = true;
        if (selectImage != null) selectImage.sprite = selectSprite;
    }

    public void ConfirmSelect()
    {
        if (focused == null || focused.IsLocked || randomRoutine != null) return;
        if (focused.Data.Id == CharacterSelection.SelectedId) { ShowToast("ALREADY SELECTED"); return; }
        CharacterSelection.SelectedId = focused.Data.Id;
        RefreshSelected();
        RefreshSelectButton();
        Play(selectClip);
        ShowToast(focused.Data.displayName + " SELECTED!");
    }

    public void PickRandom()
    {
        if (randomRoutine != null) return;
        var pool = cards.FindAll(c => !c.IsLocked);
        if (pool.Count == 0) return;
        randomRoutine = StartCoroutine(RandomRoutine(pool));
    }

    private IEnumerator RandomRoutine(List<CharacterCardUI> pool)
    {
        CharacterCardUI last = focused;
        int steps = 10;
        for (int i = 0; i < steps; i++)
        {
            CharacterCardUI next;
            do { next = pool[Random.Range(0, pool.Count)]; } while (pool.Count > 1 && next == last);
            last = next;
            if (focused != null) focused.SetFocused(false);
            focused = next;
            focused.SetFocused(true);
            Play(focusClip);
            yield return new WaitForSecondsRealtime(0.05f + i * 0.012f);
        }
        randomRoutine = null;
        Focus(last, true);
    }

    private void PlayPreview()
    {
        if (focused == null) return;
        if (focused.Data.previewClip != null) Play(focused.Data.previewClip);
        else ShowToast("PREVIEW COMING SOON");
    }

    private void RefreshWallet()
    {
        if (coinText != null) coinText.text = Wallet.Coins.ToString("N0");
        if (gemText != null) gemText.text = Wallet.Gems.ToString("N0");
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
    }

    private void ShowToast(string msg)
    {
        if (toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(msg));
    }

    private IEnumerator ToastRoutine(string msg)
    {
        var rt = toastText.rectTransform;
        toastText.text = msg;
        toastText.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            float pop = t < 0.12f ? Mathf.Lerp(0.6f, 1.12f, t / 0.12f) : (t < 0.24f ? Mathf.Lerp(1.12f, 1f, (t - 0.12f) / 0.12f) : 1f);
            rt.localScale = new Vector3(pop, pop, 1f);
            toastText.alpha = t > 1f ? 1f - (t - 1f) / 0.4f : 1f;
            yield return null;
        }
        toastText.gameObject.SetActive(false);
        toastText.alpha = 1f;
        toastRoutine = null;
    }
}
