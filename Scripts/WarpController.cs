using UnityEngine;

public class WarpController : MonoBehaviour
{
    private bool active = true;
    void OnTriggerEnter(Collider target){
        // player has entered the portal
        if (active && target.CompareTag("Player")){
            if (!LevelManager.LoadNextLevel()){
                PlayerStatus playerStatus = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatus>();
                playerStatus.LoadVictoryScreen();
                active = false;
            }
            // else, next level is loaded
        }
    }
}
