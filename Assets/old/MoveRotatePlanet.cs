using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using UnityEngine;

public class MoveRotatePlanet : MonoBehaviour {
    public float speed = 4f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 rotation = new Vector3(-input.z, input.x, 0f) * speed;
        transform.Rotate(rotation * Time.deltaTime);
    }
}
