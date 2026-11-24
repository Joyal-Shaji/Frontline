using System.Collections.Generic;
using UnityEngine;

public sealed class MainMenu : MonoBehaviour
{
    private enum MenuPage
    {
        Main,
        Settings,
        Controls
    }

    private readonly List<Behaviour> gameplayBehaviours = new List<Behaviour>();
    private readonly List<bool> gameplayBehaviourStates = new List<bool>();

    private MenuPage currentPage;
    private bool isPlaying;
    private float previousTimeScale = 1f;
    private float masterVolume = 1f;
    private bool fullscreen;
    private Texture2D panelTexture;
    private Texture2D overlayTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D accentTexture;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle secondaryButtonStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle footerStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMenuExists()
    {
        if (FindFirstObjectByType<MainMenu>() == null)
        {
            GameObject menuObject = new GameObject("Main Menu");
            menuObject.AddComponent<MainMenu>();
        }
    }

    private void Awake()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        masterVolume = AudioListener.volume;
        fullscreen = Screen.fullScreen;

        DisableGameplayInput<PlayerMovement>();
        DisableGameplayInput<SwitchCameraMode>();
        DisableGameplayInput<EditorViewModeMovement>();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CreateTextures();
    }

    private void OnDestroy()
    {
        if (!isPlaying)
        {
            Time.timeScale = previousTimeScale;
            RestoreGameplayInput();
        }

        DestroyTextures();
    }

    private void DisableGameplayInput<T>() where T : Behaviour
    {
        T[] behaviours = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T behaviour in behaviours)
        {
            gameplayBehaviours.Add(behaviour);
            gameplayBehaviourStates.Add(behaviour.enabled);
            behaviour.enabled = false;
        }
    }

    private void RestoreGameplayInput()
    {
        for (int i = 0; i < gameplayBehaviours.Count; i++)
        {
            if (gameplayBehaviours[i] != null)
            {
                gameplayBehaviours[i].enabled = gameplayBehaviourStates[i];
            }
        }
    }

    private void CreateTextures()
    {
        panelTexture = MakeTexture(new Color(0.055f, 0.075f, 0.09f, 0.98f));
        overlayTexture = MakeTexture(new Color(0.015f, 0.025f, 0.03f, 0.78f));
        buttonTexture = MakeTexture(new Color(0.12f, 0.17f, 0.19f, 1f));
        buttonHoverTexture = MakeTexture(new Color(0.17f, 0.25f, 0.27f, 1f));
        accentTexture = MakeTexture(new Color(0.31f, 0.91f, 0.76f, 1f));
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void DestroyTextures()
    {
        Destroy(panelTexture);
        Destroy(overlayTexture);
        Destroy(buttonTexture);
        Destroy(buttonHoverTexture);
        Destroy(accentTexture);
    }

    private void OnGUI()
    {
        if (isPlaying)
        {
            return;
        }

        EnsureStyles();
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);

        float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 650f, 1.25f);
        float panelWidth = 440f;
        float panelHeight = currentPage == MenuPage.Main ? 490f : 540f;
        float x = (Screen.width - panelWidth * scale) * 0.5f;
        float y = (Screen.height - panelHeight * scale) * 0.5f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, Vector3.one * scale);

        GUI.Box(new Rect(0f, 0f, panelWidth, panelHeight), GUIContent.none, panelStyle);
        GUI.DrawTexture(new Rect(0f, 0f, panelWidth, 5f), accentTexture);
        DrawPage(panelWidth, panelHeight);

        GUI.matrix = previousMatrix;
    }

    private void DrawPage(float panelWidth, float panelHeight)
    {
        GUI.Label(new Rect(36f, 28f, panelWidth - 72f, 22f), "FRONTLINE  /  FIELD OPERATIONS", footerStyle);
        GUI.Label(new Rect(36f, 61f, panelWidth - 72f, 62f), "FRONTLINE", titleStyle);
        GUI.Label(new Rect(36f, 120f, panelWidth - 72f, 28f), "DEFEND THE LINE. HOLD YOUR GROUND.", subtitleStyle);

        if (currentPage == MenuPage.Main)
        {
            DrawMainPage(panelWidth, panelHeight);
        }
        else if (currentPage == MenuPage.Settings)
        {
            DrawSettingsPage(panelWidth, panelHeight);
        }
        else
        {
            DrawControlsPage(panelWidth, panelHeight);
        }

        GUI.Label(new Rect(36f, panelHeight - 36f, panelWidth - 72f, 18f), "EARLY ACCESS  •  PREPARE FOR DEPLOYMENT", footerStyle);
    }

    private void DrawMainPage(float panelWidth, float panelHeight)
    {
        float buttonX = 56f;
        float buttonWidth = panelWidth - 112f;
        float buttonY = 205f;
        float buttonHeight = 58f;
        float spacing = 14f;

        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "START GAME", buttonStyle))
        {
            StartGame();
        }
        if (GUI.Button(new Rect(buttonX, buttonY + buttonHeight + spacing, buttonWidth, 50f), "SETTINGS", secondaryButtonStyle))
        {
            currentPage = MenuPage.Settings;
        }
        if (GUI.Button(new Rect(buttonX, buttonY + (buttonHeight + spacing) * 2f, buttonWidth, 50f), "CONTROLS", secondaryButtonStyle))
        {
            currentPage = MenuPage.Controls;
        }
    }

    private void DrawSettingsPage(float panelWidth, float panelHeight)
    {
        float left = 44f;
        float width = panelWidth - 88f;
        GUI.Label(new Rect(left, 190f, width, 34f), "SETTINGS", headingStyle);
        GUI.Label(new Rect(left, 245f, width, 26f), "Master volume", bodyStyle);
        masterVolume = GUI.HorizontalSlider(new Rect(left, 282f, width, 20f), masterVolume, 0f, 1f);
        AudioListener.volume = masterVolume;
        GUI.Label(new Rect(left, 305f, width, 24f), Mathf.RoundToInt(masterVolume * 100f) + "%", footerStyle);

        bool nextFullscreen = GUI.Toggle(new Rect(left, 348f, width, 34f), fullscreen, "  Fullscreen", bodyStyle);
        if (nextFullscreen != fullscreen)
        {
            fullscreen = nextFullscreen;
            Screen.fullScreen = fullscreen;
        }

        if (GUI.Button(new Rect(left, panelHeight - 100f, width, 48f), "BACK", secondaryButtonStyle))
        {
            currentPage = MenuPage.Main;
        }
    }

    private void DrawControlsPage(float panelWidth, float panelHeight)
    {
        float left = 44f;
        float width = panelWidth - 88f;
        GUI.Label(new Rect(left, 190f, width, 34f), "FIELD CONTROLS", headingStyle);
        GUI.Label(new Rect(left, 238f, width, 30f), "W  A  S  D", bodyStyle);
        GUI.Label(new Rect(left + 170f, 238f, width - 170f, 30f), "Move", footerStyle);
        GUI.Label(new Rect(left, 278f, width, 30f), "MOUSE", bodyStyle);
        GUI.Label(new Rect(left + 170f, 278f, width - 170f, 30f), "Look around", footerStyle);
        GUI.Label(new Rect(left, 318f, width, 30f), "SPACE", bodyStyle);
        GUI.Label(new Rect(left + 170f, 318f, width - 170f, 30f), "Jump", footerStyle);
        GUI.Label(new Rect(left, 358f, width, 30f), "LEFT SHIFT", bodyStyle);
        GUI.Label(new Rect(left + 170f, 358f, width - 170f, 30f), "Sprint", footerStyle);
        GUI.Label(new Rect(left, 398f, width, 30f), "E", bodyStyle);
        GUI.Label(new Rect(left + 170f, 398f, width - 170f, 30f), "Switch camera mode", footerStyle);

        if (GUI.Button(new Rect(left, panelHeight - 100f, width, 48f), "BACK", secondaryButtonStyle))
        {
            currentPage = MenuPage.Main;
        }
    }

    private void StartGame()
    {
        isPlaying = true;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        RestoreGameplayInput();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture },
            border = new RectOffset(0, 0, 0, 0)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 46,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.68f, 0.77f, 0.79f) }
        };
        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.31f, 0.91f, 0.76f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        footerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = { textColor = new Color(0.55f, 0.66f, 0.68f) }
        };
        buttonStyle = MakeButtonStyle(16, accentTexture, Color.black);
        secondaryButtonStyle = MakeButtonStyle(14, buttonTexture, Color.white);
    }

    private GUIStyle MakeButtonStyle(int fontSize, Texture2D normalTexture, Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            normal = { background = normalTexture, textColor = textColor },
            hover = { background = buttonHoverTexture, textColor = Color.white },
            active = { background = accentTexture, textColor = Color.black },
            border = new RectOffset(8, 8, 8, 8)
        };
        return style;
    }
}
