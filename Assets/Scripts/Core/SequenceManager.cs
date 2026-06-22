using UnityEngine;

/// <summary>
/// Main controller. Reads sequence.json from Resources/, builds conditions,
/// drives the arrow step-by-step, and coordinates all subsystems.
/// 
/// Add to an empty GameObject called "SequenceManager".
/// Also add ConditionMonitor, GuardMonitor, SceneTargetProvider to the same GameObject.
/// </summary>
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
    [Tooltip("Drag the Main Camera inside XR Origin here")]
    public Transform cameraTransform;

    [Header("Optional audio feedback")]
    public AudioManager audioManager;

    [Header("Drag EditorInteractorDriver here (swap for XRInteractorAdapter on Quest 3)")]
    public MonoBehaviour interactorInputMono;

    [Header("Editor testing")]
    public KeyCode resetKey = KeyCode.R;

    // ── runtime ──────────────────────────────────────────
    private IInteractorInput _input;
    private SequenceData     _data;
    private int              _step = -1;

    // ─────────────────────────────────────────────────────
    void Start()
    {
        _input = interactorInputMono as IInteractorInput;
        if (_input == null)
        {
            Debug.LogError("[SequenceManager] interactorInputMono must implement IInteractorInput!");
            return;
        }

        // Fallback to main camera if not assigned
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        LoadAndBegin();
    }

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            Debug.Log("[SequenceManager] Manual reset triggered.");
            ResetSequence();
        }
    }

    void LoadAndBegin()
    {
        var json = Resources.Load<TextAsset>(jsonResourcePath);
        if (json == null)
        {
            Debug.LogError($"[SequenceManager] Missing Resources/{jsonResourcePath}.json");
            return;
        }
        _data = JsonUtility.FromJson<SequenceData>(json.text);

        guardMonitor.Init(targetProvider, _input);
        conditionMonitor.OnConditionMet += OnStepComplete;

        promptDisplay?.HidePrompt();
        _step = -1;
        GoToStep(0);
    }

    public void ResetSequence()
    {
        conditionMonitor.StopMonitoring();
        if (_data != null)
            foreach (var sd in _data.steps)
                targetProvider.GetTargetByName(sd.target)?.ResetMaterial();

        arrowController.SetCorrectionMode(false);
        _step = -1;
        GoToStep(0);
    }

    // ─────────────────────────────────────────────────────
    void GoToStep(int index)
    {
        if (_step >= 0 && _step < _data.steps.Count)
            targetProvider.GetTargetByName(_data.steps[_step].target)?.SetComplete();

        _step = index;

        if (_step >= _data.steps.Count)
        {
            arrowController.SetTarget(null);
            promptDisplay?.ShowPrompt("✅  Sequence Complete!  (Press R to restart)");
            audioManager?.PlaySequenceComplete();
            Debug.Log("[SequenceManager] All steps done.");
            return;
        }

        StepData    sd     = _data.steps[_step];
        ShapeTarget target = targetProvider.GetTargetByName(sd.target);

        if (target == null)
        {
            Debug.LogError($"[SequenceManager] GameObject not found: '{sd.target}' – check JSON spelling.");
            return;
        }

        target.ResetMaterial();
        arrowController.SetTarget(target.anchor);
        guardMonitor.SetCurrentTarget(target);
        ShowPrompt(sd);
        promptDisplay?.ShowStepCounter(_step, _data.steps.Count);

        ICondition cond = BuildCondition(sd, target);

        if (cond is RotateCondition rc)
            rc.CorrectionNeeded += OnCorrectionNeeded;

        conditionMonitor.StartMonitoring(cond);
        Debug.Log($"[SequenceManager] Step {_step + 1}/{_data.steps.Count}  →  {sd.target}  [{sd.condition}]");
    }

    // ─────────────────────────────────────────────────────
    ICondition BuildCondition(StepData sd, ShapeTarget target)
    {
        switch (sd.condition.ToLower())
        {
            case "proximity":
                return new ProximityCondition(target.anchor, _input);

            case "rotate":
                var dir = sd.dir?.ToUpper() == "CCW"
                    ? RotateCondition.Direction.CCW
                    : RotateCondition.Direction.CW;
                return new RotateCondition(target.transform, dir, sd.amount);

            case "confirm":
                return new ConfirmCondition(_input);

            // ── NEW orientation conditions ──────────────────

            case "faceuser":
                // Requires a child OrientationMarker named by sd.markerName
                var fuMarker = FindMarker(target, sd.markerName);
                return new FaceUserCondition(fuMarker, cameraTransform,
                                             sd.angleTolerance > 0 ? sd.angleTolerance : 20f,
                                             sd.holdDuration   > 0 ? sd.holdDuration   : 1f);

            case "verticalseam":
                var vsMarker = FindMarker(target, sd.markerName);
                return new VerticalAlignCondition(vsMarker, cameraTransform,
                                                  sd.angleTolerance > 0 ? sd.angleTolerance : 15f,
                                                  sd.holdDuration   > 0 ? sd.holdDuration   : 1f);

            case "faceandtilt":
                var ftMarker = FindMarker(target, sd.markerName);
                return new FaceAndTiltCondition(ftMarker, target.transform, cameraTransform,
                                                sd.amount        > 0 ? sd.amount        : 45f,
                                                sd.angleTolerance > 0 ? sd.angleTolerance : 20f,
                                                15f,
                                                sd.holdDuration   > 0 ? sd.holdDuration   : 1f);

            default:
                Debug.LogWarning($"[SequenceManager] Unknown condition '{sd.condition}' – defaulting to confirm.");
                return new ConfirmCondition(_input);
        }
    }

    /// <summary>Finds an OrientationMarker child by name on the target's GameObject.</summary>
    OrientationMarker FindMarker(ShapeTarget target, string markerName)
    {
        if (string.IsNullOrEmpty(markerName))
        {
            // Fall back: return first OrientationMarker found on target
            var fallback = target.GetComponentInChildren<OrientationMarker>();
            if (fallback == null)
                Debug.LogError($"[SequenceManager] No OrientationMarker found on {target.name}. " +
                               "Add a child empty with OrientationMarker component.");
            return fallback;
        }

        var child = target.transform.Find(markerName);
        if (child == null)
        {
            Debug.LogError($"[SequenceManager] Marker '{markerName}' not found on {target.name}.");
            return null;
        }
        return child.GetComponent<OrientationMarker>();
    }

    // ─────────────────────────────────────────────────────
    void ShowPrompt(StepData sd)
    {
        if (!promptDisplay) return;
        string msg = sd.condition.ToLower() switch
        {
            "proximity"    => $"Move close to {sd.target}",
            "rotate"       => $"Rotate {sd.target}  {sd.amount}°  {sd.dir}",
            "confirm"      => $"Press SPACE to confirm {sd.target}",
            "faceuser"     => $"Hold {sd.target} so the {sd.markerName} faces you",
            "verticalseam" => $"Rotate {sd.target} until the seam is vertical",
            "faceandtilt"  => $"Face {sd.target}'s equator toward you, then tilt 45° up",
            _              => sd.target
        };
        promptDisplay.ShowPrompt(msg);
    }

    void OnCorrectionNeeded(bool active)
    {
        arrowController.SetCorrectionMode(active);
        if (active)
        {
            promptDisplay?.ShowPrompt("⚠  Wrong direction!  Go the other way!");
            audioManager?.PlayGuardError();
        }
        else if (_step < _data.steps.Count)
            ShowPrompt(_data.steps[_step]);
    }

    void OnStepComplete()
    {
        conditionMonitor.StopMonitoring();
        Debug.Log($"[SequenceManager] Step {_step + 1} complete.");
        audioManager?.PlayStepComplete();
        guardMonitor.TriggerGrace();
        GoToStep(_step + 1);
    }
}