using System.Collections;
using UnityEngine;

// Attached to each piece prefab as a component
public class GamePiece : MonoBehaviour
{
    private Color startColor;

    private GamePiece ghost;

    private const float height = 2.3f; // Height of picked up pieces, in board tiles
    private const float speed = 5f; // Reciprocal of duration in seconds

    void Awake()
    {
        startColor = GetComponent<Renderer>().material.color;
    }

    public void Highlight(bool value)
    {
        Renderer renderer = GetComponent<Renderer>();
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
            StartCoroutine("PutDown");
        }
    }

    private IEnumerator PickUp()
    {
        ghost = Instantiate(this, transform.parent.position, transform.parent.rotation);
        Renderer renderer = ghost.GetComponent<Renderer>();
        Color oldColor = renderer.material.color;
        renderer.material.color = new Color(oldColor.r, oldColor.g, oldColor.b, 0.5f);

        foreach (object nil in Lerp(Vector3.zero, height * GameManager.tileUp))
        {
            yield return null;
        }
    }

    private IEnumerator PutDown()
    {
        Destroy(ghost.gameObject);
        ghost = null;

        foreach (object nil in Lerp(height * GameManager.tileUp, Vector3.zero))
        {
            yield return null;
        }
    }

    private IEnumerable Lerp(Vector3 start, Vector3 end)
    {
        transform.localPosition = start;
        yield return null;

        float t = Time.deltaTime * speed;
        while (t < 1f)
        {
            transform.localPosition = Vector3.Lerp(start, end, t);
            yield return null;

            t += Time.deltaTime * speed;
        }

        transform.localPosition = end;
    }
}