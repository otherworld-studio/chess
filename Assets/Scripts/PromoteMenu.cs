using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// We don't attach this to the actual Canvas because we disable it when not visible
public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private Button knightButton, bishopButton, rookButton, queenButton;

    private bool inMotion;

    private const float buttonSize = 50f;
    private const float speed = 10f;

    void OnEnable()
    {
        if (GetComponent<Renderer>().isVisible) canvas.SetActive(true); // Canvas MUST be set active before coroutine begins (see coroutine)
        StartCoroutine(AnimateButtons());
    }
    
    void OnDisable()
    {
        StopAllCoroutines();
        canvas.SetActive(false);
    }

    void OnBecameVisible()
    {
        if (enabled)
        {
            canvas.SetActive(true);
            Update(); // Update() isn't called automatically in the same frame, so we must do it ourselves
        }
    }

    void OnBecameInvisible()
    {
        canvas.SetActive(false);
    }

    void Update()
    {
        if (canvas.activeSelf && !inMotion) UpdateButtons();
    }

    private void UpdateButtons()
    {
        Rect bounds = boundingBox2D;
        float w = buttonSize + bounds.width * 0.5f;
        float h = buttonSize + bounds.height * 0.5f;

        knightButton.transform.position = bounds.center + new Vector2(w, 0f);
        bishopButton.transform.position = bounds.center + new Vector2(0f, -h);
        rookButton.transform.position = bounds.center + new Vector2(-w, 0f);
        queenButton.transform.position = bounds.center + new Vector2(0f, h);
    }

    private IEnumerator AnimateButtons()
    {
        inMotion = true;

        float t = 0f;
        while (t < 1f)
        {
            if (canvas.activeSelf)
            {
                Rect bounds = boundingBox2D;
                float w = buttonSize + bounds.width * 0.5f;
                float h = buttonSize + bounds.height * 0.5f;

                knightButton.transform.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(w, 0f), t);
                bishopButton.transform.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(0f, -h), t);
                rookButton.transform.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(-w, 0f), t);
                queenButton.transform.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(0f, h), t);

                Vector3 scale = t * Vector3.one;
                knightButton.transform.localScale = scale;
                bishopButton.transform.localScale = scale;
                rookButton.transform.localScale = scale;
                queenButton.transform.localScale = scale;
            }

            yield return null;

            t += Time.deltaTime * speed;
        }

        UpdateButtons();

        knightButton.transform.localScale = Vector3.one;
        bishopButton.transform.localScale = Vector3.one;
        rookButton.transform.localScale = Vector3.one;
        queenButton.transform.localScale = Vector3.one;

        inMotion = false;
    }

    private Rect boundingBox2D { get
        {
            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity, xMax = 0f, yMax = 0f;
            foreach (Vector3 v in GetComponent<MeshFilter>().mesh.vertices)
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
