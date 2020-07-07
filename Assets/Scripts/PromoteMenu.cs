using System.Collections;
using UnityEngine;

// We don't attach this to the actual Canvas because we disable it when not visible
public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private RectTransform knightButton, bishopButton, rookButton, queenButton;

    private Mesh mesh;

    private const float buttonSize = 50f;
    private const float speed = 10f;

    void Awake()
    {
        Vector2 sizeDelta = new Vector2(buttonSize, buttonSize);
        knightButton.sizeDelta = sizeDelta;
        bishopButton.sizeDelta = sizeDelta;
        rookButton.sizeDelta = sizeDelta;
        queenButton.sizeDelta = sizeDelta;

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
        Rect bounds = GetMenuRectangle();
        Vector2 right = new Vector2(buttonSize + bounds.width * 0.5f, 0f);
        Vector2 up = new Vector2(0f, buttonSize + bounds.height * 0.5f);

        knightButton.position = bounds.center + right;
        bishopButton.position = bounds.center - up;
        rookButton.position = bounds.center - right;
        queenButton.position = bounds.center + up;
    }

    private Coroutine animateButtonsCoroutine;
    private IEnumerator AnimateButtonsRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            if (canvas.activeSelf)
            {
                Rect bounds = GetMenuRectangle();
                Vector2 right = new Vector2(buttonSize + bounds.width * 0.5f, 0f);
                Vector2 up = new Vector2(0f, buttonSize + bounds.height * 0.5f);

                knightButton.position = Vector2.Lerp(bounds.center, bounds.center + right, t);
                bishopButton.position = Vector2.Lerp(bounds.center, bounds.center - up, t);
                rookButton.position = Vector2.Lerp(bounds.center, bounds.center - right, t);
                queenButton.position = Vector2.Lerp(bounds.center, bounds.center + up, t);

                Vector3 scale = new Vector3(t, t, t);
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

    private Rect GetBoundingBox2D()
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

    private Rect GetMenuRectangle()
    {
        Bounds bounds = mesh.bounds;
        Vector3 centerInWorld = transform.TransformPoint(bounds.center);
        Vector3 centerInViewport = Camera.main.WorldToViewportPoint(centerInWorld);
        Vector2 centerInScreen = Camera.main.ViewportToScreenPoint(centerInViewport);

        // Distance between left and right buttons is proportional only to magnification
        Vector3 screenLeftInWorld = Camera.main.ScreenToWorldPoint(new Vector3(centerInScreen.x - 1f, centerInScreen.y, centerInViewport.z)) - centerInWorld;
        Vector2 leftSideInScreen = Camera.main.WorldToScreenPoint(centerInWorld + screenLeftInWorld * (bounds.extents.x / screenLeftInWorld.magnitude));

        Ray r = Camera.main.ScreenPointToRay(new Vector2(centerInScreen.x, centerInScreen.y - 1f));
        Plane boardPlane = new Plane(Vector3.up, centerInWorld);
        float enter;
        bool success = boardPlane.Raycast(r, out enter);
        Debug.Assert(success);
        Vector3 screenDownInWorld = r.GetPoint(enter) - centerInWorld;
        Vector2 bottomSideInScreen = Camera.main.WorldToScreenPoint(new Vector3(centerInWorld.x, centerInWorld.y - bounds.extents.y, centerInWorld.z) + screenDownInWorld * (bounds.extents.x / screenDownInWorld.magnitude));
        if (bottomSideInScreen.y > centerInScreen.y)
            bottomSideInScreen = Camera.main.WorldToScreenPoint(new Vector3(centerInWorld.x, centerInWorld.y + bounds.extents.y, centerInWorld.z));

        Vector2 cornerInScreen = new Vector2(leftSideInScreen.x, bottomSideInScreen.y);

        return new Rect(cornerInScreen, 2 * (centerInScreen - cornerInScreen));
    }
}
