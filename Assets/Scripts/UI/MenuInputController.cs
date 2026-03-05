using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives UI navigation in non-gameplay scenes (title, main menu, save select, boss select)
/// using the same controls as PlayerControls:
/// - Move (stick / D-pad / WASD) to move between buttons
/// - Dash to submit / select
/// - Jump to go back / cancel
/// - Start to confirm / start when appropriate
///
/// Attach this to a GameObject in UI-focused scenes. Make sure there is an EventSystem
/// and that "firstSelected" is set on the EventSystem or a button.
/// </summary>
public class MenuInputController : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("First selectable UI element when this menu becomes active (optional; falls back to EventSystem.firstSelectedObject).")]
    public GameObject firstSelected;

    [Tooltip("Optional: name of the previous scene to load when Jump is used as Back and no local handler consumes it.")]
    public string backSceneName = "";

    private PlayerControls _controls;
    private EventSystem _eventSystem;

    private void Awake()
    {
        _controls = new PlayerControls();
        _eventSystem = EventSystem.current;
    }

    private void OnEnable()
    {
        if (_controls == null)
            _controls = new PlayerControls();

        // Navigation (same as player movement)
        _controls.Player.Move.performed += OnMove;
        _controls.Player.Move.canceled += OnMoveCanceled;

        // Dash = Submit / Select
        _controls.Player.Dash.performed += OnSubmit;

        // Jump = Back / Cancel
        _controls.Player.Jump.performed += OnBack;

        // Start = Start / Confirm (maps to EventSystem submit as well)
        _controls.Player.CombatStyleMenu.performed += OnStart;

        _controls.Player.Enable();

        SelectFirst();
    }

    private void OnDisable()
    {
        if (_controls != null)
        {
            _controls.Player.Move.performed -= OnMove;
            _controls.Player.Move.canceled  -= OnMoveCanceled;
            _controls.Player.Dash.performed -= OnSubmit;
            _controls.Player.Jump.performed -= OnBack;
            _controls.Player.CombatStyleMenu.performed -= OnStart;
            _controls.Player.Disable();
        }
    }

    private void SelectFirst()
    {
        if (_eventSystem == null)
            _eventSystem = EventSystem.current;
        if (_eventSystem == null) return;

        GameObject toSelect = firstSelected != null ? firstSelected : _eventSystem.firstSelectedGameObject;
        if (toSelect != null)
            _eventSystem.SetSelectedGameObject(toSelect);
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (_eventSystem == null)
            _eventSystem = EventSystem.current;
        if (_eventSystem == null) return;

        Vector2 move = ctx.ReadValue<Vector2>();
        if (move == Vector2.zero) return;

        // Let the current selected object handle navigation via the current InputModule.
        // Here we simply ensure something is selected so keyboard/controller navigation works.
        if (_eventSystem.currentSelectedGameObject == null)
            SelectFirst();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        // Nothing special needed; UI modules handle repeated navigation.
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (_eventSystem == null)
            _eventSystem = EventSystem.current;
        if (_eventSystem == null) return;

        var selected = _eventSystem.currentSelectedGameObject;
        if (selected == null)
        {
            SelectFirst();
            selected = _eventSystem.currentSelectedGameObject;
        }

        if (selected != null)
        {
            // Simulate a Submit event on the currently selected UI element
            ExecuteEvents.Execute(selected, new BaseEventData(_eventSystem), ExecuteEvents.submitHandler);
        }
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        // "Jump" as Back / Cancel in menus:
        // - First, send a Cancel event to the currently selected UI element.
        // - If nothing handles it and backSceneName is set, load that scene.
        if (_eventSystem == null)
            _eventSystem = EventSystem.current;
        if (_eventSystem == null) return;

        var selected = _eventSystem.currentSelectedGameObject;
        if (selected != null)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(_eventSystem), ExecuteEvents.cancelHandler);
        }

        if (!string.IsNullOrEmpty(backSceneName))
        {
            ScreenFader.LoadScene(backSceneName);
        }
    }

    private void OnStart(InputAction.CallbackContext ctx)
    {
        // Treat Start the same as Submit in menus
        OnSubmit(ctx);
    }
}

