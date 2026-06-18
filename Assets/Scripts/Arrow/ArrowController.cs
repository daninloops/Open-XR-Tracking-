using UnityEngine;

/// <summary>
/// Attach to your Arrow GameObject.
/// Call SetTarget(anchor) to point it at a new anchor every step.
/// Call SetCorrectionMode(true) to turn it red during wrong-direction rotation.
/// The arrow hovers above the anchor and bobs gently.
/// </summary>
public class ArrowController : MonoBehaviour
{
    [Header("Float & Bob")]
    public float floatHeight = 0.4f;
    public float bobSpeed    = 1.5f;
    public float bobAmount   = 0.05f;

    [Header("Materials")]
    public Material normalMaterial;
    public Material correctionMaterial;

    private Transform  _anchor;
    private Renderer[] _renderers;
    private float      _bobTimer;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_anchor == null) return;

        _bobTimer += Time.deltaTime * bobSpeed;
        float bob = Mathf.Sin(_bobTimer) * bobAmount;

        transform.position = _anchor.position + Vector3.up * (floatHeight + bob);

        // Point down toward the anchor
        Vector3 dir = _anchor.position - transform.position;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void SetTarget(Transform anchor)
    {
        _anchor = anchor;
        gameObject.SetActive(anchor != null);
    }

    public void SetCorrectionMode(bool active)
    {
        Material m = active ? correctionMaterial : normalMaterial;
        if (m == null) return;
        foreach (var r in _renderers) r.material = m;
    }
}