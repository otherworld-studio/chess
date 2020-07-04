using System.Collections;
using UnityEngine;

using PieceType = Board.PieceType;

// Attached to each piece prefab as a component
public class GamePiece : MonoBehaviour
{
    [SerializeField]
    private float yOffset = 0f;
    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private GameObject promoteMenu;

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
        if (value)
        {
            StartCoroutine("PickUp");
        } else
        {
            StartCoroutine("PutDown"); // TODO: only activate promoteMenu when coroutine is finished
        }
    }

    private IEnumerator PickUp()
    {
        ghost = Instantiate(this);
        Color oldColor = ghost.renderer.material.color;
        ghost.renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, ghostAlpha);

        Vector3 start = Vector3.up * yOffset;
        foreach (object nil in MovementCoroutine(start, start + height * GameManager.tileUp))
        {
            yield return null;
        }
    }

    private IEnumerator PutDown()
    {
        Destroy(ghost.gameObject);
        ghost = null;

        Vector3 start = Vector3.up * yOffset;
        foreach (object nil in MovementCoroutine(start + height * GameManager.tileUp, start))
        {
            yield return null;
        }
    }

    private IEnumerable MovementCoroutine(Vector3 start, Vector3 end)
    {
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
    }

    public void RequestPromotion()
    {
        Debug.Assert(promoteMenu != null);
        promoteMenu.SetActive(true);
    }

    public void Promote(int type)
    {
        promoteMenu.SetActive(false);
        GameManager.Promote((PieceType)type);
    }
}