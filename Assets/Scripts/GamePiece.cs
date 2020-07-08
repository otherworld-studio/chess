using System.Collections;
using UnityEngine;

using PieceColor = Board.PieceColor;

public class GamePiece : MonoBehaviour
{
    [SerializeField]
    private PieceColor pieceColor;
    [SerializeField]
    private float yOffset = 0f;
    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private PromoteMenu promoteMenu;

    public PieceColor color { get { return pieceColor; } }

    private Color startColor;
    private GamePiece ghost;

    private const float height = 2.3f; // Height of picked up pieces, in board tiles
    private const float speed = 10f; // Reciprocal of duration in seconds
    private const float ghostAlpha = 0.25f;

    void Awake()
    {
        renderer.material.SetFloat("_ZWrite", 1);
        startColor = renderer.material.color;
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            renderer.material.color = Color.yellow; // TODO: make your own shader
        } else
        {
            renderer.material.color = startColor;
        }
    }

    public void Select(bool value)
    {
        StopAllCoroutines();
        globalTranslationCoroutine = null;
        moveCoroutine = null;

        Vector3 grounded = new Vector3(0f, yOffset, 0f);
        Vector3 raised = grounded + height * GameManager.tileUp;
        if (value)
        {
            ghost = Instantiate(this);
            Color oldColor = ghost.renderer.material.color;
            ghost.renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, ghostAlpha);

            localTranslationCoroutine = StartCoroutine(LocalTranslationRoutine(grounded, raised));
        } else
        {
            Destroy(ghost.gameObject);
            ghost = null;

            localTranslationCoroutine = StartCoroutine(LocalTranslationRoutine(raised, grounded));
        }
    }

    public void RequestPromotion()
    {
        StartCoroutine(RequestPromotionRoutine());
    }

    private IEnumerator RequestPromotionRoutine()
    {
        while (localTranslationCoroutine != null || globalTranslationCoroutine != null || moveCoroutine != null)
            yield return new WaitForSeconds(GameManager.waitInterval);

        promoteMenu.enabled = true;
    }

    // Called by UI buttons
    public void Promote(int type)
    {
        GameManager.Promote((Board.PieceType)type);
        promoteMenu.enabled = false;
    }

    private Coroutine localTranslationCoroutine;
    private IEnumerator LocalTranslationRoutine(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < 1f)
        {
            renderer.transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;

            t += Time.deltaTime * speed;
        }

        renderer.transform.localPosition = to;

        localTranslationCoroutine = null;
    }

    private Coroutine globalTranslationCoroutine;
    private IEnumerator GlobalTranslationRoutine(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < 1f)
        {
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;

            t += Time.deltaTime * speed;
        }

        transform.position = to;

        globalTranslationCoroutine = null;
    }

    public void Move(Vector3 from, Vector3 to)
    {
        StopAllCoroutines();
        moveCoroutine = StartCoroutine(MoveRoutine(from, to));
    }

    private Coroutine moveCoroutine;
    private IEnumerator MoveRoutine(Vector3 from, Vector3 to)
    {
        Vector3 grounded = new Vector3(0f, yOffset, 0f);
        Vector3 raised = grounded + height * GameManager.tileUp;

        localTranslationCoroutine = StartCoroutine(LocalTranslationRoutine(grounded, raised));
        yield return localTranslationCoroutine;

        globalTranslationCoroutine = StartCoroutine(GlobalTranslationRoutine(from, to));
        yield return globalTranslationCoroutine;

        localTranslationCoroutine = StartCoroutine(LocalTranslationRoutine(raised, grounded));
        yield return localTranslationCoroutine;

        moveCoroutine = null;
    }
}