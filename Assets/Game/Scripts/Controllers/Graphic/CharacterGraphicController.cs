using UnityEngine;
using System.Collections.Generic;

public class CharacterGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    private Dictionary<Character, GameObject> characterGameObjectMap;

	// Use this for initialization
    private void Start ()
    {
		characterGameObjectMap = new Dictionary<Character, GameObject>();
		world.CharacterCreated += OnCharacterCreated;

		foreach(Character character in world.Characters)
        {
			world.OnCharacterCreated(new CharacterEventArgs(character));
		}
	}

	public void OnCharacterCreated(object sender, CharacterEventArgs args)
    {
		GameObject characterGameObject = new GameObject();
		characterGameObjectMap.Add(args.Character, characterGameObject);
		characterGameObject.name = "Character";
		characterGameObject.transform.position = new Vector3(args.Character.X, args.Character.Y, 0);
		characterGameObject.transform.SetParent(transform, true);

		SpriteRenderer spriteRenderer = characterGameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = SpriteManager.Current.GetSprite("Characters", "p1_front");
		spriteRenderer.sortingLayerName = "Characters";

		args.Character.CharacterChanged += OnCharacterChanged;
	}

    private void OnCharacterChanged(object sender, CharacterEventArgs args)
    {
		if(characterGameObjectMap.ContainsKey(args.Character) == false)
        {
			Debug.LogError("CharacterGraphicController::OnCharacterChanged: Trying to change visuals for character not in our map.");
			return;
		}

		GameObject characterGameObject = characterGameObjectMap[args.Character];
        characterGameObject.transform.position = new Vector3( args.Character.X, args.Character.Y, 0);
	}	
}
