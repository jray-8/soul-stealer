using UnityEngine;

public class SoulController : MonoBehaviour
{
    [SerializeField] private bool cloaked = true;
    [SerializeField] private bool drawAggro = false;
    private bool collected = false;
    private PlayerStatus playerStatus;

    public bool IsCloaked{
        get {return cloaked;}
    }

    void Start(){
        playerStatus = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatus>();
    }

    void OnTriggerEnter(Collider target){ // collect this soul
        if (!collected && target.CompareTag("Player")){
            playerStatus.collectSoul();
            collected = true;
            if (drawAggro){
                playerStatus.AttractEnemies();
            }
            gameObject.tag = "Untagged"; // untag this soul
            playerStatus.FindSouls(); // update list
            Destroy(gameObject); // remove this soul
        }
    }
}
