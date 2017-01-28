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
		world.CharacterManager.CharacterCreated += OnCharacterCreated;

		foreach(Character character in world.CharacterManager)
        {
			world.CharacterManager.OnCharacterCreated(
                new CharacterEventArgs(character));
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

        GameObject inventoryGameObject = new GameObject("Inventory");
        SpriteRenderer inventorySpriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
        inventorySpriteRenderer.sortingOrder = 1;
        inventorySpriteRenderer.sortingLayerName = "Characters";
        inventoryGameObject.transform.SetParent(characterGameObject.transform);
        inventoryGameObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        inventoryGameObject.transform.localPosition = new Vector3(0, -0.37f, 0);

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
        characterGameObject.transform.position = new Vector3(args.Character.X, args.Character.Y, 0);

        SpriteRenderer inventorySpriteRenderer = characterGameObjectMap[args.Character].transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
        inventorySpriteRenderer.sprite = args.Character.Inventory != null ? SpriteManager.Current.GetSprite("Inventory", args.Character.Inventory.GetName()) : null;
    }	
}
