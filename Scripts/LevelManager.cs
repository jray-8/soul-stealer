using UnityEngine.SceneManagement;

public static class LevelManager
{
    public static readonly int LEVEL_COUNT = SceneManager.sceneCount;
    private static int globalSceneIndex = 0; // current level

    // Scene Management
    public static bool LoadNextLevel(){ // true if the next level exists
        if (globalSceneIndex >= (LEVEL_COUNT - 1)){
            return false;
        }
        SceneManager.LoadScene(++globalSceneIndex);
        return true;
    }

    public static void ReloadLevel(){
        SceneManager.LoadScene(globalSceneIndex);
    }

    public static void RestartGame(){
        globalSceneIndex = 0;
        ReloadLevel();
    }
}