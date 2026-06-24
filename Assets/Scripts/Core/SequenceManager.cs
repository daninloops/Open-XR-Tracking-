using UnityEngine;
using System.Collections;
public class SequenceManager : MonoBehaviour
{
    [Header("JSON (file in Assets/Resources/, no extension)")]
    public string jsonResourcePath = "sequence";

    [Header("Scene References")]
    public ArrowController     arrowController;
    public PromptDisplay       promptDisplay;
    public ConditionMonitor    conditionMonitor;
    public GuardMonitor        guardMonitor;
    public SceneTargetProvider targetProvider;

    [Header("Camera (used for orientation conditions)")]
    public Transform cameraTransform;

    [Header("Drag EditorInteractorDriver here (swap for XRInteractorAdapter on Quest 3)")]
    public MonoBehaviour interactorInputMono;

    private IInteractorInput _input;
    private SequenceData     _data;
    private int              _step = -1;
    private string           _lastTarget = ""; // tracks when we switch to a new shape

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        _input = interactorInputMono as IInteractorInput;
        if (_input == null)
        {
            Debug.LogError("[SequenceManager] interactorInputMono must implement IInteractorInput!");
            return;
        }

        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        var json = Resources.Load<TextAsset>(jsonResourcePath);
        if (json == null)
        {
            Debug.LogError($"[SequenceManager] Missing Resources/{jsonResourcePath}.json");
            return;
        }
        _data = JsonUtility.FromJson<SequenceData>(json.text);

        // Randomize every target's starting rotation so no condition is pre-satisfied
        foreach (var step in _data.steps)
        {
            var t = targetProvider.GetTargetByName(step.target);
            if (t != null) t.transform.rotation = Random.rotation;
        }

        guardMonitor.Init(targetProvider, _input);
        conditionMonitor.OnConditionMet += OnStepComplete;

        promptDisplay?.HidePrompt();
        GoToStep(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_step < 0 || _data == null || _step >= _data.steps.Count) return;

        ShapeTarget target = targetProvider.GetTargetByName(_data.steps[_step].target);
        if (target == null) return;

        // Y axis
        if (Input.GetKey(KeyCode.Q)) target.transform.Rotate(0f,  90f * Time.deltaTime, 0f, Space.World);
        if (Input.GetKey(KeyCode.E)) target.transform.Rotate(0f, -90f * Time.deltaTime, 0f, Space.World);
        // X axis
        if (Input.GetKey(KeyCode.UpArrow))   target.transform.Rotate( 45f * Time.deltaTime, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.DownArrow))  target.transform.Rotate(-45f * Time.deltaTime, 0f, 0f, Space.World);
        // Z axis
        if (Input.GetKey(KeyCode.Z)) target.transform.Rotate(0f, 0f,  45f * Time.deltaTime, Space.World);
        if (Input.GetKey(KeyCode.X)) target.transform.Rotate(0f, 0f, -45f * Time.deltaTime, Space.World);
    }

    // ─────────────────────────────────────────────────────────────────────
    void GoToStep(int index)
    {
        
        _step = index;

        // Sequence finished
        if (_step >= _data.steps.Count)
        {
            arrowController?.SetTarget(null);
            promptDisplay?.ShowPrompt("✅  Sequence Complete!");
            Debug.Log("[SequenceManager] All steps done.");
            return;
        }

        StepData    sd     = _data.steps[_step];
        ShapeTarget target = targetProvider.GetTargetByName(sd.target);

        if (target == null)
        {
            Debug.LogError($"[SequenceManager] GameObject not found: '{sd.target}'");
            return;
        }

        // Only reset material when switching to a NEW shape, not between steps on same shape
        if (sd.target != _lastTarget)
        {
            target.ResetMaterial();
            _lastTarget = sd.target;
        }

        Transform anchorOrSelf = target.anchor != null ? target.anchor : target.transform;
        arrowController?.SetTarget(anchorOrSelf);

        guardMonitor.SetCurrentTarget(target);
        ShowPrompt(sd);

        ICondition cond = BuildCondition(sd, target);
        if (cond is RotateCondition rc)
            rc.CorrectionNeeded += OnCorrectionNeeded;

        conditionMonitor.StartMonitoring(cond);
        Debug.Log($"[SequenceManager] Step {_step + 1}/{_data.steps.Count} → {sd.target} [{sd.condition}]");
    }

    // ─────────────────────────────────────────────────────────────────────
    ICondition BuildCondition(StepData sd, ShapeTarget target)
    {
        switch (sd.condition.ToLower())
        {
            case "proximity":
                Transform proximityAnchor = target.anchor != null ? target.anchor : target.transform;
                return new ProximityCondition(proximityAnchor, _input);

            case "rotate":
                var dir = sd.dir?.ToUpper() == "CCW"
                    ? RotateCondition.Direction.CCW
                    : RotateCondition.Direction.CW;
                return new RotateCondition(target.transform, dir, sd.amount);

            case "confirm":
                return new ConfirmCondition(_input);

            case "facecamera":
                Transform logo = target.transform.Find("LogoMarker");
                if (logo == null) Debug.LogError($"[SequenceManager] No 'LogoMarker' child on {sd.target}");
                return new FaceCameraCondition(logo, cameraTransform);

            case "seamvertical":
                Transform seam = target.transform.Find("SeamMarker");
                if (seam == null) Debug.LogError($"[SequenceManager] No 'SeamMarker' child on {sd.target}");
                return new SeamVerticalCondition(seam, cameraTransform);

            case "equatortilt":
                Transform ring = target.transform.Find("EquatorSeam");
                if (ring == null) Debug.LogError($"[SequenceManager] No 'EquatorSeam' child on {sd.target}");
                return new EquatorTiltCondition(ring, target.transform, cameraTransform, sd.amount);

            default:
                Debug.LogWarning($"[SequenceManager] Unknown condition '{sd.condition}' – defaulting to confirm.");
                return new ConfirmCondition(_input);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void ShowPrompt(StepData sd)
    {
        if (!promptDisplay) return;
        string msg = sd.condition.ToLower() switch
        {
            "proximity"    => $"Move close to {sd.target}",
            "rotate"       => $"Rotate {sd.target} {sd.amount}° {sd.dir}  [Q=CCW / E=CW]",
            "confirm"      => $"Press SPACE to confirm {sd.target}",
            "facecamera"   => $"Orient {sd.target} so the green marker faces you  [Q/E/↑↓/Z/X to rotate]",
            "seamvertical" => $"Turn {sd.target} until the white seam runs vertically  [Q/E/↑↓/Z/X]",
            "equatortilt"  => $"Face the ring toward you, tilt {sd.target} {sd.amount}° up  [↑↓ to tilt]",
            _              => sd.target
        };
        promptDisplay.ShowPrompt(msg);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnCorrectionNeeded(bool active)
    {
        arrowController?.SetCorrectionMode(active);
        if (active)
            promptDisplay?.ShowPrompt("⚠  Wrong direction!  Go the other way!");
        else if (_step < _data.steps.Count)
            ShowPrompt(_data.steps[_step]);
    }

    void OnStepComplete()
    {
       StartCoroutine(HandleStepCompletion());
    }
    IEnumerator HandleStepCompletion()
{
    conditionMonitor.StopMonitoring();

    StepData currentStep = _data.steps[_step];

    ShapeTarget currentTarget =
        targetProvider.GetTargetByName(currentStep.target);

    if (currentTarget != null)
        currentTarget.FlashSuccess();

    yield return new WaitForSeconds(1f);

    bool isLastStepForRock = true;

    for (int i = _step + 1; i < _data.steps.Count; i++)
    {
        if (_data.steps[i].target == currentStep.target)
        {
            isLastStepForRock = false;
            break;
        }
    }

    if (isLastStepForRock && currentTarget != null)
    {
        currentTarget.SetComplete();
    }

    Debug.Log($"[SequenceManager] Step {_step + 1} complete.");

    GoToStep(_step + 1);
}
}