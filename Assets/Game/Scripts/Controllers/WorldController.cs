
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }
    public World World { get; protected set; }

    private static bool loadWorld = false;

    private void OnEnable()
    {       
        if (Instance != null)
        {
            Debug.LogError("There should never be two World controllers.");
        }
        Instance = this;

        if(loadWorld)
        {
            loadWorld = false;
            GenerateFromSaveFile();
        }    
        else
        {
            GenerateEmpty();
        }
    }

    private void Update()
    {
        // TODO: Add pause/unpause, speed controls, etc; custom Time.deltaTime
        World.Update(Time.deltaTime);
    }

    private void CentreCamera()
    {
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Width / 2, Camera.main.transform.position.z);
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates
    /// </summary>
    /// <returns>The tile at World coordinate.</returns>
    /// <param name="coordinate">Unity World-Space coordinates.</param>
    public Tile GetTileAtWorldCoordinate(Vector2 coordinate)
    {
        int x = Mathf.FloorToInt(coordinate.x + 0.5f);
        int y = Mathf.FloorToInt(coordinate.y + 0.5f);

        return World.GetTileAt(x, y);
    } 

    public void New()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Save()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, World);
        writer.Close();

        PlayerPrefs.SetString("SaveGame00", writer.ToString());
    }

    public void Load()
    {
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GenerateEmpty()
    {
        World = new World(100, 100);
        CentreCamera();
    }

    private void GenerateFromSaveFile()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame00"));
        World = (World)serializer.Deserialize(reader);
        reader.Close();

        CentreCamera();
    }
}
