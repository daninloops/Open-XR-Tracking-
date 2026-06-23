using UnityEngine;

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

        guardMonitor.Init(targetProvider, _input);
        conditionMonitor.OnConditionMet += OnStepComplete;

        promptDisplay?.HidePrompt();
        GoToStep(0);
    }

    void Update()
    {
        if (_step < 0 || _data == null || _step >= _data.steps.Count) return;

        ShapeTarget target = targetProvider.GetTargetByName(_data.steps[_step].target);
        if (target == null) return;

        if (Input.GetKey(KeyCode.Q))
            target.transform.Rotate(0f,  90f * Time.deltaTime, 0f, Space.World);
        if (Input.GetKey(KeyCode.E))
            target.transform.Rotate(0f, -90f * Time.deltaTime, 0f, Space.World);

        if (Input.GetKey(KeyCode.UpArrow))
            target.transform.Rotate(45f * Time.deltaTime, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.DownArrow))
            target.transform.Rotate(-45f * Time.deltaTime, 0f, 0f, Space.World);
    }

    void GoToStep(int index)
    {
        // Mark the step we just FINISHED as complete (green)
        // _step still holds the old index here, before we update it
        if (_step >= 0 && _step < _data.steps.Count)
            targetProvider.GetTargetByName(_data.steps[_step].target)?.SetComplete();

        // Now advance to the new step
        _step = index;

        // Sequence finished
        if (_step >= _data.steps.Count)
        {
            arrowController?.SetTarget(null);
            promptDisplay?.ShowPrompt("✅  Sequence Complete!");
            Debug.Log("[SequenceManager] All steps done.");
            return;
        }

        // Set up the new active step
        StepData    sd     = _data.steps[_step];
        ShapeTarget target = targetProvider.GetTargetByName(sd.target);

        if (target == null)
        {
            Debug.LogError($"[SequenceManager] GameObject not found: '{sd.target}'");
            return;
        }

        target.ResetMaterial();

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

    void ShowPrompt(StepData sd)
    {
        if (!promptDisplay) return;
        string msg = sd.condition.ToLower() switch
        {
            "proximity"    => $"Move close to {sd.target}",
            "rotate"       => $"Rotate {sd.target} {sd.amount}° {sd.dir}  [Q = CCW / E = CW]",
            "confirm"      => $"Press SPACE to confirm {sd.target}",
            "facecamera"   => $"Hold {sd.target} with the green logo facing you at eye level",
            "seamvertical" => $"Turn {sd.target} until the white seam runs vertically",
            "equatortilt"  => $"Face the equator ring toward you, then tilt {sd.target} {sd.amount}° upward  [↑↓ to tilt]",
            _              => sd.target
        };
        promptDisplay.ShowPrompt(msg);
    }

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
        conditionMonitor.StopMonitoring();
        Debug.Log($"[SequenceManager] Step {_step + 1} complete.");
        GoToStep(_step + 1);
    }
}
