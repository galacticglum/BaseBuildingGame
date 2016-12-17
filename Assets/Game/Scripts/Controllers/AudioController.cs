using UnityEngine;

public class AudioController : MonoBehaviour
{
    private float audioCooldown = 0;

	// Use this for initialization
	private void Start ()
    {
        WorldController.Instance.World.FurnitureCreated += OnFurnitureCreated;
        WorldController.Instance.World.TileChanged += OnTileChanged;
    }

    private void Update()
    {
        audioCooldown -= Time.deltaTime;
    }

    private void OnTileChanged(object sender, TileChangedEventArgs args)
    {
        // FIXME
        if (audioCooldown > 0)
        {
            return;
        }

        AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + args.Tile.Type + "_OnCreated");
        if (audioClip == null) return;
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
        audioCooldown = 0.1f;
    }

    public void OnFurnitureCreated(object sender, FurnitureCreatedEventArgs args)
    {
        // FIXME
        if (audioCooldown > 0)
        {
            return;
        }

        AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + args.Furniture.Type + "_OnCreated") ?? Resources.Load<AudioClip>("Audio/Wall_OnCreated");
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
        audioCooldown = 0.1f;
    }
}
