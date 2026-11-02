using UnityEngine;
using UnityEngine.Pool;

namespace RhythmGame
{
    public class NotePool : MonoBehaviour
    {
        public NoteView notePrefab;
        public Transform poolParent;

        ObjectPool<NoteView> pool;

        void Awake()
        {
            pool = new ObjectPool<NoteView>(
                createFunc: () => Instantiate(notePrefab, poolParent),
                actionOnGet: n => { if (n != null) n.gameObject.SetActive(true); },
                actionOnRelease: n => { if (n != null) n.gameObject.SetActive(false); },
                actionOnDestroy: n => { if (n != null && n.gameObject != null) Destroy(n.gameObject); },
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 64);
        }

        public NoteView Get() => pool.Get();
        public void Release(NoteView n) => pool.Release(n);
    }
}