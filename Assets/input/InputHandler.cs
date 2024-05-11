using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour {

    public Player player;
    public GameCamera cam;
    public PlanetSettings planetSettings;

    InputActions inputs;
    InputAction moveAction;
    InputAction boostAction;

    private void OnEnable()
    {
        inputs = new InputActions();
        inputs.Enable();
        moveAction = inputs.FindAction("Move");
        boostAction = inputs.FindAction("Boost");

        cam.SetPlayerMaxSpeed(player.maxSpeed);
        player.SetWorldRadius(planetSettings.radius);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 move = moveAction.ReadValue<Vector3>();
        player.UpdateInput(move.z, move.x, move.y, boostAction.IsPressed());
        cam.SetPlayerCurrentSpeed(player.currentSpeed);
    }


}
