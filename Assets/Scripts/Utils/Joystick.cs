using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField]
	private RectTransform background = null; // The background of the joystick

	[SerializeField]
	private RectTransform handle = null; // The handle of the joystick

	private Vector2 inputVector = Vector2.zero;
	private Canvas canvas; // Reference to the canvas

	public float Horizontal => inputVector.x;
	public float Vertical => inputVector.y;

	private void Start()
	{
		if (background == null)
			background = GetComponent<RectTransform>();

		if (handle == null && transform.childCount > 0)
			handle = transform.GetChild(0).GetComponent<RectTransform>();

		canvas = GetComponentInParent<Canvas>(); // Find the Canvas in the parent hierarchy
		if (canvas == null)
			Debug.LogError("Joystick: The Canvas component was not found in the parent hierarchy. Make sure this joystick is a child of a Canvas.");
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (canvas == null) return; // Guard clause if canvas is not found

		Vector2 position = RectTransformUtility.WorldToScreenPoint(null, background.position);
		Vector2 radius = background.sizeDelta / 2;
		inputVector = (eventData.position - position) / (radius * canvas.scaleFactor); // Use the canvas's scaleFactor
		HandleInput(inputVector.magnitude, inputVector.normalized, radius, Camera.main);
		handle.anchoredPosition = inputVector * radius * 0.4f; // Adjust for handle's movement range
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		OnDrag(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		inputVector = Vector2.zero;
		handle.anchoredPosition = Vector2.zero;
	}

	private void HandleInput(float magnitude, Vector2 normalized, Vector2 radius, Camera cam)
	{
		if (magnitude > 0.1f)
		{
			if (magnitude > 1)
				inputVector = normalized;
		}
		else
			inputVector = Vector2.zero;
	}
}
