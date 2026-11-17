using UnityEngine;

/// <summary>
///  World‑Space Canvas or GameObject will always orient to Main Camera
/// Edit‑mode & Play‑mode
/// </summary>
[DisallowMultipleComponent]
public class LookAtCamera : MonoBehaviour
{
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;   

        Vector3 direction = transform.position - _cam.transform.position;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}