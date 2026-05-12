using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    // Singleton instance
    public static GlobalUIManager Instance { get; private set; }

    [Header("UI Canvases")]
    public GameObject optionsCanvas;
    public GameObject achievementCanvas;

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicates if returning to the title screen
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across all scenes
    }

    public void ToggleOptions(bool state)
    {
        optionsCanvas.SetActive(state);
    }

    public void ToggleAchievements(bool state)
    {
        achievementCanvas.SetActive(state);
    }
}