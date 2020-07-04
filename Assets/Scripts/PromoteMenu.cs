using UnityEngine;
using UnityEngine.UI;

public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private Button knightButton, bishopButton, rookButton, queenButton;

    private Collider collider;

    void Awake()
    {
        collider = GetComponent<Collider>();
    }

    void OnEnable()
    {
        if (GetComponent<Renderer>().isVisible) canvas.SetActive(true);
    }

    void OnDisable()
    {
        canvas.SetActive(false);
    }

    void OnBecameVisible()
    {
        if (enabled) canvas.SetActive(true);
    }

    void OnBecameInvisible()
    {
        canvas.SetActive(false);
    }

    void Update()
    {
        Rect bounds = boundingBox2D;
        // TODO: draw each button slightly outside of the bounding box, with a minimum distance from the center to prevent overlap
        Vector2 centerPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, collider.bounds.center);
        knightButton.transform.position = centerPoint + new Vector2(radius, 0f);
        bishopButton.transform.position = centerPoint + new Vector2(0f, -radius);
        rookButton.transform.position = centerPoint + new Vector2(-radius, 0f);
        queenButton.transform.position = centerPoint + new Vector2(0f, radius);
    }

    // TODO
    private float CameraHeight()
    {
        float min = Camera.main.WorldToScreenPoint(collider.bounds.min).y, max = Camera.main.WorldToScreenPoint(collider.bounds.max).y;
        return Mathf.Abs((max - min) / Camera.main.pixelHeight);
    }

    private Rect boundingBox2D { get
        {
            Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;

            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity, xMax = 0f, yMax = 0f;
            foreach (Vector3 v in vertices)
            {
                Vector2 pos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.TransformPoint(v));

                if (pos.x < xMin) xMin = pos.x;
                if (pos.y < yMin) yMin = pos.y;
                if (pos.x > xMax) xMax = pos.x;
                if (pos.y > yMax) yMax = pos.y;
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
