using System.Collections.Generic;

public interface ITargetProvider
{
    IReadOnlyList<ShapeTarget> GetAllTargets();
    ShapeTarget                GetTargetByName(string name);
}
