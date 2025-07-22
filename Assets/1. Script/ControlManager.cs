using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ControlManager : MonoBehaviour
{
    public enum ControlType
    {
        Player,
        Boat,
        None
    }
    public static ControlManager Instance;

    public CharacterContoller characterController;
    public ThirdPersonCameraController thirdPersonCameraController;
    public GrappleController grappleController;

    public ControlType currentControlType = ControlType.None;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if(characterController == null)
        {
            characterController = FindAnyObjectByType<CharacterContoller>();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        characterController.OnMove(context);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        thirdPersonCameraController.OnLook(context);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        characterController.OnJump(context);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnInterect(InputAction.CallbackContext context)
    {
        if (context.started)
        {
        }
        else if (context.performed)
        {
        }
        else if (context.canceled)
        {
        }
    }
    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            if (IsPointerOverUI(Mouse.current.position.ReadValue())) return;

            grappleController.OnGrapple();

        }
        else
        {
            if (IsPointerOverUI(Mouse.current.position.ReadValue())) return;

            grappleController.OnRelease();
        }
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
        }
    }

    public bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }
}
