using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectCookieSfx;
    
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private NoiseMeterUI noiseMeterUI;
    
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
    
    private void PlayCollectCookieSfx()
    {
        if (audioSource != null && collectCookieSfx != null)
        {
            audioSource.PlayOneShot(collectCookieSfx);
        }
    }
    
    
}
