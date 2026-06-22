using System;
using System.Collections.Generic;

[Serializable]
public class SequenceData
{
    public List<StepData> steps;
}

[Serializable]
public class StepData
{
    public string target;         // GameObject name: "CubeA", "Item1" etc.
    public string condition;      // "proximity"|"rotate"|"confirm"|"faceuser"|"verticalseam"|"faceandtilt"
    public string dir;            // "CW"|"CCW"  (rotate only)
    public float  amount;         // degrees (rotate / faceandtilt tilt angle)
    public string markerName;     // Name of child OrientationMarker (orientation conditions)
    public float  angleTolerance; // degrees tolerance (orientation conditions, 0 = use default)
    public float  holdDuration;   // seconds to hold pose (orientation conditions, 0 = use default)
}