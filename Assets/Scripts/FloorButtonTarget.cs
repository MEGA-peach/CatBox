using UnityEngine;

public abstract class FloorButtonTarget : MonoBehaviour
{
    public virtual void OnButtonPressed(GridFloorButton button) { }
    public virtual void OnButtonReleased(GridFloorButton button) { }
}