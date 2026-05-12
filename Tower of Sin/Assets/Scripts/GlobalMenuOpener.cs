using UnityEngine;

public class GlobalMenuOpener : MonoBehaviour
{
    public void OpenGlobalOptions()
    {
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ToggleOptions(true);

             Time.timeScale = 0f;

             Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("GlobalUIManager not found");
        }
    }

    public void OpenGlobalAchievements()
    {
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ToggleAchievements(true);

             Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("GlobalUIManager not found");
        }
    }
}