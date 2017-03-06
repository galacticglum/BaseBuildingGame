using UnityEngine;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; private set; }

    public World World { get; private set; }
    public string FileSaveBasePath { get { return Path.Combine(Application.persistentDataPath, "Saves"); } }

    public bool IsModal;
    private bool isPaused;
    public bool IsPaused
    {
        get { return isPaused || IsModal; }
        set { isPaused = value; }
    }

    public ConstructionController ConstructionController { get; private set; }
    public MouseController MouseController { get; private set; }
    public KeyboardController KeyboardController { get; private set; }
    public CameraController CameraController { get; private set; }
    public InventoryDebugController InventoryDebugController { get; private set; }
    public ModManager ModManager { get; private set; }

    private AudioController audioController;
    private TileGraphicController tileGraphicController;
    private CharacterGraphicController characterGraphicController;
    private JobGraphicController jobGraphicController;
    private InventoryGraphicController inventoryGraphicController;
    private FurnitureGraphicController furnitureGraphicController;

    private float timeScale = 1f;
    public float TimeScale
    {
        get { return timeScale; }
        set { timeScale = value; }
    }

    private static string loadWorldFromFile;

    [SerializeField]
    private bool developmentMode;
    public bool DevelopmentMode
    {
        get { return developmentMode; }
        set { developmentMode = value; }
    }

    [SerializeField]
    private GameObject inventoryUI;
    [SerializeField]
    private GameObject circleCursorPrefab;

    // Use this for initialization
    private void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }

        Instance = this;
        ModManager = new ModManager(Path.Combine(Application.streamingAssetsPath, "Data"));

        if (loadWorldFromFile != null)
        {
            CreateWorldFromSaveFile();
            loadWorldFromFile = null;
        }
        else
        {
            CreateEmptyWorld();
        }

        audioController = new AudioController();
    }

    private void Start()
    {
        GameObject pathVisualizer = new GameObject("VisualPath", typeof(PathVisualizer));

        tileGraphicController = new TileGraphicController();
        characterGraphicController = new CharacterGraphicController();
        furnitureGraphicController = new FurnitureGraphicController();
        jobGraphicController = new JobGraphicController(furnitureGraphicController);
        inventoryGraphicController = new InventoryGraphicController(inventoryUI);
        ConstructionController = new ConstructionController();

        if(GameSettings.GetAsBoolean("DevTools_enabled", false))
        {
            InventoryDebugController = new InventoryDebugController();
        }

        MouseController = new MouseController(ConstructionController, furnitureGraphicController, circleCursorPrefab);
        KeyboardController = new KeyboardController(ConstructionController, Instance);
        CameraController = new CameraController();

        GameObject controllers = GameObject.Find("Controllers");
        Instantiate(Resources.Load("UIController"), controllers.transform);

        GameObject Canvas = GameObject.Find("Canvas");
        GameObject obj = Instantiate(Resources.Load("UI/ContextMenu"),Canvas.transform.position, Canvas.transform.rotation, Canvas.transform) as GameObject;
        if (obj != null)
        {
            obj.name = "ContextMenu";
        }
    }

    private void Update()
    {
        MouseController.Update(IsModal);
        KeyboardController.Update(IsModal);
        CameraController.Update(IsModal);

        if (IsPaused == false)
        {
            World.Update(Time.deltaTime * timeScale);
        }

        audioController.Update(Time.deltaTime);
    }

    public Tile GetTileAtWorldCoordinate(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return World.GetTileAt(x, y);
    }

    public void CreateWorld()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadWorld(string fileName)
    {
        loadWorldFromFile = fileName;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CreateEmptyWorld()
    {
        // get world size from settings
        int width = GameSettings.GetAsInt("worldWidth", 100);
        int height = GameSettings.GetAsInt("worldHeight", 100);

        World = new World(width, height);
        Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z);
    }

    private void CreateWorldFromSaveFile()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(File.ReadAllText(loadWorldFromFile));

        World = (World)serializer.Deserialize(reader);
        reader.Close();

        Camera.main.transform.position = new Vector3(World.Width / 2.0f, World.Height / 2.0f, Camera.main.transform.position.z);
    }
}
