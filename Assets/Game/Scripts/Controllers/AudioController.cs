using UnityEngine;

public class AudioController
{
    private float audioCooldown;
    public AudioController()
    {
        World.Current.FurnitureCreated += OnFurnitureCreated;
        World.Current.TileChanged += OnTileChanged;
    }
	
    public void Update(float deltaTime)
    {
        audioCooldown -= deltaTime;
    }

    private void OnTileChanged(object sender, TileEventArgs args)
    {
        if (audioCooldown > 0)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Sounds/Floor_OnCreated"), Camera.main.transform.position);
        audioCooldown = 0.1f;
    }

    public void OnFurnitureCreated(object sender, FurnitureEventArgs args)
    {
        if (audioCooldown > 0)
        {
            return;
        }
		
        AudioClip audioClip = Resources.Load<AudioClip>("Sounds/" + args.Furniture.Type + "_OnCreated") ?? Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
        audioCooldown = 0.1f;
    }
}
