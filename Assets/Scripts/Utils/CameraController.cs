using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float rotateSpeed = 2f;
	[SerializeField] private float verticalSpeed = 3f;
	[SerializeField] private float shiftMultiplier = 2f;

	[Header("Mobile")]

	[SerializeField] private GameObject joystickPanel;

	[SerializeField] private Joystick movementJoystick;
	[SerializeField] private Joystick rotationJoystick; 

	private Vector3 minBounds = new Vector3(-5, 0.5f, -5);
	private Vector3 maxBounds;

	private bool isRotating;
	private Vector2 previousTouchPosition;
	private float initialTouchDistance;
	private float lastTouchDistance;

	private Vector3 startPos;
	private Quaternion startRot;

	private void Start()
	{
		startPos = transform.position;
		startRot = transform.rotation;
	}

	void Update()
	{
		if(AOEffectManager.main.gameState.gameMode == GameMode.Playing)
		{
			maxBounds = new Vector3(AOEffectManager.main.gridManager.gridSizeX + 5, 10, AOEffectManager.main.gridManager.gridSizeZ + 5);

#if UNITY_STANDALONE || UNITY_WEBGL
			HandleMovement();
			HandleRotation();
#elif UNITY_IOS || UNITY_ANDROID
			joystickPanel.SetActive(true);
			HandleJoystickMovement();
			HandleJoystickRotation();
#endif
		}
		else
		{
#if UNITY_IOS || UNITY_ANDROID
			joystickPanel.SetActive(false);
#endif
			transform.position = startPos;
			transform.rotation = startRot;
		}
	}

	void HandleMovement()
	{
		float moveSpeedMultiplier = Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f;
		float horizontalInput = Input.GetAxis("Horizontal");
		float verticalInput = Input.GetAxis("Vertical");
		float upDownInput = Input.GetKey(KeyCode.E) ? 1f : Input.GetKey(KeyCode.Q) ? -1f : 0f;

		Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
		Vector3 verticalMovement = Vector3.up * upDownInput * verticalSpeed * Time.deltaTime;
		Vector3 horizontalMovement = transform.right * moveDirection.x + transform.forward * moveDirection.z;
		Vector3 movement = (horizontalMovement + verticalMovement) * moveSpeed * moveSpeedMultiplier * Time.deltaTime;

		Vector3 newPosition = transform.position + movement;

		// Clamp the new position to ensure it's within the bounds
		newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
		newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
		newPosition.z = Mathf.Clamp(newPosition.z, minBounds.z, maxBounds.z);

		// Apply the clamped position
		transform.position = newPosition;
	}

	void HandleRotation()
	{
		if (Input.GetMouseButtonDown(1))
		{
			isRotating = true;
		}
		else if (Input.GetMouseButtonUp(1))
		{
			isRotating = false;
		}

		if (isRotating)
		{
			float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
			float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed;

			transform.rotation *= Quaternion.Euler(-mouseY, mouseX, 0f);
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
		}
	}

	void HandleJoystickMovement()
	{
		// Get input from the movement joystick
		float horizontalInput = movementJoystick.Horizontal;
		float verticalInput = movementJoystick.Vertical;

		Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput)/*.normalized*/;
		Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

		Vector3 newPosition = transform.position + transform.right * movement.x + transform.forward * movement.z;

		// Clamp the new position to ensure it's within the bounds
		newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
		newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
		newPosition.z = Mathf.Clamp(newPosition.z, minBounds.z, maxBounds.z);

		// Apply the clamped position
		transform.position = newPosition;
	}

	void HandleJoystickRotation()
	{
		// Get input from the rotation joystick
		float rotationX = rotationJoystick.Horizontal * rotateSpeed/2;
		float rotationY = rotationJoystick.Vertical * rotateSpeed/2;

		// Apply rotation
		transform.rotation *= Quaternion.Euler(-rotationY, rotationX, 0f);
		// Prevent rotation around the Z axis
		transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
	}
}
