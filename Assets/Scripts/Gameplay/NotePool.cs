using UnityEngine;
using UnityEngine.Pool;

namespace RhythmGame
{
    /// <summary>
    /// Quản lý vòng đời GameObject của note bằng UnityEngine.Pool.ObjectPool,
    /// tránh Instantiate/Destroy liên tục gây giật khi note dày đặc.
    /// </summary>
    public class NotePool : MonoBehaviour
    {
        public NoteView notePrefab;
        public Transform poolParent;

        ObjectPool<NoteView> pool;

        void Awake()
        {
            pool = new ObjectPool<NoteView>(
                createFunc: () => Instantiate(notePrefab, poolParent),
                actionOnGet: n => n.gameObject.SetActive(true),
                actionOnRelease: n => n.gameObject.SetActive(false),
                actionOnDestroy: n => Destroy(n.gameObject),
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 64);
        }

        public NoteView Get() => pool.Get();
        public void Release(NoteView n) => pool.Release(n);
    }
}
