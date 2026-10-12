using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Thuan/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Thong tin")]
    public string displayName = "KAI";
    public string title = "THE STREET VOICE";
    [TextArea(2, 4)] public string description;
    [Tooltip("3 the: o 1 hong, o 2 xanh, o 3 tim")]
    public string[] tags = { "RAPPER", "STREET", "ALL ROUND" };
    public string quote = "\"LOUDER THAN YESTERDAY\"";

    [Header("Chi so (0-10)")]
    [Range(0, 10)] public int vocal = 8;
    [Range(0, 10)] public int style = 8;
    [Range(0, 10)] public int power = 8;
    [Range(0, 10)] public int speed = 8;

    [Header("Hinh")]
    [Tooltip("The nho trong luoi chon")]
    public Sprite cardSprite;
    [Tooltip("Hinh toan than tach nen (neu co) - hien to ben trai")]
    public Sprite fullArt;
    [Tooltip("Dung khi chua co fullArt - anh chan dung hien trong khung ben trai")]
    public Sprite posterSprite;

    [Header("Khac")]
    public AudioClip previewClip;
    public bool unlocked = true;

    public string Id { get { return name; } }
}
