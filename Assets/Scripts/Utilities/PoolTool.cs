using UnityEngine;
using UnityEngine.Pool;
[DefaultExecutionOrder(-100)]
public class PoolTool : MonoBehaviour
{
    public GameObject objPrefab;
    private ObjectPool<GameObject> pool;
    // Start is called before the first frame update
    private void Awake()
    {
        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(objPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(value: true),
            actionOnRelease: (obj) => obj.SetActive(value: false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
        PreFillPool(count: 10);
    }

    private void PreFillPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    public GameObject GetObjectFromPool()
    {
        return pool.Get();
    }

    public void ReleaseObjectToPool(GameObject obj)
    {
        pool.Release(obj);
    }
}
