using System.Collections;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    [SerializeField]
    private float yOffset = 0f;
    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private PromoteMenu promoteMenu;

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
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        if (value)
        {
            ghost = Instantiate(this);
            Color oldColor = ghost.renderer.material.color;
            ghost.renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, ghostAlpha);

            Vector3 start = Vector3.up * yOffset;
            moveCoroutine = StartCoroutine(MoveRoutine(start, start + height * GameManager.tileUp));
        } else
        {
            Destroy(ghost.gameObject);
            ghost = null;

            Vector3 start = Vector3.up * yOffset;
            moveCoroutine = StartCoroutine(MoveRoutine(start + height * GameManager.tileUp, start));
        }
    }

    public void RequestPromotion()
    {
        StartCoroutine(WaitForIdle());
    }

    // Called by UI buttons
    public void Promote(int type)
    {
        GameManager.Promote((Board.PieceType)type);
        promoteMenu.enabled = false;
    }

    private Coroutine moveCoroutine;
    private IEnumerator MoveRoutine(Vector3 start, Vector3 end)
    {
        float t = 0f;
        while (t < 1f)
        {
            renderer.transform.localPosition = Vector3.Lerp(start, end, t);
            yield return null;

            t += Time.deltaTime * speed;
        }

        renderer.transform.localPosition = end;

        moveCoroutine = null;
    }

    private IEnumerator WaitForIdle()
    {
        while (moveCoroutine != null)
            yield return new WaitForSeconds(0.1f);

        promoteMenu.enabled = true;
    }
}