using UnityEngine;

public enum FoodType
{
    Cookies,
    Donuts,
    GingerBreadMan,
    Waffles,
    Pancakes
}

public class CookieStyler : MonoBehaviour
{
    [Header("Sprite Component")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Food Type Selection")]
    [SerializeField] private FoodType currentFoodType = FoodType.Cookies;
    
    [Header("Food Sprites")]
    [SerializeField] private Sprite cookiesSprite;
    [SerializeField] private Sprite donutsSprite;
    [SerializeField] private Sprite gingerBreadManSprite;
    [SerializeField] private Sprite wafflesSprite;
    [SerializeField] private Sprite pancakesSprite;
    
    private FoodType previousFoodType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // Set initial sprite
        previousFoodType = currentFoodType;
        UpdateSprite();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if food type changed in inspector
        if (currentFoodType != previousFoodType)
        {
            UpdateSprite();
            previousFoodType = currentFoodType;
        }
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        Sprite newSprite = GetSpriteForFoodType(currentFoodType);
        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }
    
    private Sprite GetSpriteForFoodType(FoodType foodType)
    {
        return foodType switch
        {
            FoodType.Cookies => cookiesSprite,
            FoodType.Donuts => donutsSprite,
            FoodType.GingerBreadMan => gingerBreadManSprite,
            FoodType.Waffles => wafflesSprite,
            FoodType.Pancakes => pancakesSprite,
            _ => null
        };
    }
    
    // Public method to change food type programmatically
    public void SetFoodType(FoodType foodType)
    {
        currentFoodType = foodType;
        UpdateSprite();
        previousFoodType = currentFoodType;
    }
    
    // Public method to get current food type
    public FoodType GetCurrentFoodType()
    {
        return currentFoodType;
    }
}
