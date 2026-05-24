using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        // Havuzda eleman kalmadıysa güvenlik önlemi: dinamik genişletme
        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null && pool.prefab != null)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                poolDictionary[tag].Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"Havuzda yeterli {tag} kalmadi ve prefab bulunamadi!");
                return null;
            }
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // OnEnable fonksiyonunun KESİN tetiklenmesi için önce kapatıp sonra açıyoruz
        objectToSpawn.SetActive(false); 
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true); 

        return objectToSpawn;
    }

    // Objelerin öldüğünde çağıracağı YENİ fonksiyon
    public void ReturnToPool(string tag, GameObject obj)
    {
        obj.SetActive(false);
        if (poolDictionary.ContainsKey(tag))
        {
            poolDictionary[tag].Enqueue(obj);
        }
    }
}