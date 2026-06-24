using UnityEngine;

public class ShapeTarget : MonoBehaviour
{
    [Header("Anchor")]
    public Transform anchor;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material errorMaterial;
    public Material completeMaterial;

    private Renderer _rend;

    private bool _lockedGreen = false;

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
        ApplyMaterial(defaultMaterial);
    }

    public void FlashError()
    {
        if (_lockedGreen) return;

        StopAllCoroutines();
        StartCoroutine(FlashMaterial(errorMaterial, 0.75f));
    }

    public void FlashSuccess()
    {
        StopAllCoroutines();
        StartCoroutine(FlashMaterial(completeMaterial, 1f));
    }

    public void SetComplete()
    {
        _lockedGreen = true;
        StopAllCoroutines();
        ApplyMaterial(completeMaterial);
    }

    public void ResetMaterial()
    {
        if (_lockedGreen) return;

        StopAllCoroutines();
        ApplyMaterial(defaultMaterial);
    }

    private System.Collections.IEnumerator FlashMaterial(Material mat, float duration)
    {
        ApplyMaterial(mat);

        yield return new WaitForSeconds(duration);

        if (!_lockedGreen)
            ApplyMaterial(defaultMaterial);
    }

    private void ApplyMaterial(Material mat)
    {
        if (_rend != null && mat != null)
            _rend.material = mat;
    }
}