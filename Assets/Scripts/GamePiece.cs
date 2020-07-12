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
    private Vector3 grounded, raised;
    private GamePiece ghost;

    private Coroutine ascendCoroutine;
    public bool Ascending() { return ascendCoroutine != null; }
    private Coroutine descendCoroutine;
    public bool Descending() { return descendCoroutine != null; }
    private Coroutine sidewaysCoroutine;
    public bool MovingSideways() { return sidewaysCoroutine != null; }

    private const float height = 2.3f; // Height of picked up pieces, in board tiles
    private const float speed = 10f; // Reciprocal of duration in seconds
    private const float ghostAlpha = 0.25f;

    void Awake()
    {
        renderer.material.SetFloat("_ZWrite", 1);
        startColor = renderer.material.color;

        grounded = new Vector3(0f, yOffset, 0f);
        raised = grounded + height * GameManager.tileUp;
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            //renderer.material.shader = GameManager.pieceShader; TODO
            renderer.material.color = Color.yellow;
        } else
        {
            renderer.material.color = startColor;
        }
    }

    public void Select(bool value)
    {
        StopAllCoroutines();
        ascendCoroutine = null;
        descendCoroutine = null;
        sidewaysCoroutine = null;

        if (value)
        {
            ghost = Instantiate(this);
            Color oldColor = ghost.renderer.material.color;
            ghost.renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, ghostAlpha);

            ascendCoroutine = StartCoroutine(AscendRoutine());
        } else
        {
            Destroy(ghost.gameObject);
            ghost = null;

            descendCoroutine = StartCoroutine(DescendRoutine());
        }
    }
    
    private IEnumerator AscendRoutine()
    {
        yield return LocalTranslationRoutine(grounded, raised);

        ascendCoroutine = null;
    }
    
    private IEnumerator DescendRoutine()
    {
        yield return LocalTranslationRoutine(raised, grounded);

        descendCoroutine = null;
    }

    public void Move(Vector3 from, Vector3 to)
    {
        StopAllCoroutines();
        descendCoroutine = null;
        sidewaysCoroutine = null;
        ascendCoroutine = StartCoroutine(MoveRoutine(from, to));
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to)
    {
        yield return LocalTranslationRoutine(grounded, raised);

        sidewaysCoroutine = ascendCoroutine;
        ascendCoroutine = null;

        yield return GlobalTranslationRoutine(from, to);

        descendCoroutine = sidewaysCoroutine;
        sidewaysCoroutine = null;

        yield return LocalTranslationRoutine(raised, grounded);

        descendCoroutine = null;
    }

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
    }

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
    }

    public void RequestPromotion()
    {
        StartCoroutine(RequestPromotionRoutine());
    }

    private IEnumerator RequestPromotionRoutine()
    {
        while (Ascending() || Descending() || MovingSideways())
            yield return new WaitForSeconds(GameManager.waitInterval);

        promoteMenu.enabled = true;
    }

    // Called by UI buttons
    public void Promote(int type)
    {
        GameManager.Promote((Board.PieceType)type);
        promoteMenu.enabled = false;
    }
}