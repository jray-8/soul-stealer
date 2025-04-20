using UnityEngine;

public class QuitController : MonoBehaviour
{
    void Update(){
        // escape to quit
        if (Input.GetKeyDown(KeyCode.Escape)){
            Application.Quit();
        }
    }
}
