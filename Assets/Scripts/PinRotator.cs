using UnityEngine;

public class PinRotator : MonoBehaviour
{
    public float rotateSpeed = 30f;
    public float floatSpeed = 1f;
    public float floatAmount = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Rotate
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        // Float
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}