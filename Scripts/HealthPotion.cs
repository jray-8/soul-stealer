using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [SerializeField] private float health = 15;
    [SerializeField] private float despawnTime = -1f; // infinite
    private bool collected = false;
    private bool despawns = false;
    private float timer;
    private PlayerStatus playerStatus;

    void Start(){
        playerStatus = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStatus>();
        if (despawnTime > 0){
            despawns = true;
            timer = 0;
        }
    }

    void Update(){ // check despawn
        if (!despawns){
            return;
        }
        timer += Time.deltaTime;
        if (timer >= despawnTime){
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider target){ // collect this potion
        if (!collected && target.CompareTag("Player")){
            playerStatus.Heal(health);
            collected = true;
            Destroy(gameObject); // remove this potion
        }
    }
}
