//Detection.cs is a plain text class- which is not attached to any game object
//it represents one detection result from yolo (the simulated version)
//later real yolo output will be converted into this format before entering the pipeline- so nothing else is to be changed 
[System.Serializable]
public class Detection
{
    //The object category e.g "cup", "chair", "person"

    public string label;
    // The 2D bounding box in the pixel coordinates (xMin,yMin,xMax,yMax)
    //UnityEngine.Rect stores exactly this:x,y,width,height 
    //but we will store as explicit mins/maxs for clarity 
    public float xMin;//x
    public float yMin;//y
    public float xMax;//width
    public float yMax;//height 

//How confident YOLO is (0.0 to 1.0) Simulated as 1 for now 
public float confidence ;
// Constructor so we can create a Detection in one line 
public Detection (string label, float xMin, float yMin, float xMax, float yMax, float confidence)
    {
        this.label=label;
        this.xMin=xMin;
        this.yMin=yMin;
        this.yMax=yMax;
        this.confidence= confidence;
    }
    //convenience property :pixel coordinates of the box centre 
    // this is what gets projected to a 3d ray in part c 
    public float CenterX=> (xMin +xMax)/2f;
    public float CenterY=> (yMin+yMax)/2f;
}