using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

public class TerrainToMeshWindow : EditorWindow
{
    Terrain terrain;
    TerrainData data;
    public enum ExportFormat
    {
        OBJ,
        FBX
    }

    ExportFormat exportFormat;
    float polygonRatio = 100f;

    [MenuItem("Tools/Terrain To Mesh")]
    static void Open()
    {
        GetWindow<TerrainToMeshWindow>();
    }

    bool showNoTerrainWarning;

    void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        exportFormat = (ExportFormat)EditorGUILayout.EnumPopup("Export Format", exportFormat);

        polygonRatio = EditorGUILayout.Slider("Polygon Ratio (%)", polygonRatio, 1f, 100f);

        if (terrain != null && terrain.terrainData != null)
        {
            int fullRes = terrain.terrainData.heightmapResolution;
            int meshRes = GetMeshResolution(fullRes);
            long tris = (long)(meshRes - 1) * (meshRes - 1) * 2;
            EditorGUILayout.HelpBox("Mesh: " + meshRes + " x " + meshRes + " vertices\n" +
            "Triangles: " + tris.ToString("N0"),
            MessageType.Info);
        }

        if (showNoTerrainWarning && terrain == null)
        {
            EditorGUILayout.HelpBox("No terrain selected", MessageType.Warning);
        }

        if (GUILayout.Button("Convert"))
        {
            showNoTerrainWarning = terrain == null;
            if (terrain != null)
            {
                ConvertToMesh();
            }
        }
    }

    int GetMeshResolution(int fullRes)
    {
        float ratio = polygonRatio / 100f;
        int meshRes = Mathf.RoundToInt((fullRes - 1) * Mathf.Sqrt(ratio)) + 1;
        return Mathf.Clamp(meshRes, 2, fullRes);
    }

    private void ConvertToMesh()
    {
        Debug.Log("Terrain : " + terrain.name);
        data = terrain.terrainData;

        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);
        Vector3 size = data.size;

        float spacingX = size.x / (res - 1);
        float spacingZ = size.z / (res - 1);

        int meshRes = GetMeshResolution(res);
        float meshSpacingX = size.x / (meshRes - 1);
        float meshSpacingZ = size.z / (meshRes - 1);

        Debug.Log("Heightmap res : " + res + " | Mesh res : " + meshRes +
                  " | Triangles : " + ((meshRes - 1) * (meshRes - 1) * 2));

        int[] triangles = new int[(meshRes - 1) * (meshRes - 1) * 6];
        int t = 0;

        Vector3[] vertices = new Vector3[meshRes * meshRes];
        for (int z = 0; z < meshRes; z++)
        {
            for (int x = 0; x < meshRes; x++)
            {
                int index = z * meshRes + x;
                float u = x / (float)(meshRes - 1);
                float v = z / (float)(meshRes - 1);
                float h = data.GetInterpolatedHeight(u, v);
                vertices[index] = new Vector3(x * meshSpacingX, h, z * meshSpacingZ);
            }
        }

        for (int z = 0; z < meshRes - 1; z++)
        {
            for (int x = 0; x < meshRes - 1; x++)
            {
                int a = z * meshRes + x;
                int b = a + 1;
                int c = a + meshRes;
                int d = c + 1;

                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = b;

                triangles[t++] = b;
                triangles[t++] = c;
                triangles[t++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        string path = Application.dataPath + "/" + data.name;
        if (exportFormat == ExportFormat.OBJ)
        {
            ExportToObj(mesh, path + ".obj");
        }
        else if (exportFormat == ExportFormat.FBX)
        {
            ExportToFbx(mesh, path);
        }

        GenerateNormalMap(heights, res, spacingX, spacingZ, size, path + "_NormalMap.png");
    }

    private void ExportToFbx(Mesh mesh, string path)
    {
        GameObject gO = new GameObject(data.name + "_Mesh");

        gO.AddComponent<MeshFilter>().sharedMesh = mesh;
        gO.transform.position = terrain.transform.position;
        gO.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        ModelExporter.ExportObject(path, gO);
        //DestroyImmediate(gO);
    }

    private void ExportToObj(Mesh mesh, string path)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        sb.AppendLine("# Terrain export");
        sb.AppendLine("o " + data.name);

        foreach (Vector3 v in vertices)
        {
            sb.Append("v ")
              .Append((-v.x).ToString("F6", inv)).Append(' ')
              .Append(v.y.ToString("F6", inv)).Append(' ')
              .Append(v.z.ToString("F6", inv)).Append('\n');
        }

        foreach (Vector3 n in normals)
        {
            sb.Append("vn ")
              .Append((-n.x).ToString("F6", inv)).Append(' ')
              .Append(n.y.ToString("F6", inv)).Append(' ')
              .Append(n.z.ToString("F6", inv)).Append('\n');
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i] + 1;
            int b = triangles[i + 2] + 1;
            int c = triangles[i + 1] + 1;
            sb.Append("f ")
              .Append(a).Append("//").Append(a).Append(' ')
              .Append(b).Append("//").Append(b).Append(' ')
              .Append(c).Append("//").Append(c).Append('\n');
        }

        System.IO.File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
    }

    private void GenerateNormalMap(float[,] heights, int res, float spacingX, float spacingZ, Vector3 size, string path)
    {
        Texture2D texture = new Texture2D(res, res, TextureFormat.RGB24, false);

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int xLeft = Mathf.Max(x - 1, 0);
                int xRight = Mathf.Min(x + 1, res - 1);
                int zDown = Mathf.Max(z - 1, 0);
                int zUp = Mathf.Min(z + 1, res - 1);

                float hLeft = heights[z, xLeft] * size.y;
                float hRight = heights[z, xRight] * size.y;
                float hDown = heights[zDown, x] * size.y;
                float hUp = heights[zUp, x] * size.y;

                float slopeX = (hLeft - hRight) / (2 * spacingX);
                float slopeZ = (hDown - hUp) / (2 * spacingZ);

                Vector3 normal = new Vector3(slopeX, 1, slopeZ);
                normal.Normalize();

                float r = normal.x * 0.5f + 0.5f;
                float g = normal.z * 0.5f + 0.5f;
                float b = normal.y * 0.5f + 0.5f;

                texture.SetPixel(x, z, new Color(r, g, b));
            }
        }

        texture.Apply();

        byte[] png = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, png);
    }
}