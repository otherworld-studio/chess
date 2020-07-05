using UnityEngine;
using UnityEngine.UI;

// We don't attach this to the actual Canvas because we disable it when not visible
public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private Button knightButton, bishopButton, rookButton, queenButton;

    private const float buttonSize = 50f; // TODO: when we eventually make the icons grow to size, this is their final size

    void OnEnable()
    {
        if (GetComponent<Renderer>().isVisible)
        {
            canvas.SetActive(true);
            Update(); // Update() isn't called automatically in the same frame, so we must do it ourselves
        }
    }

    void OnDisable()
    {
        canvas.SetActive(false);
    }

    void OnBecameVisible()
    {
        if (enabled)
        {
            canvas.SetActive(true);
            Update();
        }
    }

    void OnBecameInvisible()
    {
        canvas.SetActive(false);
    }

    void Update()
    {
        if (canvas.activeSelf) {
            Rect bounds = boundingBox2D;
            float w = buttonSize + bounds.width * 0.5f;
            float h = buttonSize + bounds.height * 0.5f;

            knightButton.transform.position = bounds.center + new Vector2(w, 0f);
            bishopButton.transform.position = bounds.center + new Vector2(0f, -h);
            rookButton.transform.position = bounds.center + new Vector2(-w, 0f);
            queenButton.transform.position = bounds.center + new Vector2(0f, h);
        }
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
