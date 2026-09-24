using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.UI
{
    [DisallowMultipleComponent]
    public class HitEffectSystem : MonoBehaviour
    {
        [Header("Popup")]
        public HitResultPopup popupPrefab;

        private Transform container;

        private void Start()
        {
            var go = new GameObject("Popups");
            container = go.transform;
            container.SetParent(transform, false);
        }

        public void SpawnPopup(Judgement judgement, Vector3 worldPos)
        {
            HitResultPopup instance;
            if (popupPrefab != null)
            {
                instance = Instantiate(popupPrefab, worldPos, Quaternion.identity, container);
            }
            else
            {
                var go = new GameObject($"Popup_{judgement}");
                go.transform.SetParent(container, true);
                go.transform.position = worldPos;
                instance = go.AddComponent<HitResultPopup>();
            }
            instance.Configure(judgement);
        }
    }
}
