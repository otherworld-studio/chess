using System.Collections;
using UnityEngine;

public class PromoteMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas;
    [SerializeField]
    private RectTransform knightButton, bishopButton, rookButton, queenButton;
    [SerializeField]
    private MeshFilter boundsMeshFilter;

    private const float buttonSize = 50f;
    private const float speed = 10f;

    void Awake()
    {
        Vector2 sizeDelta = new Vector2(buttonSize, buttonSize);
        knightButton.sizeDelta = sizeDelta;
        bishopButton.sizeDelta = sizeDelta;
        rookButton.sizeDelta = sizeDelta;
        queenButton.sizeDelta = sizeDelta;
    }

    void OnEnable()
    {
        if (GetComponent<Renderer>().isVisible)
            canvas.SetActive(true); // Set canvas to active before coroutine begins
        animateButtonsCoroutine = StartCoroutine(AnimateButtonsRoutine());
    }
    
    void OnDisable()
    {
        canvas.SetActive(false);
        if (animateButtonsCoroutine != null)
            StopCoroutine(animateButtonsCoroutine);
        animateButtonsCoroutine = null;
    }

    void OnBecameVisible()
    {
        if (enabled)
        {
            canvas.SetActive(true);
            UpdateButtons(); // because Update() isn't called after OnBecameVisible in the same frame
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
        Rect boundingBox = GetBoundingBox2D();
        Vector2 right = new Vector2(buttonSize + boundingBox.width * 0.5f, 0f);
        Vector2 up = new Vector2(0f, buttonSize + boundingBox.height * 0.5f);

        knightButton.position = boundingBox.center + right;
        bishopButton.position = boundingBox.center - up;
        rookButton.position = boundingBox.center - right;
        queenButton.position = boundingBox.center + up;
    }

    private Coroutine animateButtonsCoroutine;
    private IEnumerator AnimateButtonsRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            if (canvas.activeSelf)
            {
                Rect bounds = GetBoundingBox2D();
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
        foreach (Vector3 v in boundsMeshFilter.sharedMesh.vertices)
        {
            Vector2 pos = RectTransformUtility.WorldToScreenPoint(Camera.main, boundsMeshFilter.transform.TransformPoint(v));

            if (pos.x < xMin) xMin = pos.x;
            if (pos.y < yMin) yMin = pos.y;
            if (pos.x > xMax) xMax = pos.x;
            if (pos.y > yMax) yMax = pos.y;
        }

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    /* The result of several attempts to make a faster GetBoundingBox2D - in the end I found that using a simpler cylindrical mesh was much easier
    private Rect GetMenuRectangle()
    {
        Bounds bounds = mesh.bounds;
        Vector3 centerInWorld = transform.TransformPoint(bounds.center);
        Vector3 centerInViewport = Camera.main.WorldToViewportPoint(centerInWorld);
        Vector2 centerInScreen = Camera.main.ViewportToScreenPoint(centerInViewport);

        // Distance between left and right buttons is proportional only to magnification
        Vector3 screenLeftInWorld = Camera.main.ScreenToWorldPoint(new Vector3(centerInScreen.x - 1f, centerInScreen.y, centerInViewport.z)) - centerInWorld;
        Vector2 leftEdgeInScreen = Camera.main.WorldToScreenPoint(centerInWorld + screenLeftInWorld * (bounds.extents.x / screenLeftInWorld.magnitude));

        // Distance between top and bottom buttons depends on magnification (extents.y) as well as a small correction for the base of the chess piece (extents.x)
        Vector3 bottomInWorld = new Vector3(centerInWorld.x, centerInWorld.y - bounds.extents.y, centerInWorld.z);
        Vector3 bottomInViewport = Camera.main.WorldToViewportPoint(bottomInWorld);
        Vector2 bottomInScreen = Camera.main.ViewportToScreenPoint(bottomInViewport);
        Vector3 screenDownInWorld = (Camera.main.ScreenToWorldPoint(new Vector3(bottomInScreen.x, bottomInScreen.y - 1f, bottomInViewport.z)) - bottomInWorld).normalized;
        float dot = Vector3.Dot(-Vector3.up, screenDownInWorld);
        float correction = Mathf.Sqrt(Mathf.Sqrt(1f - dot * dot * dot * dot * dot * dot));
        Debug.Log(correction);
        Vector2 bottomEdgeInScreen = Camera.main.WorldToScreenPoint(bottomInWorld + screenDownInWorld * bounds.extents.x * correction);
        if (bottomEdgeInScreen.y > centerInScreen.y) // sometimes the top of the piece is below its center on the screen
            bottomEdgeInScreen = Camera.main.WorldToScreenPoint(new Vector3(centerInWorld.x, centerInWorld.y + bounds.extents.y, centerInWorld.z));

        Vector2 cornerInScreen = new Vector2(leftEdgeInScreen.x, bottomEdgeInScreen.y);

        return new Rect(cornerInScreen, 2 * (centerInScreen - cornerInScreen));

    // Raycasting solution (blinks near the plane)
        Vector3 screenDownInWorld;
        Plane boardPlane = new Plane(Vector3.up, centerInWorld);
        float enter;
        if (Camera.main.transform.position.y > centerInWorld.y)
        {
            Ray r = Camera.main.ScreenPointToRay(new Vector2(centerInScreen.x, centerInScreen.y - 1f));
            bool success = boardPlane.Raycast(r, out enter);
            Debug.Assert(success);
            screenDownInWorld = r.GetPoint(enter) - centerInWorld;
        } else
        {
            Ray r = Camera.main.ScreenPointToRay(new Vector2(centerInScreen.x, centerInScreen.y + 1f));
            bool success = boardPlane.Raycast(r, out enter);
            Debug.Assert(success);
            screenDownInWorld = centerInWorld - r.GetPoint(enter);
        }
        Vector2 bottomEdgeInScreen = Camera.main.WorldToScreenPoint(new Vector3(centerInWorld.x, centerInWorld.y - bounds.extents.y, centerInWorld.z) + screenDownInWorld * (bounds.extents.x / screenDownInWorld.magnitude));
    }
    */
}
