using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSceneBootstrap : MonoBehaviour
{
    const string PreScene = "preM00";
    const string MainScene = "M00";
    const string DocumentScene = "document";

    static bool registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (!registered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            registered = true;
        }

        Bootstrap(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Bootstrap(scene);
    }

    static void Bootstrap(Scene scene)
    {
        if (scene.name != PreScene && scene.name != MainScene && scene.name != DocumentScene)
            return;

        if (scene.name == PreScene && FindFirstObjectByType<PreM00TutorialBootstrap>() != null)
            return;

        if (scene.name == MainScene && FindFirstObjectByType<M00TutorialBootstrap>() != null)
            return;

        if (scene.name == DocumentScene && FindFirstObjectByType<DocumentTutorialBootstrap>() != null)
            return;

        if (FindFirstObjectByType<TutorialSceneBootstrap>() != null)
            return;

        var root = new GameObject("TutorialSceneBootstrap");
        root.AddComponent<TutorialSceneBootstrap>();

        if (scene.name == PreScene)
            root.AddComponent<PreM00TutorialBootstrap>();
        else if (scene.name == MainScene)
            root.AddComponent<M00TutorialBootstrap>();
        else
            root.AddComponent<DocumentTutorialBootstrap>();
    }
}
