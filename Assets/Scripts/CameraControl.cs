using UnityEngine;

public class CameraControl : MonoBehaviour
{
	[SerializeField]
	private float rotateSpeed = 5f, zoomSpeed = 8f, zoomLimit = 1f;

	private float min, max;

	void Awake()
	{
		float x = 5.25f, y = 7.5f, pitch = 60f;
		min = pitch - Mathf.Rad2Deg * Mathf.Atan(y / x);
		Debug.Assert(min > 1f);
		max = 180f - min;
		transform.position = GameManager.boardCenter - x * GameManager.tileForward + y * GameManager.tileUp;
		transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
	}

	void Update()
	{
		// Rotating
		if (Input.GetMouseButton(1))
		{
			// Rotating
			transform.RotateAround(GameManager.boardCenter, Vector3.up, Input.GetAxis("Mouse X") * rotateSpeed);
			float currentAngle = Vector3.Angle(transform.position - GameManager.boardCenter, Vector3.up);
			float rotateAngle = Mathf.Clamp(Input.GetAxis("Mouse Y") * rotateSpeed, min - currentAngle, max - currentAngle);
			transform.RotateAround(GameManager.boardCenter, -transform.right, rotateAngle);
		}

		// Zooming
		Vector3 dir = GameManager.boardCenter - transform.position;
		float distance = dir.magnitude;
		float translation = Mathf.Min(Input.GetAxis("Mouse ScrollWheel") * zoomSpeed, distance - zoomLimit);
		transform.Translate(translation * (dir / distance), Space.World);
	}
}
