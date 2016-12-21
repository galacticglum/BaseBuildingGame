using UnityEngine;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
	public static WorldController Instance { get; protected set; }
	public World World { get; protected set; }

    private static bool loadWorld;

	// Use this for initialization
    private void OnEnable ()
    {
		if(Instance != null)
        {
			Debug.LogError("WorldController::OnEnable: There should never be two world controllers.");
		}
		Instance = this;

		if(loadWorld)
        {
			loadWorld = false;
			CreateWorldFromSaveFile();
		}
		else
        {
			GenerateEmpty();
		}
	}

    private void Update()
    {
		// TODO: Add pause/unpause, speed controls, etc...
		World.Update(Time.deltaTime);

	}
		
	public Tile GetTileAtWorldCoordinate(Vector3 coordinate)
    {
		int x = Mathf.FloorToInt(coordinate.x + 0.5f);
		int y = Mathf.FloorToInt(coordinate.y + 0.5f);
		
		return World.GetTileAt(x, y);
	}

	public void New()
    {
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
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

        // Center the Camera
        Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z);
	}

    private void CreateWorldFromSaveFile()
    {
		XmlSerializer serializer = new XmlSerializer(typeof(World));
		TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame00"));
		World = (World)serializer.Deserialize(reader);
		reader.Close();

		// Center the Camera
		Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z );

	}

}
