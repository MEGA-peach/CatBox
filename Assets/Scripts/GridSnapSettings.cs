// GridSnapSettings.cs
using UnityEngine;

[CreateAssetMenu(menuName = "CatBox/Grid Snap Settings", fileName = "GridSnapSettings")]
public class GridSnapSettings : ScriptableObject
{
    public enum CellAnchor { Center, BottomCenter, Custom }

    [Header("Snap Target")]
    public CellAnchor anchor = CellAnchor.Center;
    public Vector2 customWorldOffset = Vector2.zero;

    [Header("Axis Control")]
    public bool snapX = true;
    public bool snapY = true;
    public bool preserveZ = true;

    [Header("Rounding")]
    public bool snapToNearestCell = true;
}