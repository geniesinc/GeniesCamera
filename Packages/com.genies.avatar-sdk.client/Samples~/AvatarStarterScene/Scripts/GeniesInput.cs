using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Genies.Sdk.Samples.AvatarStarter
{
    public class GeniesInputs : MonoBehaviour
    {
		private enum InputSystemType
		{
			LegacyInputManager,
			NewInputSystem,
			Both
		}

        public Vector2 Move
		{
			get => _move;
			set => _move = value;
		}

        public Vector2 Look
		{
			get => _look;
			set => _look = value;
		}

        public bool Jump
		{
			get => _jump;
			set => _jump = value;
		}

        public bool Sprint
		{
			get => _sprint;
			set => _sprint = value;
		}

        public bool AnalogMovement
		{
			get => _analogMovement;
			set => _analogMovement = value;
		}

        public bool CursorLocked
		{
			get => _cursorLocked;
			set => _cursorLocked = value;
		}

        public bool CursorInputForLook
		{
			get => _cursorInputForLook;
			set => _cursorInputForLook = value;
		}

		private InputSystemType ActiveInputSystemType
		{
			get
			{
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
				return _inputSystemType;
#elif ENABLE_INPUT_SYSTEM
				return InputSystemType.NewInputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
				return InputSystemType.LegacyInputManager;
#else
				return InputSystemType.NewInputSystem;
#endif
			}
		}

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
		[Header("Input System Selection")]
		[SerializeField] private InputSystemType _inputSystemType = InputSystemType.NewInputSystem;
#endif

#if ENABLE_INPUT_SYSTEM
		[Header("New Input System")]
		[SerializeField] private InputActionReference _moveAction;

		[SerializeField] private InputActionReference _lookAction;

		[SerializeField] private InputActionReference _jumpAction;

		[SerializeField] private InputActionReference _sprintAction;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
		[Header("Legacy Input Manager Settings")]
		[SerializeField] private string _horizontalAxisName = "Horizontal";

		[SerializeField] private string _verticalAxisName = "Vertical";

		[SerializeField] private string _mouseXAxisName = "Mouse X";

		[SerializeField] private string _mouseYAxisName = "Mouse Y";

		[SerializeField] private string _jumpButtonName = "Jump";

		[SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
#endif

        [Header("Character Input Values (Read-Only)")]
		[SerializeField] private Vector2 _move;

		[SerializeField] private Vector2 _look;

		[SerializeField] private bool _jump;

		[SerializeField] private bool _sprint;

		[Header("Movement Settings (Read-Only)")]
		[SerializeField] private bool _analogMovement;

		[Header("Mouse Cursor Settings (Read-Only)")]
		[SerializeField] private bool _cursorLocked = true;

		[SerializeField] private bool _cursorInputForLook = true;

	public void MoveInput(Vector2 newMoveDirection)
	{
		Move = newMoveDirection;
	}

		public void LookInput(Vector2 newLookDirection)
		{
			Look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			Jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			Sprint = newSprintState;
		}

#if ENABLE_INPUT_SYSTEM
	private void OnEnable()
	{
		if (ActiveInputSystemType == InputSystemType.NewInputSystem || ActiveInputSystemType == InputSystemType.Both)
		{
			InitializeInputActions();
		}
	}

	private void InitializeInputActions()
	{
		if (_moveAction != null && _moveAction.action != null)
		{
			_moveAction.action.Enable();
		}
		else
		{
			Debug.LogWarning("Move Action is not assigned in the Inspector.", this);
		}

		if (_lookAction != null && _lookAction.action != null)
		{
			_lookAction.action.Enable();
		}
		else
		{
			Debug.LogWarning("Look Action is not assigned in the Inspector.", this);
		}

		if (_jumpAction != null && _jumpAction.action != null)
		{
			_jumpAction.action.Enable();
		}
		else
		{
			Debug.LogWarning("Jump Action is not assigned in the Inspector.", this);
		}

		if (_sprintAction != null && _sprintAction.action != null)
		{
			_sprintAction.action.Enable();
		}
		else
		{
			Debug.LogWarning("Sprint Action is not assigned in the Inspector.", this);
		}
	}

	private void OnDisable()
	{
		if (_moveAction != null && _moveAction.action != null)
		{
			_moveAction.action.Disable();
		}

		if (_lookAction != null && _lookAction.action != null)
		{
			_lookAction.action.Disable();
		}

		if (_jumpAction != null && _jumpAction.action != null)
		{
			_jumpAction.action.Disable();
		}

		if (_sprintAction != null && _sprintAction.action != null)
		{
			_sprintAction.action.Disable();
		}
	}
#endif

	private void Update()
	{
		switch (ActiveInputSystemType)
		{
			case InputSystemType.NewInputSystem:
#if ENABLE_INPUT_SYSTEM
				UpdateNewInputSystem();
#endif
				break;

			case InputSystemType.LegacyInputManager:
#if ENABLE_LEGACY_INPUT_MANAGER
				UpdateLegacyInputSystem();
#endif
				break;

			case InputSystemType.Both:
#if ENABLE_INPUT_SYSTEM
				UpdateNewInputSystem();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
				UpdateLegacyInputSystem();
#endif
				break;
		}
	}

#if ENABLE_INPUT_SYSTEM
	private void UpdateNewInputSystem()
	{
		// Movement input
		if (_moveAction != null && _moveAction.action != null && _moveAction.action.enabled)
		{
			MoveInput(_moveAction.action.ReadValue<Vector2>());
		}

		// Look input
		if (CursorInputForLook && _lookAction != null && _lookAction.action != null && _lookAction.action.enabled)
		{
			Vector2 lookDirection = _lookAction.action.ReadValue<Vector2>();
			lookDirection.y *= -1f;
			LookInput(lookDirection);
		}

		// Jump input
		if (_jumpAction != null && _jumpAction.action != null && _jumpAction.action.enabled)
		{
			JumpInput(_jumpAction.action.IsPressed());
		}

		// Sprint input
		if (_sprintAction != null && _sprintAction.action != null && _sprintAction.action.enabled)
		{
			SprintInput(_sprintAction.action.IsPressed());
		}
	}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
	private void UpdateLegacyInputSystem()
	{
		// Movement input (WASD or Arrow keys)
		Vector2 moveDirection = new Vector2(
			Input.GetAxisRaw(_horizontalAxisName),
			Input.GetAxisRaw(_verticalAxisName)
		);
		MoveInput(moveDirection);

		// Look input (Mouse movement)
		if (CursorInputForLook)
		{
			Vector2 lookDirection = new Vector2(
				Input.GetAxis(_mouseXAxisName),
				-1 * Input.GetAxis(_mouseYAxisName)
			);
			LookInput(lookDirection);
		}

		// Jump input
		if (Input.GetButtonDown(_jumpButtonName))
		{
			JumpInput(true);
		}
		else if (Input.GetButtonUp(_jumpButtonName))
		{
			JumpInput(false);
		}

		// Sprint input
		if (Input.GetKeyDown(_sprintKey))
		{
			SprintInput(true);
		}
		else if (Input.GetKeyUp(_sprintKey))
		{
			SprintInput(false);
		}
	}
#endif
	}
}
