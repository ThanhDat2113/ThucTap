using UnityEngine;
using Luan.Dialogue;

namespace RhythmGame
{
    /// <summary>
    /// Spawn prefab doi thu cua tuan hien tai (StorySession.Week.opponentPrefab) khi vao GameplayScene.
    /// Giong PlayerCharacterApplier nhung o ben phai san khau, owner = NoteOwner.Opponent.
    /// Gan vao 1 object bat ky trong GameplayScene (vd: "Systems").
    /// Neu tuan chua co opponentPrefab, bo qua (GameplayStage se fallback dung mang opponents + int opponent cu).
    /// </summary>
    public class OpponentCharacterApplier : MonoBehaviour
    {
        [Header("Vi tri spawn")]
        [Tooltip("Transform danh dau cho doi thu dung tren san khau. De trong = (0,0,0).")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Neu khac rong, su dung ten object nay de tim spawnPoint khi field tren trong (vd: 'Main2').")]
        [SerializeField] private string spawnPointFallbackName = "Main2";

        private GameObject spawned;

        private void Awake()
        {
            // Chi spawn doi thu khi dang o Story Mode
            if (!StorySession.Active || StorySession.Week == null)
            {
                return;
            }

            var prefab = StorySession.Week.opponentPrefab;
            if (prefab == null)
            {
                // Khong co opponentPrefab -> GameplayStage se dung mang opponents + int opponent (backward compatible)
                Debug.Log($"[OpponentCharacterApplier] Tuan {StorySession.Week.weekNumber} chua co opponentPrefab — bo qua, GameplayStage se dung fallback.");
                return;
            }

            Transform anchor = spawnPoint;
            if (anchor == null && !string.IsNullOrEmpty(spawnPointFallbackName))
            {
                var go = GameObject.Find(spawnPointFallbackName);
                if (go != null) anchor = go.transform;
            }
            Vector3 pos = anchor != null ? anchor.position : Vector3.zero;
            Quaternion rot = anchor != null ? anchor.rotation : Quaternion.identity;

            spawned = Instantiate(prefab, pos, rot);
            spawned.name = "Opponent_" + prefab.name;

            // Gan owner = Opponent cho CharacterController (neu co)
            var cc = spawned.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.owner = NoteOwner.Opponent;
                var js = FindFirstObjectByType<JudgementSystem>();
                if (js == null)
                    Debug.LogWarning("[OpponentCharacterApplier] Khong tim thay JudgementSystem — doi thu khong nhay theo note.");
                cc.Initialize(js);
            }

            // Wire OpponentAI (neu prefab co) voi he thong gameplay
            var ai = spawned.GetComponent<OpponentAI>();
            if (ai != null)
            {
                ai.spawner = FindFirstObjectByType<NoteSpawner>();
                ai.judgementSystem = FindFirstObjectByType<JudgementSystem>();
                ai.characterController = cc;
                ai.battleUI = FindFirstObjectByType<BattleUIController>();
                ai.SetActive(false); // se duoc bat khi GameManager vao Playing

                // Dang ky voi GameManager de GameManager.co the bat/tat AI
                var gm = FindFirstObjectByType<GameManager>();
                if (gm != null) gm.opponentAI = ai;
            }

            Debug.Log($"[OpponentCharacterApplier] Spawned opponent '{prefab.name}' for week {StorySession.Week.weekNumber} at {pos}");
        }

        private void OnDestroy()
        {
            if (spawned != null) Destroy(spawned);
        }
    }
}
