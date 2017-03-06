using UnityEngine;
using System.Collections.Generic;

public class PathVisualizer : MonoBehaviour 
{
    public static PathVisualizer Current { get; private set; }

    private static Material lineMaterial;
    private Dictionary<string, List<Tile>> vertices;
    private Color colour;

    private void Awake()
    {
        vertices = new Dictionary<string, List<Tile>>();
        colour = Color.red;
    }

    private void OnEnable()
    {
        Current = this;
    }

    private void OnRenderObject()
    {
        CreateLineMaterial();

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);
        GL.Color(colour);

        foreach (string entry in vertices.Keys)
        {
            for (int i = 0; i < vertices[entry].Count; i++)
            {
                if (i != 0)
                {
                    GL.Vertex3(vertices[entry][i - 1].X, vertices[entry][i - 1].Y, 0);
                }
                else
                {
                    GL.Vertex3(vertices[entry][i].X, vertices[entry][i].Y, 0);
                }

                GL.Vertex3(vertices[entry][i].X, vertices[entry][i].Y, 0);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    public void ModifyPath(string characterName, List<Tile> vertices)
    {
        if (this.vertices.ContainsKey(characterName))
        {
            this.vertices[characterName] = vertices;
            return;
        }

        this.vertices.Add(characterName, vertices);
    }

    public void RemovePath(string characterName)
    {
        if (vertices.ContainsKey(characterName))
        {
            vertices.Remove(characterName);
        }
    }

    private static void CreateLineMaterial()
    {
        if (lineMaterial) return;

        // Unity has a built-in shader that is useful for drawing colored simple-things.
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        // Turn on alpha blending
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Turn backface culling off
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        // Turn off depth writes
        lineMaterial.SetInt("_ZWrite", 0);
    }
}
