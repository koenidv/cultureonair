using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;

public class InputHandler : MonoBehaviour
{

    public Player player;
    public GameCamera cam;
    public PlanetSettings planetSettings;

    InputActions.DefaultActions inputMap;
    InputAction moveAction;
    InputAction boostAction;
    InputAction switchViewAction;
    InputAction cameraModifierAction;

    private void OnEnable()
    {
        inputMap = new InputActions().Default;
        inputMap.Enable();

        moveAction = inputMap.Move;
        boostAction = inputMap.Boost;
        cameraModifierAction = inputMap.CameraModifier;

        inputMap.View.performed += _ => cam.GotoNextViewType();

        cam.SetPlayerMaxSpeed(player.maxSpeed);
        player.SetWorldRadius(planetSettings.radius);

        TouchSimulation.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 move = moveAction.ReadValue<Vector3>();
        player.UpdateInput(move.z, move.x, move.y, boostAction.IsPressed());
        cam.SetPlayerCurrentSpeed(player.currentSpeed);

        cam.UpdateModifierChanging(cameraModifierAction.IsInProgress());
        if (cameraModifierAction.IsInProgress() && move.magnitude == 0)
        {
            cam.UpdateModifierInput(cameraModifierAction.ReadValue<TouchState>().delta);
        }

    }


}
