using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [Tooltip("The color to flash when damaged")]
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("How long the flash lasts in seconds")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("The renderers to flash. If empty, it will auto-find them.")]
    [SerializeField] private Renderer[] renderers;

    private Color[] originalColors;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                if (renderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = renderers[i].material.color;
                }
                else if (renderers[i].material.HasProperty("_BaseColor")) // Support for URP
                {
                    originalColors[i] = renderers[i].material.GetColor("_BaseColor");
                }
            }
        }
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1. Change to Flash Color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = flashColor;
                else if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", flashColor);
            }
        }

        // 2. Wait
        yield return new WaitForSeconds(flashDuration);

        // 3. Revert to Original Color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
                else if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            }
        }
    }
}
