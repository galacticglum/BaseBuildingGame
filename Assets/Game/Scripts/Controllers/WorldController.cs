using UnityEngine;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.UI;

public class WorldController : MonoBehaviour
{
	public static WorldController Instance { get; protected set; }
	public World World { get; protected set; }

    private static string loadWorldFromFile;

    private bool isPaused;
    public bool IsPaused
    {
        get { return isPaused || IsModal; }
        set { isPaused = value; }
    }

    public bool IsModal { get; set; }

	// Use this for initialization
    private void OnEnable ()
    {
		if(Instance != null)
        {
			Debug.LogError("WorldController::OnEnable: There should never be two world controllers.");
		}
		Instance = this;

		if(!string.IsNullOrEmpty(loadWorldFromFile))
        {
			CreateWorldFromSaveFile();
            loadWorldFromFile = null;
        }
        else
        {
			GenerateEmpty();
		}
	}

    private void Update()
    {
		// TODO: Add pause/unpause, speed controls, etc...
        if (!IsPaused)
        {
            World.Update(Time.deltaTime);
        }

	}
		
	public Tile GetTileAtWorldCoordinates(Vector3 coordinate)
    {
		int x = Mathf.FloorToInt(coordinate.x + 0.5f);
		int y = Mathf.FloorToInt(coordinate.y + 0.5f);
		
		return World.GetTileAt(x, y);
	}

	public void New()
    {
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
	}

	public string Save()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, World);
		writer.Close();

        return writer.ToString();
    }

	public void Load(string filePath)
    {
		loadWorldFromFile = filePath;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

    private void GenerateEmpty()
    {
		World = new World(100, 100);

        // Center the Camera
        Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z);
	}

    private void CreateWorldFromSaveFile()
    {
		XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(File.ReadAllText(loadWorldFromFile));

		World = (World)serializer.Deserialize(reader);
		reader.Close();

		// Center the Camera
		Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z );

	}

}
