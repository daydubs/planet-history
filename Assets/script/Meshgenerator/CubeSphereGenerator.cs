using UnityEngine;
using UnityEngine.Rendering;



[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereGenerator : MonoBehaviour
{
    [SerializeField, Range(2, 256)] private int resolution = 32;
    [SerializeField] private float radius = 5f;
    [SerializeField] private bool generateOnStart = true;

    private Mesh mesh;

    private void Start()
    {
        if (generateOnStart) Generate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && generateOnStart)
            Generate();
    }
#endif

    [ContextMenu("Generate CubeSphere")]
    public void Generate()
    {
        if (resolution < 2) resolution = 2;

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "CubeSphereMesh";
            mesh.indexFormat = IndexFormat.UInt32;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
            mesh.indexFormat = IndexFormat.UInt32;
        }

        // 6 faces
        Vector3[] faceNormals =
        {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        int vertsPerFace = resolution * resolution;
        int trisPerFace = (resolution - 1) * (resolution - 1) * 6;

        Vector3[] vertices = new Vector3[vertsPerFace * 6];
        Vector3[] normals = new Vector3[vertsPerFace * 6];
        Vector2[] uvs = new Vector2[vertsPerFace * 6];
        int[] triangles = new int[trisPerFace * 6];

        int vOffset = 0;
        int tOffset = 0;

        for (int f = 0; f < 6; f++)
        {
            BuildFace(
                faceNormals[f],
                ref vertices, ref normals, ref uvs, ref triangles,
                ref vOffset, ref tOffset
            );
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void BuildFace(
        Vector3 localUp,
        ref Vector3[] vertices,
        ref Vector3[] normals,
        ref Vector2[] uvs,
        ref int[] triangles,
        ref int vOffset,
        ref int tOffset)
    {
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        int faceStart = vOffset;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = faceStart + x + y * resolution;

                Vector2 percent = new Vector2(x, y) / (resolution - 1f);
                Vector3 pointOnCube =
                    localUp +
                    (percent.x - 0.5f) * 2f * axisA +
                    (percent.y - 0.5f) * 2f * axisB;

                Vector3 pointOnSphere = pointOnCube.normalized;
                vertices[i] = pointOnSphere * radius;
                normals[i] = pointOnSphere;

                // UV locale par face (utile pour debug)
                uvs[i] = percent;
            }
        }

        for (int y = 0; y < resolution - 1; y++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i = faceStart + x + y * resolution;

                triangles[tOffset++] = i;
                triangles[tOffset++] = i + resolution + 1;
                triangles[tOffset++] = i + resolution;

                triangles[tOffset++] = i;
                triangles[tOffset++] = i + 1;
                triangles[tOffset++] = i + resolution + 1;
            }
        }

        vOffset += resolution * resolution;
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.01f, newRadius);
        Generate();
    }
}