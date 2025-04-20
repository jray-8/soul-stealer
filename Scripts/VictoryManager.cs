using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    [SerializeField] private float duration = 10f;
    private float timer = 0;

    void Start(){
        Time.timeScale = 0; // freeze game
    }

    void Update(){
        if (timer >= duration){
            Time.timeScale = 1; // un-freeze
            LevelManager.RestartGame();
        }
        else{
            timer += Time.unscaledDeltaTime;
        }
    }
}
