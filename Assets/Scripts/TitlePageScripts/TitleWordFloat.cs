using UnityEngine;

public class TitleWordFloat : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSpeed = 1.5f;

    [Header("Scale Pulse")]
    [SerializeField] private float pulseAmount = 0.03f;
    [SerializeField] private float pulseSpeed = 1.8f;

    [Header("Rotation Wiggle")]
    [SerializeField] private float rotationAmount = 1.5f;
    [SerializeField] private float rotationSpeed = 1.2f;

    [Header("Variation")]
    [SerializeField] private float timeOffset = 0f;

    private Vector3 startPosition;
    private Vector3 startScale;

    private void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;

        if (timeOffset == 0f)
        {
            timeOffset = Random.Range(0f, 10f);
        }
    }

    private void Update()
    {
        float t = Time.time + timeOffset;

        float bob = Mathf.Sin(t * bobSpeed) * bobAmount;
        float pulse = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
        float rot = Mathf.Sin(t * rotationSpeed) * rotationAmount;

        transform.localPosition = startPosition + new Vector3(0f, bob, 0f);
        transform.localScale = new Vector3(startScale.x * pulse, startScale.y * pulse, startScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, rot);
    }
}