using UnityEngine;

/// <summary>
/// Attach to CubeA, CubeB, SphereC.
/// Holds the Anchor reference and manages material state.
/// </summary>
public class ShapeTarget : MonoBehaviour
{
    [Header("Anchor (child empty object, positioned above shape)")]
    public Transform anchor;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material errorMaterial;     // red  – guard flash
    public Material completeMaterial;  // green – step done

    private Renderer _rend;
    private bool     _flashing;
    private float    _flashTimer;
    private const float FlashDuration = 0.25f;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        ApplyMaterial(defaultMaterial);
    }

    void Update()
    {
        if (!_flashing) return;
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _flashing = false;
            ApplyMaterial(defaultMaterial);
        }
    }

    public void FlashError()
    {
        ApplyMaterial(errorMaterial);
        _flashing   = true;
        _flashTimer = FlashDuration;
    }

    public void SetComplete()  => ApplyMaterial(completeMaterial);
    public void ResetMaterial() { _flashing = false; ApplyMaterial(defaultMaterial); }

    private void ApplyMaterial(Material m)
    {
        if (_rend && m) _rend.material = m;
    }
}