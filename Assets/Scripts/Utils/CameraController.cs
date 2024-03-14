using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float rotateSpeed = 2f;
	[SerializeField] private float verticalSpeed = 3f;
	[SerializeField] private float shiftMultiplier = 2f;

	[SerializeField] private GameObject joystickPanel;

	// Reference to your joystick components
	[SerializeField] private Joystick movementJoystick; // Assign in inspector
	[SerializeField] private Joystick rotationJoystick; // Assign in inspector

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
		if(AOEffectManager.main.gameState.GameMode == GameMode.Playing)
		{
			maxBounds = new Vector3(AOEffectManager.main.gridManager.gridSizeX + 5, 10, AOEffectManager.main.gridManager.gridSizeZ + 5);

			// Use different input methods depending on the platform
#if UNITY_STANDALONE || UNITY_WEBGL
			HandleMovement();
			HandleRotation();
#elif UNITY_IOS || UNITY_ANDROID
			//HandleTouchMovement();
			//HandleTouchRotation();
			joystickPanel.SetActive(true);
			HandleJoystickMovement();
			HandleJoystickRotation();
#endif
		}
		else
		{
			joystickPanel.SetActive(false);
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

	void HandleTouchMovement()
	{
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			Vector2 touchDeltaPosition = touch.deltaPosition;

			if (!isRotating) // Use single touch movement to move the camera if not rotating
			{
				Vector3 moveDirection = new Vector3(touchDeltaPosition.x, 0f, touchDeltaPosition.y).normalized;
				Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

				Vector3 newPosition = transform.position + transform.right * movement.x + transform.forward * movement.z;

				// Clamp the new position to ensure it's within the bounds
				newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
				newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
				newPosition.z = Mathf.Clamp(newPosition.z, minBounds.z, maxBounds.z);

				// Apply the clamped position
				transform.position = newPosition;
			}
		}
	}

	void HandleTouchRotation()
	{
		// Use two fingers to rotate the camera
		if (Input.touchCount == 2)
		{
			Touch touchOne = Input.GetTouch(0);
			Touch touchTwo = Input.GetTouch(1);

			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
			Vector2 touchTwoPrevPos = touchTwo.position - touchTwo.deltaPosition;

			float prevMagnitude = (touchOnePrevPos - touchTwoPrevPos).magnitude;
			float currentMagnitude = (touchOne.position - touchTwo.position).magnitude;
			float difference = currentMagnitude - prevMagnitude;

			if (Mathf.Abs(difference) > 0.01f) // Check if the fingers are moving closer or further away from each other
			{
				transform.position += transform.forward * difference * verticalSpeed * Time.deltaTime;
			}
			else // Rotate camera based on the average movement of the two touches
			{
				Vector2 midPointPrev = (touchOnePrevPos + touchTwoPrevPos) / 2;
				Vector2 midPointCurrent = (touchOne.position + touchTwo.position) / 2;
				Vector2 direction = midPointCurrent - midPointPrev;

				transform.rotation *= Quaternion.Euler(-direction.y * rotateSpeed * Time.deltaTime, direction.x * rotateSpeed * Time.deltaTime, 0f);
				transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
			}
		}
	}

	void HandleJoystickMovement()
	{
		// Get input from the movement joystick
		float horizontalInput = movementJoystick.Horizontal;
		float verticalInput = movementJoystick.Vertical;

		Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
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
