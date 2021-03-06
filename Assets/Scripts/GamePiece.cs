﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public PieceColor color { get { return pieceColor; } }

    private Vector3 grounded, raised;
    private GamePiece ghost;
    private Material startMaterial, outlineMaterial;

    private Coroutine ascendCoroutine, descendCoroutine, sidewaysCoroutine;

    public bool isMoving { get { return ascendCoroutine != null || descendCoroutine != null || sidewaysCoroutine != null; } }

    private const float height = 2.3f; // Height of picked up pieces, in board tiles
    private const float speed = 8f; // Reciprocal of duration in seconds

    void Awake()
    {
        startMaterial = renderer.material; // this automatically instantiates a new copy of renderer.material
        outlineMaterial = Instantiate(GameManager.outlineMaterial);
        outlineMaterial.SetInt("_StencilMask", 1);
        //outlineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        grounded = new Vector3(0f, yOffset, 0f);
        raised = grounded + height * GameManager.tileUp;
    }

    void OnDestroy()
    {
        Destroy(startMaterial);
        Destroy(outlineMaterial);
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            renderer.materials = new Material[] { startMaterial, outlineMaterial }; // this DOES NOT instantiate a new copy of outlineMaterial because it's already been instantiated in Awake()
            startMaterial.SetInt("_StencilMask", 1);
        } else
        {
            renderer.materials = new Material[] { startMaterial };
            startMaterial.SetInt("_StencilMask", 0);
        }
    }

    public void Select(bool value)
    {
        StopAllCoroutines();
        descendCoroutine = null;
        sidewaysCoroutine = null;

        if (value)
        {
            ghost = Instantiate(this);
            ghost.renderer.material = GameManager.ghostMaterial; // this DOES instantiate a new copy of ghostMaterial

            ascendCoroutine = StartCoroutine(AscendRoutine());
        } else
        {
            Destroy(ghost.renderer.material);
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
        while (isMoving)
            yield return new WaitForSeconds(GameManager.waitInterval);

        renderer.GetComponent<PromoteMenu>().enabled = true;
    }

    // Called by UI buttons
    public void Promote(int type)
    {
        GameManager.Promote((Board.PieceType)type);
        renderer.GetComponent<PromoteMenu>().enabled = false;
    }

    // Calculates angle-weighted vertex normals for use in the piece outline shader. Only needs to be called once for each type of piece
    // TODO: save the modified meshes into the prefabs themselves (and mark each mesh as not readable)
    public void SmoothMeshNormals()
    {
        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

        Debug.Assert(mesh.subMeshCount == 1); // as long as this is true, the label of each vertex in the triangles array should match its index in the following arrays
        Vector3[] vertices = mesh.vertices;
        int vertexCount = mesh.vertexCount;
        Debug.Assert(vertexCount == vertices.Count()); // sanity check
        int[] triangles = mesh.triangles; // {1a, 1b, 1c, 2a, 2b, 2c, etc.}

        Vector3[] results = new Vector3[vertexCount];
        Debug.Assert(results[0] == Vector3.zero);
        int stopCondition = mesh.triangles.Count();
        for (int i = 0; i < stopCondition; i += 3)
        {
            int i1 = triangles[i], i2 = triangles[i + 1], i3 = triangles[i + 2];
            Vector3 v1 = vertices[i1], v2 = vertices[i2], v3 = vertices[i3];

            Vector3 e1 = v2 - v1, e2 = v3 - v2, e3 = v1 - v3;

            Vector3 n = Vector3.Cross(e1, -e3).normalized; // don't normalize -> magnitude proportional to area (area and angle weighted normals)

            // TODO: save time by calculating norms and products manually
            float a1 = Vector3.Angle(e1, -e3), a2 = Vector3.Angle(e2, -e1);
            results[i1] += n * a1;
            results[i2] += n * a2;
            results[i3] += n * (180f - a1 - a2);
        }

        // build map of duplicates (each set of duplicates is a cycle -> getting the set of duplicates amounts to iterating around the cycle)
        // also keep a list of the first index of each cycle/set, the "representative"
        int[] map = new int[vertexCount];
        List<int> representatives = new List<int>();
        for (int i = 0; i < vertexCount; ++i)
        {
            bool match = false;
            foreach (int rep in representatives)
            {
                if (vertices[i] == vertices[rep])
                {
                    map[i] = map[rep];
                    map[rep] = i;
                    match = true;
                }
            }

            // no matches
            if (!match)
            {
                map[i] = i;
                representatives.Add(i);
            }
        }

        // assign new normals as vertex colors
        Color[] colors = new Color[vertexCount];
        foreach (int rep in representatives)
        {
            int i = rep;
            Vector3 result = Vector3.zero;
            do
            {
                result += results[i];
                i = map[i];
            } while (i != rep);

            Vector3 v = result.normalized;
            Color c = new Color(v.x, v.y, v.z);
            do
            {
                colors[i] = c;
                i = map[i];
            } while (i != rep);
        }
        mesh.colors = colors;
    }
}