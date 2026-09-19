#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>用 Unity API 生成主菜单场景，避免手写场景 YAML。</summary>
public static class MainMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Devil/Rebuild Main Menu Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = MainMenuController.SceneName;

        GameObject controller = new GameObject(nameof(MainMenuController));
        controller.AddComponent<MainMenuController>();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };

        AssetDatabase.SaveAssets();
        Debug.Log("[MainMenuBuilder] MainMenu scene created and set as build index 0.");
    }
}
#endif
