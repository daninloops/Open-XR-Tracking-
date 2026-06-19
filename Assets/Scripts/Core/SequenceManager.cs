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
    public ArrowController arrowController;
    public PromptDisplay   promptDisplay;
    public ConditionMonitor   conditionMonitor;
    public GuardMonitor       guardMonitor;
    public SceneTargetProvider targetProvider;

    [Header("Drag EditorInteractorDriver here (swap for XRInteractorAdapter on Quest 3)")]
    public MonoBehaviour interactorInputMono;

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

    // ─────────────────────────────────────────────────────
    void GoToStep(int index)
    {
        // Mark previous step complete
        if (_step >= 0 && _step < _data.steps.Count)
            targetProvider.GetTargetByName(_data.steps[_step].target)?.SetComplete();

        _step = index;

        if (_step >= _data.steps.Count)
        {
            arrowController.SetTarget(null);
            promptDisplay?.ShowPrompt("✅  Sequence Complete!");
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

        ICondition cond = BuildCondition(sd, target);

        // Hook live-correction events for rotate
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

            default:
                Debug.LogWarning($"[SequenceManager] Unknown condition '{sd.condition}' – defaulting to confirm.");
                return new ConfirmCondition(_input);
        }
    }

    // ─────────────────────────────────────────────────────
    void ShowPrompt(StepData sd)
    {
        if (!promptDisplay) return;
        string msg = sd.condition.ToLower() switch
        {
            "proximity" => $"Move close to  {sd.target}",
            "rotate"    => $"Rotate {sd.target}  {sd.amount}°  {sd.dir}",
            "confirm"   => $"Press SPACE to confirm  {sd.target}",
            _           => sd.target
        };
        promptDisplay.ShowPrompt(msg);
    }

    void OnCorrectionNeeded(bool active)
    {
        arrowController.SetCorrectionMode(active);
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
    void update()
    {
        // rotate the current target with q/e not the interactor
        if(_step<0 || _step>=_data.steps.Count) return;
        ShapeTarget target = targetProvider.GetTargetByName(_data.steps[_step].target);
    if (target == null) return;

    if (Input.GetKey(KeyCode.Q))
        target.transform.Rotate(0f, -90f * Time.deltaTime, 0f, Space.World); // CCW
    if (Input.GetKey(KeyCode.E))
        target.transform.Rotate(0f,  90f * Time.deltaTime, 0f, Space.World); // CW
    }
}