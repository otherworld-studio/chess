using UnityEngine;
using UnityEngine.UI;

public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private Collider collider;
    [SerializeField]
    private Button knightButton, bishopButton, rookButton, queenButton;

    private const float radius = 100f;

    void Update()
    {
        // TODO: use RectTransform instead?
        // TODO: make menu smaller when zoomed out?
        Vector3 centerPoint = Camera.main.WorldToScreenPoint(collider.bounds.center);
        knightButton.transform.position = centerPoint + radius * GameManager.tileRight;
        bishopButton.transform.position = centerPoint - radius * GameManager.tileUp;
        rookButton.transform.position = centerPoint - radius * GameManager.tileRight;
        queenButton.transform.position = centerPoint + radius * GameManager.tileUp;
    }
}
