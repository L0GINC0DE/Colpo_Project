using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Size")]
    public float width = 6f;
    public float height = 3f;

    [Header("Curve")]
    public float horizontalCurveAngle = 90f;
    public float verticalCurveAngle = 35f;

    [Header("Segments")]
    public int horizontalSegments = 64;
    public int verticalSegments = 24;

    [Header("Direction")]
    public bool concave = true;

    void Awake()
    {
        Generate();
    }

    void OnValidate()
    {
        horizontalSegments = Mathf.Max(1, horizontalSegments);
        verticalSegments = Mathf.Max(1, verticalSegments);
        horizontalCurveAngle = Mathf.Max(0.01f, horizontalCurveAngle);
        verticalCurveAngle = Mathf.Max(0.01f, verticalCurveAngle);

        Generate();
    }

    public void Generate()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Double Curved Screen Mesh";

        int xCount = horizontalSegments + 1;
        int yCount = verticalSegments + 1;

        Vector3[] vertices = new Vector3[xCount * yCount];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[horizontalSegments * verticalSegments * 6];

        float hAngleRad = horizontalCurveAngle * Mathf.Deg2Rad;
        float vAngleRad = verticalCurveAngle * Mathf.Deg2Rad;

        float hRadius = width / hAngleRad;
        float vRadius = height / vAngleRad;

        float hStart = -hAngleRad * 0.5f;
        float vStart = -vAngleRad * 0.5f;

        int v = 0;

        for (int y = 0; y < yCount; y++)
        {
            float y01 = y / (float)verticalSegments;
            float vAngle = vStart + vAngleRad * y01;

            for (int x = 0; x < xCount; x++)
            {
                float x01 = x / (float)horizontalSegments;
                float hAngle = hStart + hAngleRad * x01;

                float xPos = Mathf.Sin(hAngle) * hRadius;
                float yPos = Mathf.Sin(vAngle) * vRadius;

                float zFromX = Mathf.Cos(hAngle) * hRadius - hRadius;
                float zFromY = Mathf.Cos(vAngle) * vRadius - vRadius;

                float zPos = zFromX + zFromY;

                if (!concave)
                    zPos = -zPos;

                vertices[v] = new Vector3(xPos, yPos, zPos);
                uvs[v] = new Vector2(x01, y01);
                v++;
            }
        }

        int t = 0;

        for (int y = 0; y < verticalSegments; y++)
        {
            for (int x = 0; x < horizontalSegments; x++)
            {
                int i = y * xCount + x;

                triangles[t++] = i;
                triangles[t++] = i + xCount;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + xCount;
                triangles[t++] = i + xCount + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
