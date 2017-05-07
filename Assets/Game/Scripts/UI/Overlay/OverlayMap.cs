using UnityEngine;
using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class OverlayMap : MonoBehaviour
{
    private static List<Color32> _random_colors;

    public Dictionary<string, OverlayDescriptor> overlays;
    public Vector3 leftBottomCorner = new Vector3(-0.5f, -0.5f, 1f);

    private ColourMap colourMap;
    public ColourMap ColourMap
    {
        get { return colourMap; }
        set
        {
            colourMap = value;
            GenerateColorMap();
        }
    }

    public GameObject parentPanel;

    [Range(0,1)]
    public float transparency = 0.8f;
    public float updateInterval = 5f;
    private float elapsed = 0f;
    
    public int xPixelsPerTile = 20;
    public int yPixelsPerTile = 20;
    public int xSize = 10;
    public int ySize = 10;

    private Script script;

    public string currentOverlay;
    public Func<int, int, int> valueAt;

    public string xmlFileName = "overlay_prototypes.xml";
    public string LUAFileName = "overlay_functions.lua";

    private Texture2D colorMapTexture;
    private Vector3[] newVertices;
    private Vector3[] newNormals;
    private Vector2[] newUV;
    private int[] newTriangles;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private Color32[] colorMapArray;
    private bool initialized;
    private Texture2D texture;

    private GameObject colorMapView;
    private GameObject textView;

    private void Awake()
    {
        Bake();
    }

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        overlays = OverlayDescriptor.ReadPrototypes(xmlFileName);

        UserData.RegisterAssembly();
        string scriptFile = System.IO.Path.Combine(Application.streamingAssetsPath, System.IO.Path.Combine("Overlay", LUAFileName));
        string scriptTxt = System.IO.File.ReadAllText(scriptFile);
        
        script = new Script();
        script.DoString(scriptTxt);

        // Build GUI
        CreateGUI();

        SetSize(100, 100);
        SetOverlay("None");
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (currentOverlay != "None" && elapsed > updateInterval)
        {
            Bake();
            elapsed = 0f;
        }

        Vector2 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(valueAt != null)
        {
            textView.GetComponent<UnityEngine.UI.Text>().text = string.Format("[DEBUG] Currently over: {0}", valueAt((int)(point.x + 0.5f), (int)(point.y + 0.5f)));
        }
    }

    public void SetSize(int x, int y)
    {
        xSize = x;
        ySize = y;
        if (meshRenderer != null)
        {
            Init();
        }
    }

    private void Init()
    {
        GenerateMesh();
        GenerateColorMap();

        // Size in pixels of overlay texture and create texture
        int textureWidth = xSize * xPixelsPerTile;
        int textureHeight = ySize * yPixelsPerTile;
        texture = new Texture2D(textureWidth, textureHeight)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        // Set material
        Shader shader = Shader.Find("Transparent/Diffuse");
        Material mat = new Material(shader);
        meshRenderer.material = mat;
        if(meshRenderer == null || texture == null)
        {
            Debug.LogError("Material or renderer is null. Failing.");
        }

        meshRenderer.material.mainTexture = texture;
        initialized = true;
    }

    public void Bake()
    {
        if (initialized && valueAt != null)
            GenerateTexture();
    }
    
    private void GenerateColorMap()
    {
        colorMapArray = ColorMap(ColourMap, 255);

        int textureWidth = colorMapArray.Length * xPixelsPerTile;
        int textureHeight = yPixelsPerTile;
        colorMapTexture = new Texture2D(textureWidth, textureHeight);
        
        int n = 0;
        foreach (Color32 baseColor in colorMapArray)
        {
            for (int y = 0; y < yPixelsPerTile; y++)
            {
                for (int x = 0; x < xPixelsPerTile; x++)
                {
                    Color colorCopy = baseColor;
                    colorCopy.a = transparency;
                    // Add some noise to "prettify"
                    colorCopy.r += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorCopy.b += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorCopy.g += UnityEngine.Random.Range(-0.03f, 0.03f);
                    colorMapTexture.SetPixel(n * xPixelsPerTile + x, y, colorCopy);
                }
            }

            ++n;
        }
        colorMapTexture.Apply();
        colorMapView.GetComponent<UnityEngine.UI.Image>().material.mainTexture = colorMapTexture;
    }

    private void GenerateTexture()
    {
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                float v = valueAt(x, y);
                Debug.Assert(v >= 0 && v < 256);
                Graphics.CopyTexture(colorMapTexture, 0, 0, (int) v % 256 * xPixelsPerTile, 0, xPixelsPerTile, yPixelsPerTile, texture, 0, 0, x * xPixelsPerTile, y * yPixelsPerTile);
            }
        }
        texture.Apply(true);
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        if (meshFilter != null)
        {
            meshFilter.mesh = mesh;
        }

        int xSizeP = xSize + 1;
        int ySizeP = ySize + 1;

        newVertices = new Vector3[xSizeP * ySizeP];
        newNormals = new Vector3[xSizeP * ySizeP];
        newUV = new Vector2[xSizeP * ySizeP];
        newTriangles = new int[(xSizeP - 1) * (ySizeP - 1) * 6];
        
        for (int y = 0; y < ySizeP; y++)
        {
            for (int x = 0; x < xSizeP; x++)
            {
                newVertices[y * xSizeP + x] = new Vector3(x, y, 0) + leftBottomCorner;
                newNormals[x * ySizeP + y] = Vector3.up;
                newUV[y * xSizeP + x] = new Vector2((float)x / xSize, (float)y / ySize);
            }
        }

        int offset = 0;
        for (int y = 0; y < ySizeP - 1; y++)
        {
            for (int x = 0; x < xSizeP - 1; x++)
            {
                int index = y * xSizeP + x;
                newTriangles[offset + 0] = index;
                newTriangles[offset + 1] = index + xSizeP;
                newTriangles[offset + 2] = index + xSizeP + 1;

                newTriangles[offset + 3] = index;
                newTriangles[offset + 5] = index + 1;
                newTriangles[offset + 4] = index + xSizeP + 1;

                offset += 6;
            }
        }

        mesh.vertices = newVertices;
        mesh.uv = newUV;
        mesh.triangles = newTriangles;
        mesh.normals = newNormals;

    }

    private static void GenerateRandomColors(int size)
    {
        if (_random_colors == null)
        {
            _random_colors = new List<Color32>();
        }
        for (int i = _random_colors.Count; i < size; i++)
        {
            _random_colors.Add(UnityEngine.Random.ColorHSV());
        }
    }

    public static Color32[] ColorMap(ColourMap colorMap, int size = 256, byte alpha = 128)
    {
        Color32[] color32s = new Color32[size];
        Func<int, Color32> map;

        switch (colorMap)
        {
            default:
                map = arg01 =>
                {
                    Color32 c = new Color32(255, 255, 255, alpha);
                    switch (arg01)
                    {
                        case 64:
                            c.r = 0;
                            c.g = 255;
                            break;
                        case 128:
                            c.r = 0;
                            c.b = 255;
                            break;
                        case 192:
                            c.g = 255;
                            c.b = 0;
                            break;
                        default:
                            if (arg01 < 64)
                            {
                                c.r = 0;
                                c.g = (byte)(4 * arg01);
                            }
                            else if (arg01 < 128)
                            {
                                c.r = 0;
                                c.b = (byte)(256 + 4 * (64 - arg01));
                            }
                            else if (arg01 < 192)
                            {
                                c.r = (byte)(4 * (arg01 - 128));
                                c.b = 0;
                            }
                            else
                            {
                                c.g = (byte)(256 + 4 * (192 - arg01));
                                c.b = 0;
                            }
                            break;
                    }

                    return c;
                };
                break;
            case ColourMap.Random:
                GenerateRandomColors(size);
                map = arg01 => _random_colors[arg01];
                break;
        }

        for (int i = 0; i < size; i++)
        {
            color32s[i] = map(i);
        }
        return color32s;
    }

    public void SetOverlay(string name)
    {
        if(name == "None")
        {
            meshRenderer.enabled = false;
            currentOverlay = name;
            return;
        }

        if (overlays.ContainsKey(name))
        {
            meshRenderer.enabled = true;
            currentOverlay = name;
            OverlayDescriptor descr = overlays[name];
            object handle = script.Globals[descr.LuaFunctionName];
            if (handle == null)
            {
                Debug.LogError(string.Format("Couldn't find a function named '{0}' in '{1}'", descr.LuaFunctionName));
                return;
            }

            valueAt = (x, y) => 
            {
                if (WorldController.Instance == null) return 0;
                Tile tile = WorldController.Instance.GetTileAtWorldCoordinate(new Vector3(x, y, 0));
                return (int) script.Call(handle, tile, World.Current).ToScalar().CastToNumber();
            };

            ColourMap = descr.ColourMap;
            Bake();
        }
        else
        {
            Debug.LogError(string.Format("Overlay with name {0} not found in prototypes", name));
        }
    }

    private void CreateGUI()
    {
        UnityEngine.UI.Dropdown dropdown = parentPanel.GetComponentInChildren<UnityEngine.UI.Dropdown>();
        if(dropdown == null)
        {
            return;
        }

        textView = new GameObject();
        textView.AddComponent<UnityEngine.UI.Text>();
        textView.AddComponent<UnityEngine.UI.LayoutElement>();
        textView.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 30;
        textView.GetComponent<UnityEngine.UI.LayoutElement>().minWidth = 150;
        textView.transform.SetParent(parentPanel.transform);
        textView.GetComponent<UnityEngine.UI.Text>().text = "Currently selected:";
        textView.GetComponent<UnityEngine.UI.Text>().resizeTextForBestFit = true;
        textView.GetComponent<UnityEngine.UI.Text>().font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        colorMapView = new GameObject();
        colorMapView.AddComponent<UnityEngine.UI.Image>();
        colorMapView.transform.SetParent(parentPanel.transform);
        //colorMapView.AddComponent<UnityEngine.UI.Text>();
        colorMapView.AddComponent<UnityEngine.UI.LayoutElement>();
        colorMapView.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 30;
        colorMapView.GetComponent<UnityEngine.UI.LayoutElement>().minWidth = 150;
        Shader shader = Shader.Find("UI/Unlit/Transparent");
        colorMapView.GetComponent<UnityEngine.UI.Image>().material = new Material(shader);

        List <string> options = new List<string> { "None" };
        options.AddRange(overlays.Keys);

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(idx => SetOverlay(dropdown.captionText.text));
    }
}
