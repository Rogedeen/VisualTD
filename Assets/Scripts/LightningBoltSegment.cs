using UnityEngine;
using DigitalRuby.LightningBolt;

[RequireComponent(typeof(LightningBoltScript))]
public class LightningBoltSegment : MonoBehaviour
{
    private LightningBoltScript boltScript;
    private string poolTag = "LightningSegment";

    private void Awake()
    {
        boltScript = GetComponent<LightningBoltScript>();
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.8f; // Daha kalın ve belirgin yapalım
            
            // Eğer materyal varsa rengini maviye çekelim (HDR destekliyse parlar)
            if (lr.material != null)
            {
                lr.material.SetColor("_Color", new Color(0.2f, 0.5f, 1f, 1f));
                lr.material.SetColor("_TintColor", new Color(0.2f, 0.5f, 1f, 1f));
                lr.material.EnableKeyword("_EMISSION");
                lr.material.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 1f, 1f) * 2f);
            }
        }
    }

    public void Initialize(Vector3 start, Vector3 end, float duration, string tag = "LightningSegment")
    {
        poolTag = tag;
        
        // Culling (kamera görmeme) hatasını önlemek için objeyi ortaya taşı
        transform.position = (start + end) * 0.5f;

        boltScript.StartObject = null;
        boltScript.EndObject = null;
        boltScript.StartPosition = start;
        boltScript.EndPosition = end;
        boltScript.Duration = duration;
        
        boltScript.Trigger();
        
        // Obje deaktif olmadan hemen önce yıldırımın kendisi silinebilir, 
        // o yüzden deaktif süresini bir tık uzun tutuyoruz.
        CancelInvoke();
        Invoke(nameof(Deactivate), duration + 0.05f);
    }

    private void Deactivate()
    {
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
