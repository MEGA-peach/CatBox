using UnityEngine;

[DisallowMultipleComponent]
public class CatAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string faceXParam = "FaceX";
    [SerializeField] private string faceYParam = "FaceY";
    [SerializeField] private string hitTriggerParam = "Hit";
    [SerializeField] private string draggedBoolParam = "IsDragged";
    [SerializeField] private string inBoxBoolParam = "IsInBox";

    [Header("State Names")]
    [SerializeField] private string inBoxStateName = "InBox";

    private readonly Vector2 defaultFacing = Vector2.down;
    private Vector2 currentFacing = Vector2.down;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        FaceDefault();
        SetDragged(false);
        SetInBox(false);
    }

    public void FaceDirection(Vector3Int gridDirection)
    {
        if (gridDirection == Vector3Int.zero)
        {
            FaceDefault();
            return;
        }

        if (Mathf.Abs(gridDirection.x) > 0)
        {
            currentFacing = new Vector2(gridDirection.x > 0 ? 1f : -1f, 0f);
        }
        else if (Mathf.Abs(gridDirection.y) > 0)
        {
            currentFacing = new Vector2(0f, gridDirection.y > 0 ? 1f : -1f);
        }

        ApplyFacing();
    }

    public void FaceTowardWorldPosition(Vector3 targetWorldPosition)
    {
        Vector3 delta = targetWorldPosition - transform.position;

        if (delta.sqrMagnitude <= 0.000001f)
        {
            FaceDefault();
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            FaceDirection(new Vector3Int(delta.x > 0f ? 1 : -1, 0, 0));
        }
        else
        {
            FaceDirection(new Vector3Int(0, delta.y > 0f ? 1 : -1, 0));
        }
    }

    public void FaceDefault()
    {
        currentFacing = defaultFacing;
        ApplyFacing();
    }

    public void SetDragged(bool isDragged)
    {
        if (animator == null)
            return;

        animator.SetBool(draggedBoolParam, isDragged);
    }

    public void SetInBox(bool isInBox)
    {
        if (animator == null)
            return;

        animator.SetBool(inBoxBoolParam, isInBox);

        if (isInBox)
        {
            animator.ResetTrigger(hitTriggerParam);
            animator.SetBool(draggedBoolParam, false);

            if (!string.IsNullOrEmpty(inBoxStateName))
                animator.Play(inBoxStateName, 0, 0f);
        }
    }

    public void PlayHit()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(hitTriggerParam);
        animator.SetTrigger(hitTriggerParam);
    }

    private void ApplyFacing()
    {
        if (animator == null)
            return;

        animator.SetFloat(faceXParam, currentFacing.x);
        animator.SetFloat(faceYParam, currentFacing.y);
    }
}