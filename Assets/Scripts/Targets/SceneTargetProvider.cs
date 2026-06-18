using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds every ShapeTarget in the scene at Awake.
/// Swappable: replace with a provider that reads from a server, etc.
/// Add this component to the SequenceManager GameObject.
/// </summary>
public class SceneTargetProvider : MonoBehaviour, ITargetProvider
{
    private readonly List<ShapeTarget>             _list  = new();
    private readonly Dictionary<string,ShapeTarget> _dict = new();

    void Awake() => Refresh();

    public void Refresh()
    {
        _list.Clear();
        _dict.Clear();
        foreach (var t in FindObjectsByType<ShapeTarget>(FindObjectsSortMode.None))
        {
            _list.Add(t);
            _dict[t.gameObject.name] = t;
        }
    }

    public IReadOnlyList<ShapeTarget> GetAllTargets()          => _list;
    public ShapeTarget GetTargetByName(string name)
    {
        _dict.TryGetValue(name, out var t);
        return t;
    }
}