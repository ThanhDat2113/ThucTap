using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Spawn prefab nhan vat da chon (CharacterSelection.SelectedId) khi vao gameplay.
    /// Gan vao 1 object bat ky trong GameplayScene (vd: "Systems").
    /// </summary>
    public class PlayerCharacterApplier : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Keo TAT CA CharacterData assets vao day.")]
        [SerializeField] private CharacterData[] allCharacters;

        [Header("Vi tri spawn")]
        [Tooltip("Transform danh dau cho nhan vat dung tren san khau. De trong = (0,0,0).")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Neu khac rong, su dung ten object nay de tim spawnPoint khi field tren trong (vd: 'Main1').")]
        [SerializeField] private string spawnPointFallbackName = "Main1";

        private GameObject spawned;

        private void Awake()
        {
            // Xac dinh vi tri spawn
            Transform anchor = spawnPoint;
            if (anchor == null && !string.IsNullOrEmpty(spawnPointFallbackName))
            {
                var go = GameObject.Find(spawnPointFallbackName);
                if (go != null) anchor = go.transform;
            }
            Vector3 pos = anchor != null ? anchor.position : Vector3.zero;
            Quaternion rot = anchor != null ? anchor.rotation : Quaternion.identity;

            // An 2 model cu neu con trong scene
            var main1 = GameObject.Find("Main1");
            var main2 = GameObject.Find("Main2");
            if (main1 != null) main1.SetActive(false);
            if (main2 != null) main2.SetActive(false);

            // Tim CharacterData theo SelectedId
            var data = Find(CharacterSelection.SelectedId);
            if (data == null)
            {
                Debug.LogError($"[PlayerCharacterApplier] Khong tim thay CharacterData cho id '{CharacterSelection.SelectedId}'. Kiem tra All Characters da keo du chua.");
                return;
            }
            if (data.gameplayPrefab == null)
            {
                Debug.LogError($"[PlayerCharacterApplier] CharacterData '{data.Id}' chua co Gameplay Prefab!");
                return;
            }

            // Spawn
            spawned = Instantiate(data.gameplayPrefab, pos, rot);

            // Wire JudgementSystem cho CharacterController qua Initialize()
            var cc = spawned.GetComponent<CharacterController>();
            if (cc != null)
            {
                var js = FindFirstObjectByType<JudgementSystem>();
                if (js == null)
                    Debug.LogError("[PlayerCharacterApplier] Không tìm thấy JudgementSystem — nhân vật sẽ không nhảy theo note.");
                cc.Initialize(js);
            }

            Debug.Log($"[PlayerCharacterApplier] Spawned '{data.gameplayPrefab.name}' for id '{data.Id}' at {pos}");
        }

        private void OnDestroy()
        {
            if (spawned != null) Destroy(spawned);
        }

        private CharacterData Find(string id)
        {
            if (string.IsNullOrEmpty(id) || allCharacters == null) return null;
            foreach (var c in allCharacters)
                if (c != null && c.Id == id) return c;
            return null;
        }
    }
}
