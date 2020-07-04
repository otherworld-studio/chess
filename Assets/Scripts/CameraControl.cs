using UnityEngine;

public class CameraControl : MonoBehaviour
{
	[SerializeField]
	private float rotateSpeed = 4f, zoomSpeed = 8f, zoomLimit = 1f;

	private float minAngle, maxAngle;
	private Vector3 center;

	void Awake()
	{
		float x = 5.25f, y = 7.5f, pitch = 60f;
		minAngle = pitch - Mathf.Rad2Deg * Mathf.Atan(y / x);
		Debug.Assert(minAngle > 1f);
		maxAngle = 180f - minAngle;
		center = GameManager.boardCenter;
		transform.position = center - x * GameManager.tileForward + y * GameManager.tileUp;
		transform.localRotation = Quaternion.Euler(Vector3.right * pitch);
	}

	void Update()
	{
		// Rotating
		if (Input.GetMouseButton(1))
		{
			// Rotating
			transform.RotateAround(center, Vector3.up, Input.GetAxis("Mouse X") * rotateSpeed);
			float currentAngle = Vector3.Angle(transform.position - center, Vector3.up);
			float rotateAngle = Mathf.Clamp(Input.GetAxis("Mouse Y") * rotateSpeed, minAngle - currentAngle, maxAngle - currentAngle);
			transform.RotateAround(center, -transform.right, rotateAngle);
		}

		// Zooming
		Vector3 dir = center - transform.position;
		float distance = dir.magnitude;
		float translation = Mathf.Min(Input.GetAxis("Mouse ScrollWheel") * zoomSpeed, distance - zoomLimit);
		transform.Translate(translation * (dir / distance), Space.World);
	}
}
