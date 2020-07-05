using System.Collections;
using UnityEngine;

using PieceType = Board.PieceType;

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
    private bool inMotion;

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
        if (value)
        {
            ghost = Instantiate(this);
            Color oldColor = ghost.renderer.material.color;
            ghost.renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, ghostAlpha);

            Vector3 start = Vector3.up * yOffset;
            StartCoroutine(Move(start, start + height * GameManager.tileUp));
        } else
        {
            Destroy(ghost.gameObject);
            ghost = null;

            Vector3 start = Vector3.up * yOffset;
            StartCoroutine(Move(start + height * GameManager.tileUp, start));
        }
    }

    public void RequestPromotion()
    {
        StartCoroutine(WaitForIdle());
    }

    // Called by UI buttons
    public void Promote(int type)
    {
        GameManager.Promote((PieceType)type);
        promoteMenu.enabled = false;
    }

    private IEnumerator Move(Vector3 start, Vector3 end)
    {
        inMotion = true;
        renderer.transform.localPosition = start;
        yield return null;

        float t = Time.deltaTime * speed;
        while (t < 1f)
        {
            renderer.transform.localPosition = Vector3.Lerp(start, end, t);
            yield return null;

            t += Time.deltaTime * speed;
        }

        renderer.transform.localPosition = end;
        inMotion = false;
    }

    private IEnumerator WaitForIdle()
    {
        while (inMotion) yield return new WaitForSeconds(0.1f);

        promoteMenu.enabled = true;
    }
}