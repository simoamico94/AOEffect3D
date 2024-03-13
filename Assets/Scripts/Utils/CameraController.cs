using UnityEngine;

public class CameraController : MonoBehaviour
{
	public AOEffectManager manager;

	public float moveSpeed = 5f;
	public float rotateSpeed = 2f;
	public float verticalSpeed = 3f;
	public float shiftMultiplier = 2f;

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
		if(manager.gameState.GameMode == GameMode.Playing)
		{
			// Use different input methods depending on the platform
	#if UNITY_STANDALONE || UNITY_WEBGL
			HandleMovement();
			HandleRotation();
	#elif UNITY_IOS || UNITY_ANDROID
			HandleTouchMovement();
			HandleTouchRotation();
	#endif
		}
		else
		{
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

		transform.position += movement;
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
				transform.position += transform.right * movement.x + transform.forward * movement.z;
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
}
