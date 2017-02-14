using UnityEngine;

public class CharacterAnimator
{
    private readonly Character character;
    private readonly SpriteRenderer spriteRenderer;

    private int currentFrameIndex;
    private const int animationLength = 40;

    private Sprite[] frames;
    public Sprite[] Frames
    {
        get { return frames; }
        set
        {
            frames = value;
            foreach (Sprite sprite in frames)
            {
                sprite.texture.filterMode = FilterMode.Point;
            }
        }
    }

    public CharacterAnimator(Character character, SpriteRenderer spriteRenderer)
    {
        this.character = character;
        this.spriteRenderer = spriteRenderer;

        frames = new Sprite[9];
    }

    public void Update(float deltaTime)
    {
        if (currentFrameIndex >= animationLength)
        {
            currentFrameIndex = 0;
        }

        currentFrameIndex++;

        if (character.IsWalking)
        {
            switch (character.Direction)
            {
                case CharacterDirection.North: 
                    AnimateFrame(5, 6);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.East: 
                    AnimateFrame(3, 4);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.South: 
                    AnimateFrame(7, 8);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.West: 
                    AnimateFrame(3, 4);
                    spriteRenderer.flipX = true;
                    break;
            }
        }
        else
        {
            switch (character.Direction)
            {
                case CharacterDirection.North:
                    AnimateFrame(2);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.East:
                    AnimateFrame(1);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.South:
                    AnimateFrame(0);
                    spriteRenderer.flipX = false;
                    break;
                case CharacterDirection.West:
                    AnimateFrame(1); 
                    spriteRenderer.flipX = true;
                    break;
            }
        }
    }

    private void AnimateFrame(int spriteIndex)
    {
        spriteRenderer.sprite = frames[spriteIndex];
    }

    private void AnimateFrame(int spriteIndexA, int spriteIndexB)
    {
        switch (currentFrameIndex)
        {
            case 1:
                AnimateFrame(spriteIndexA);
                break;
            case animationLength / 2:
                AnimateFrame(spriteIndexB);
                break;
        }
    }
}