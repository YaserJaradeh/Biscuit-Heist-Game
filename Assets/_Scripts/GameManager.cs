using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectCookieSfx;
    
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private NoiseMeterUI noiseMeterUI;

    [Header("Transitions")] 
    [SerializeField] private Animator transitionController;
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CollectCookie()
    {
        // Play sound effect
        PlayCollectCookieSfx();
        // Update score UI
        if (scoreText != null)
        {
            int.TryParse(scoreText.text, out int remaining);
            remaining--;
            scoreText.text = remaining.ToString();
        }
    }

    public void ExitRoom()
    {
        // Play sound
        // Start transition animation
        transitionController.SetBool("IsEnding", true);
        transitionController.SetBool("IsStarting", false);
        // Load Next Scene (after delay) 
        Invoke(nameof(LoadNextScene), 5f);
    }
    
    private void LoadNextScene()
    {
        Debug.Log("Loading Next Scene...");
        // Reset transition state before loading new scene
        transitionController.SetBool("IsEnding", false);
        transitionController.SetBool("IsStarting", true);
        
        // Get current scene index and load the next one
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = (currentSceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
        
        SceneManager.LoadScene(nextSceneIndex);
    }
    
    private void PlayCollectCookieSfx()
    {
        if (audioSource != null && collectCookieSfx != null)
        {
            audioSource.PlayOneShot(collectCookieSfx);
        }
    }
    
    
}
