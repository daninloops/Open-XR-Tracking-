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

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Interactor (EditorInteractorDriver or XRInteractorAdapter)")]
    public MonoBehaviour interactorInputMono;

    private IInteractorInput     _input;
    private SequenceData         _data;
    private int                  _step       = -1;
    private string               _lastTarget = "";
    private TwistActionCondition _activeTwist;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        _input = interactorInputMono as IInteractorInput;
        if (_input == null)
        {
            Debug.LogError("[SequenceManager] Interactor does not implement IInteractorInput.");
            return;
        }

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        TextAsset json = Resources.Load<TextAsset>(jsonResourcePath);
        if (json == null)
        {
            Debug.LogError("[SequenceManager] Cannot find sequence.json in Resources.");
            return;
        }

        _data = JsonUtility.FromJson<SequenceData>(json.text);
        if (_data == null || _data.steps == null || _data.steps.Count == 0)
        {
            Debug.LogError("[SequenceManager] No steps found in JSON.");
            return;
        }

        guardMonitor.Init(targetProvider, _input);
        conditionMonitor.OnConditionMet += OnStepComplete;
        promptDisplay.HidePrompt();
        GoToStep(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_step < 0 || _step >= _data.steps.Count) return;

        ShapeTarget target = targetProvider.GetTargetByName(_data.steps[_step].target);
        if (target == null) return;

        string condition = _data.steps[_step].condition.ToLower();

        bool isTwistAction = condition == "tighten"     || condition == "loosen"  ||
                             condition == "capopen"      || condition == "capclose" ||
                             condition == "screwaction";

        bool isOrientationAction = condition == "facecamera"  || condition == "seamvertical" ||
                                   condition == "equatortilt" || condition == "labelup"      ||
                                   condition == "flatsurface" || condition == "screwalign";

        bool isPositionAction = condition == "slotinsert" || condition == "shelfplace" ||
                                condition == "proximity"  || condition == "griphold";

        if (isTwistAction)
        {
            // Q/E rotate the INTERACTOR (wrist twist)
            // E = clockwise (negative Y in Unity), Q = counter-clockwise (positive Y)
            Transform hand = (interactorInputMono as MonoBehaviour).transform;
            if (Input.GetKey(KeyCode.E)) hand.Rotate(0, -90f * Time.deltaTime, 0, Space.World); // CW
            if (Input.GetKey(KeyCode.Q)) hand.Rotate(0,  90f * Time.deltaTime, 0, Space.World); // CCW

            // Visually rotate the object to match twist progress
            // Negate AccumulatedDegrees so CW twist rotates the mesh CW visually
            if (_activeTwist != null)
            {
                float targetY = -_activeTwist.AccumulatedDegrees;
                Vector3 e = target.transform.localEulerAngles;
                target.transform.localRotation = Quaternion.Euler(e.x, targetY, e.z);
            }
        }
        else if (isOrientationAction)
        {
            // Q/E/Z/X rotate the TARGET object
            // E = clockwise, Q = counter-clockwise (matches real-world convention)
            if (Input.GetKey(KeyCode.E)) target.transform.Rotate(0,  90f * Time.deltaTime, 0, Space.World); // CW
            if (Input.GetKey(KeyCode.Q)) target.transform.Rotate(0, -90f * Time.deltaTime, 0, Space.World); // CCW
            if (Input.GetKey(KeyCode.Z)) target.transform.Rotate(0, 0,  45f * Time.deltaTime, Space.World);
            if (Input.GetKey(KeyCode.X)) target.transform.Rotate(0, 0, -45f * Time.deltaTime, Space.World);
        }
        else if (isPositionAction)
        {
            // Interactor moves freely with WASD — no extra rotation needed here
        }
        else
        {
            // Default for any other condition type
            if (Input.GetKey(KeyCode.E))         target.transform.Rotate(0,  90f * Time.deltaTime, 0, Space.World);
            if (Input.GetKey(KeyCode.Q))         target.transform.Rotate(0, -90f * Time.deltaTime, 0, Space.World);
            if (Input.GetKey(KeyCode.UpArrow))   target.transform.Rotate( 45f * Time.deltaTime, 0, 0, Space.World);
            if (Input.GetKey(KeyCode.DownArrow)) target.transform.Rotate(-45f * Time.deltaTime, 0, 0, Space.World);
            if (Input.GetKey(KeyCode.Z))         target.transform.Rotate(0, 0,  45f * Time.deltaTime, Space.World);
            if (Input.GetKey(KeyCode.X))         target.transform.Rotate(0, 0, -45f * Time.deltaTime, Space.World);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void GoToStep(int index)
    {
        _step = index;

        if (_step >= _data.steps.Count)
        {
            arrowController.SetTarget(null);
            promptDisplay.ShowPrompt("Sequence Complete!");
            Debug.Log("[SequenceManager] Sequence Complete.");
            return;
        }

        StepData    step   = _data.steps[_step];
        ShapeTarget target = targetProvider.GetTargetByName(step.target);

        if (target == null)
        {
            Debug.LogError("[SequenceManager] Missing target: " + step.target);
            return;
        }

        if (_lastTarget != step.target)
        {
            target.ResetMaterial();
            _lastTarget = step.target;
        }

        Transform anchor = target.anchor != null ? target.anchor : target.transform;
        arrowController.SetTarget(anchor);
        guardMonitor.SetCurrentTarget(target);
        ShowPrompt(step);

        ICondition condition = BuildCondition(step, target);

        if (condition is RotateCondition rc)
            rc.CorrectionNeeded += OnCorrectionNeeded;

        conditionMonitor.StartMonitoring(condition);
        Debug.Log("[SequenceManager] Step " + (_step + 1) + "/" + _data.steps.Count + ": " + step.target + " (" + step.condition + ")");
    }

    // ─────────────────────────────────────────────────────────────────────
    ICondition BuildCondition(StepData sd, ShapeTarget target)
    {
        switch (sd.condition.ToLower())
        {
            case "proximity":
            {
                Transform anchor = target.anchor != null ? target.anchor : target.transform;
                return new ProximityCondition(anchor, _input);
            }

            case "rotate":
            {
                RotateCondition.Direction dir =
                    sd.dir != null && sd.dir.ToUpper() == "CCW"
                    ? RotateCondition.Direction.CCW
                    : RotateCondition.Direction.CW;
                return new RotateCondition(target.transform, dir, sd.amount);
            }

            case "confirm":
                return new ConfirmCondition(_input);

            case "facecamera":
            {
                Transform logo = target.transform.Find("LogoMarker");
                if (logo == null) Debug.LogError("[SequenceManager] LogoMarker missing on " + target.name);
                return new FaceCameraCondition(logo, cameraTransform);
            }

            case "seamvertical":
            {
                Transform seam = target.transform.Find("SeamMarker");
                if (seam == null) Debug.LogError("[SequenceManager] SeamMarker missing on " + target.name);
                return new SeamVerticalCondition(seam, cameraTransform);
            }

            case "equatortilt":
            {
                Transform ring = target.transform.Find("EquatorSeam");
                if (ring == null) Debug.LogError("[SequenceManager] EquatorSeam missing on " + target.name);
                return new EquatorTiltCondition(ring, target.transform, cameraTransform, sd.amount);
            }

            case "screwaction":
            {
                Transform screwHead = target.transform.Find("ScrewHead")
                                   ?? target.transform.Find("Anchor")
                                   ?? target.transform;
                Transform hand = (interactorInputMono as MonoBehaviour).transform;
                ScrewActionCondition screw = new ScrewActionCondition(screwHead, hand, 3f,
                    sd.amount > 0 ? sd.amount : 360f);
                screw.OnWrongAction        += msg => { arrowController.SetCorrectionMode(true);  target.FlashError(); promptDisplay.ShowPrompt(msg); };
                screw.OnGripReacquired     += ()  => { arrowController.SetCorrectionMode(false); target.ResetMaterial(); ShowPrompt(_data.steps[_step]); };
                screw.OnDirectionCorrected += ()  => { arrowController.SetCorrectionMode(false); target.ResetMaterial(); ShowPrompt(_data.steps[_step]); };
                return screw;
            }

            case "griphold":
            {
                Transform gripPoint = target.anchor != null ? target.anchor : target.transform;
                GripHoldCondition grip = new GripHoldCondition(gripPoint, _input, 3f, 1f);
                grip.OnWrongAction   += msg => { arrowController.SetCorrectionMode(true);  target.FlashError(); promptDisplay.ShowPrompt(msg); };
                grip.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); target.ResetMaterial(); ShowPrompt(_data.steps[_step]); };
                return grip;
            }

            case "tighten":
            {
                Transform anchor = target.transform.Find("Anchor") ?? target.transform;
                Transform hand   = (interactorInputMono as MonoBehaviour).transform;
                var cond = new TwistActionCondition(anchor, hand, target.transform,
                    TwistActionCondition.TwistDirection.CW,
                    sd.amount > 0 ? sd.amount : 360f, 3f);
                cond.OnWrongAction        += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnGripReacquired     += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                cond.OnDirectionCorrected += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                _activeTwist = cond;
                return cond;
            }

            case "loosen":
            {
                Transform anchor = target.transform.Find("Anchor") ?? target.transform;
                Transform hand   = (interactorInputMono as MonoBehaviour).transform;
                var cond = new TwistActionCondition(anchor, hand, target.transform,
                    TwistActionCondition.TwistDirection.CCW,
                    sd.amount > 0 ? sd.amount : 360f, 3f);
                cond.OnWrongAction        += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnGripReacquired     += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                cond.OnDirectionCorrected += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                _activeTwist = cond;
                return cond;
            }

            case "capopen":
            {
                Transform anchor = target.transform.Find("CapAnchor") ?? target.transform;
                Transform hand   = (interactorInputMono as MonoBehaviour).transform;
                var cond = new TwistActionCondition(anchor, hand, target.transform,
                    TwistActionCondition.TwistDirection.CCW,
                    sd.amount > 0 ? sd.amount : 360f, 3f);
                cond.OnWrongAction        += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnGripReacquired     += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                cond.OnDirectionCorrected += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                _activeTwist = cond;
                return cond;
            }

            case "capclose":
            {
                Transform anchor = target.transform.Find("CapAnchor") ?? target.transform;
                Transform hand   = (interactorInputMono as MonoBehaviour).transform;
                var cond = new TwistActionCondition(anchor, hand, target.transform,
                    TwistActionCondition.TwistDirection.CW,
                    sd.amount > 0 ? sd.amount : 360f, 3f);
                cond.OnWrongAction        += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnGripReacquired     += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                cond.OnDirectionCorrected += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                _activeTwist = cond;
                return cond;
            }

            case "screwalign":
            {
                Transform slotMarker = target.transform.Find("SlotMarker");
                if (slotMarker == null)
                    Debug.LogError("[SequenceManager] No SlotMarker child on " + target.name);
                var cond = new ScrewAlignCondition(slotMarker);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            case "handshake":
            {
                Transform hand = (interactorInputMono as MonoBehaviour).transform;
                var cond = new HandshakeCondition(target.transform, hand);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            case "labelup":
            {
                var cond = new LabelUpCondition(target.transform);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            case "flatsurface":
            {
                var cond = new FlatSurfaceCondition(target.transform);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            case "slotinsert":
            {
                Transform slotTarget = target.transform.Find("SlotTarget");
                if (slotTarget == null)
                    Debug.LogError("[SequenceManager] No SlotTarget child on " + target.name);
                Transform hand = (interactorInputMono as MonoBehaviour).transform;
                var cond = new SlotInsertCondition(slotTarget, hand);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            case "shelfplace":
            {
                Transform shelfZone = target.transform.Find("ShelfZone");
                if (shelfZone == null)
                    Debug.LogError("[SequenceManager] No ShelfZone child on " + target.name);
                Transform hand = (interactorInputMono as MonoBehaviour).transform;
                var cond = new ShelfPlacementCondition(shelfZone, hand);
                cond.OnWrongAction  += msg => { arrowController.SetCorrectionMode(true);  promptDisplay.ShowPrompt(msg); };
                cond.OnCorrectAction += ()  => { arrowController.SetCorrectionMode(false); ShowPrompt(_data.steps[_step]); };
                return cond;
            }

            default:
                Debug.LogWarning("[SequenceManager] Unknown condition: " + sd.condition);
                return new ConfirmCondition(_input);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void ShowPrompt(StepData sd)
    {
        if (promptDisplay == null) return;
        string msg = "";
        switch (sd.condition.ToLower())
        {
            case "proximity":    msg = "Move close to " + sd.target; break;
            case "rotate":       msg = "Rotate " + sd.target + " " + sd.amount + " deg " + sd.dir; break;
            case "confirm":      msg = "Press SPACE to confirm."; break;
            case "facecamera":   msg = "Face " + sd.target + " towards yourself."; break;
            case "seamvertical": msg = "Rotate until the seam is vertical."; break;
            case "equatortilt":  msg = "Tilt " + sd.target + " by " + sd.amount + " degrees."; break;
            case "screwaction":  msg = "Grip the screw head and twist CLOCKWISE  [E]"; break;
            case "griphold":     msg = "Move close to " + sd.target + " and hold LEFT SHIFT to grip."; break;
            case "tighten":      msg = "Grip " + sd.target + " and twist CLOCKWISE  [E]"; break;
            case "loosen":       msg = "Grip " + sd.target + " and twist COUNTER-CLOCKWISE  [Q]"; break;
            case "capopen":      msg = "Grip the cap and twist COUNTER-CLOCKWISE to open  [Q]"; break;
            case "capclose":     msg = "Grip the cap and twist CLOCKWISE to close  [E]"; break;
            case "screwalign":   msg = "Rotate " + sd.target + " to align the slot horizontally  [E=CW / Q=CCW]"; break;
            case "handshake":    msg = "Approach " + sd.target + " from the front and extend your hand."; break;
            case "labelup":      msg = "Rotate " + sd.target + " so the label faces UP  [E=CW / Q=CCW / Z/X]"; break;
            case "flatsurface":  msg = "Tilt " + sd.target + " flat with base facing down  [Q/E/Z/X]"; break;
            case "slotinsert":   msg = "Align hand with slot then slide in  [Q/E to rotate, WASD to move]"; break;
            case "shelfplace":   msg = "Move to the shelf zone  [WASD to move]"; break;
            default:             msg = sd.target; break;
        }
        promptDisplay.ShowPrompt(msg);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnCorrectionNeeded(bool active)
    {
        arrowController?.SetCorrectionMode(active);
        if (_step < 0 || _step >= _data.steps.Count) return;

        ShapeTarget target = targetProvider.GetTargetByName(_data.steps[_step].target);
        if (target == null) return;

        if (active)
        {
            target.FlashError();
            promptDisplay.ShowPrompt("Wrong direction! Rotate the other way.");
        }
        else
        {
            target.ResetMaterial();
            ShowPrompt(_data.steps[_step]);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnStepComplete() => StartCoroutine(HandleStepCompletion());

    IEnumerator HandleStepCompletion()
    {
        conditionMonitor.StopMonitoring();

        StepData    currentStep   = _data.steps[_step];
        ShapeTarget currentTarget = targetProvider.GetTargetByName(currentStep.target);

        if (currentTarget != null) currentTarget.FlashSuccess();

        yield return new WaitForSeconds(1f);

        bool lastStepForTarget = true;
        for (int i = _step + 1; i < _data.steps.Count; i++)
        {
            if (_data.steps[i].target == currentStep.target)
            {
                lastStepForTarget = false;
                break;
            }
        }

        if (currentTarget != null)
        {
            if (lastStepForTarget) currentTarget.SetComplete();
            else                   currentTarget.ResetMaterial();
        }

        _activeTwist = null;
        arrowController?.SetCorrectionMode(false);
        Debug.Log("[SequenceManager] Step " + (_step + 1) + " Complete.");
        GoToStep(_step + 1);
    }
}