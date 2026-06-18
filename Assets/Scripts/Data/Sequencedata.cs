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
    public string target;       // GameObject name: "CubeA", "CubeB", "SphereC"
    public string condition;    // "proximity" | "rotate" | "confirm"
    public string dir;          // "CW" | "CCW"  (rotate only, else "")
    public float  amount;       // degrees       (rotate only, else 0)
}