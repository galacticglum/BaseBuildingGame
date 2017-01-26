using UnityEngine;

public class AudioController : MonoBehaviour
{
	private float audioCooldown;

	// Use this for initialization
	private void Start ()
    {
		WorldController.Instance.World.FurnitureManager.FurnitureCreated += OnFurnitureCreated;
		WorldController.Instance.World.TileChanged += OnTileChanged;
	}
	
	// Update is called once per frame
	private void Update ()
    {
		audioCooldown -= Time.deltaTime;
	}

	private void OnTileChanged(object sender, TileEventArgs args)
    {
        if (audioCooldown > 0)
        {
            return;
        }

        AudioClip audioClip = Resources.Load<AudioClip>("Sounds/Floor_OnCreated");
		AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
		audioCooldown = 0.1f;
	}

	public void OnFurnitureCreated(object sender, FurnitureEventArgs args)
    {
        if (audioCooldown > 0)
        {
            return;
        }

        AudioClip audioClip = Resources.Load<AudioClip>("Sounds/"+ args.Furniture.Type +"_OnCreated") ?? 
            Resources.Load<AudioClip>("Sounds/Wall_OnCreated");

        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
		audioCooldown = 0.1f;
	}
}
