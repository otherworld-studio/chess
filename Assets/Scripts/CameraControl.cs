using UnityEngine;

public class CameraControl : MonoBehaviour
{
	[SerializeField]
	private GameManager gameManager;
	[SerializeField]
	private const float speed = 5f;

	void Update()
	{
		if (Input.GetMouseButton(1))
		{
			transform.RotateAround(gameManager.boardCenter, Vector3.up, Input.GetAxis("Mouse X") * speed);
			float currentAngle = Vector3.Angle(transform.position - gameManager.boardCenter, Vector3.up);
			float rotateAngle = Mathf.Clamp(currentAngle + Input.GetAxis("Mouse Y") * speed, 1f, 179f) - currentAngle;
			transform.RotateAround(gameManager.boardCenter, -transform.right, rotateAngle);
		}
	}
}
