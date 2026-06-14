using UnityEngine;

public class ConveyorArrowBlink : MonoBehaviour
{
    [SerializeField] private Renderer[] arrowRenderers;
    [SerializeField] private Color dimColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color brightColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float blinkSpeed = 2.5f;
    [SerializeField, Min(0f)] private float phaseOffsetPerArrow = 0.18f;
    [SerializeField] private bool toggleVisibility = true;
    [SerializeField, Range(0.05f, 0.95f)] private float visibleThreshold = 0.35f;
    [SerializeField] private bool useEmission = true;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private MaterialPropertyBlock propertyBlock;
    private bool[] originalRendererEnabled;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (arrowRenderers == null || arrowRenderers.Length == 0)
        {
            arrowRenderers = GetComponentsInChildren<Renderer>(true);
        }

        originalRendererEnabled = new bool[arrowRenderers.Length];
        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            originalRendererEnabled[i] = arrowRenderers[i] != null && arrowRenderers[i].enabled;
        }
    }

    private void Update()
    {
        if (arrowRenderers == null || arrowRenderers.Length == 0 || blinkSpeed <= 0f)
        {
            return;
        }

        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            Renderer arrowRenderer = arrowRenderers[i];
            if (arrowRenderer == null) continue;

            float phase = (Time.time * blinkSpeed) - (i * phaseOffsetPerArrow);
            float pulse = (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f;
            Color color = Color.Lerp(dimColor, brightColor, pulse);

            if (toggleVisibility)
            {
                arrowRenderer.enabled = pulse >= visibleThreshold;
            }

            arrowRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(BaseColorId, color);
            if (useEmission)
            {
                propertyBlock.SetColor(EmissionColorId, color * pulse);
            }
            arrowRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnDisable()
    {
        if (arrowRenderers == null) return;

        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            Renderer arrowRenderer = arrowRenderers[i];
            if (arrowRenderer != null)
            {
                if (originalRendererEnabled != null && i < originalRendererEnabled.Length)
                {
                    arrowRenderer.enabled = originalRendererEnabled[i];
                }

                arrowRenderer.SetPropertyBlock(null);
            }
        }
    }
}
