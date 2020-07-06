using System.Collections;
using UnityEngine;

// We don't attach this to the actual Canvas because we disable it when not visible
public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private Transform knightButton, bishopButton, rookButton, queenButton;

    private Mesh mesh;

    private const float buttonSize = 50f;
    private const float speed = 10f;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    void OnEnable()
    {
        if (GetComponent<Renderer>().isVisible)
            canvas.SetActive(true); // Set canvas to active before coroutine begins
        animateButtonsCoroutine = StartCoroutine(AnimateButtonsRoutine());
    }
    
    void OnDisable()
    {
        if (animateButtonsCoroutine != null)
            StopCoroutine(animateButtonsCoroutine);
        animateButtonsCoroutine = null;
        canvas.SetActive(false);
    }

    void OnBecameVisible()
    {
        if (enabled)
        {
            canvas.SetActive(true);
            UpdateButtons(); // Because Update() isn't called after OnBecameVisible in the same frame 
        }
    }

    void OnBecameInvisible()
    {
        canvas.SetActive(false);
    }

    void Update()
    {
        if (canvas.activeSelf && animateButtonsCoroutine == null)
            UpdateButtons();
    }

    private void UpdateButtons()
    {
        Rect bounds = boundingBox2D;
        float w = buttonSize + bounds.width * 0.5f;
        float h = buttonSize + bounds.height * 0.5f;

        knightButton.position = bounds.center + new Vector2(w, 0f);
        bishopButton.position = bounds.center + new Vector2(0f, -h);
        rookButton.position = bounds.center + new Vector2(-w, 0f);
        queenButton.position = bounds.center + new Vector2(0f, h);
    }

    private Coroutine animateButtonsCoroutine;
    private IEnumerator AnimateButtonsRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            if (canvas.activeSelf)
            {
                Rect bounds = boundingBox2D;
                float w = buttonSize + bounds.width * 0.5f;
                float h = buttonSize + bounds.height * 0.5f;

                knightButton.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(w, 0f), t);
                bishopButton.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(0f, -h), t);
                rookButton.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(-w, 0f), t);
                queenButton.position = Vector2.Lerp(bounds.center, bounds.center + new Vector2(0f, h), t);

                Vector3 scale = t * Vector3.one;
                knightButton.localScale = scale;
                bishopButton.localScale = scale;
                rookButton.localScale = scale;
                queenButton.localScale = scale;
            }

            yield return null;

            t += Time.deltaTime * speed;
        }

        UpdateButtons();

        knightButton.localScale = Vector3.one;
        bishopButton.localScale = Vector3.one;
        rookButton.localScale = Vector3.one;
        queenButton.localScale = Vector3.one;

        animateButtonsCoroutine = null;
    }

    private Rect boundingBox2D { get
        {
            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity, xMax = 0f, yMax = 0f;
            foreach (Vector3 v in mesh.vertices)
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
