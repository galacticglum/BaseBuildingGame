using UnityEngine;

public class AudioController
{
    private float audioCooldown;
    public AudioController()
    {
        World.Current.cbFurnitureCreated += OnFurnitureCreated;
        World.Current.cbTileChanged += OnTileChanged;
    }
	
    public void Update(float deltaTime)
    {
        audioCooldown -= deltaTime;
    }

    private void OnTileChanged(Tile tile_data)
    {
        if (audioCooldown > 0)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Sounds/Floor_OnCreated"), Camera.main.transform.position);
        audioCooldown = 0.1f;
    }

    public void OnFurnitureCreated(Furniture furn)
    {
        if (audioCooldown > 0)
        {
            return;
        }
		
        AudioClip audioClip = Resources.Load<AudioClip>("Sounds/" + furn.Type + "_OnCreated") ?? Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
        audioCooldown = 0.1f;
    }
}
