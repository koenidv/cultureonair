using UnityEngine;
using System.Collections;
 
[AddComponentMenu("Camera-Control/Drag Orbit & Zoom")]
public class DragOrbit : MonoBehaviour
{
 
    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
 
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
 
    public float distanceMin = .5f;
    public float distanceMax = 15f;

    public bool clampY = true;
 
    private new Rigidbody rigidbody;
 
    float x = 0.0f;
    float y = 0.0f;

    float velocityX = 0.05f;
    float velocityY = 0.0f;
 
    bool orbitable;
 
    // Use this for 
    void Start() {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
 
        rigidbody = GetComponent<Rigidbody>();
 
        // Make the rigid body not change rotation
        if (rigidbody != null)
        {
            rigidbody.freezeRotation = true;
        }
    }
 
    void LateUpdate() {
        /*
        if (Input.GetKeyDown(KeyCode.LeftControl) == true)
        {
            orbitable = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl) == true)
        {
            orbitable = false;
        }
        */
 
        if (Input.GetMouseButtonDown(0))
            orbitable = true;
        else if (Input.GetMouseButtonUp(0))
            orbitable = false;
 
        if (orbitable) { 
            if (target) {
                float oldX = x;
                float oldY = y;

                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                velocityX = x - oldX;
                velocityY = y - oldY;
 
                if (clampY) {
                    y = ClampAngle(y, yMinLimit, yMaxLimit);
                }
 
                distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
 
                updateWithRotation(x, y, distance);
            }
        } else {
            x += velocityX;
            y += velocityY;
            updateWithRotation(x, y, distance);
        }
    }

    void updateWithRotation(float x, float y, float distance) {
        Quaternion rotation = Quaternion.Euler(y, x, 0);
 
                RaycastHit hit;
                if (Physics.Linecast(target.position, transform.position, out hit))
                {
                    distance -= hit.distance;
                }
                Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
                Vector3 position = rotation * negDistance + target.position;
 
                transform.rotation = rotation;
                transform.position = position;
    }

 
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}