using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    private Vector3 spawnPoint;
    private Quaternion spawnRotation;

    protected bool reachedFallLimit = false;
    protected float voidTime = 2f; // how long can character survive in the void
    
    [SerializeField] protected float decayTime = 7f; // seconds until character despawns
    [SerializeField] protected float maxHealth = 40f;
    private float health;
    protected bool alive;

    public float MaxHealth{
        get {return maxHealth;}
    }

    public float Health{
        get {return health;}
        private set {
            if (value > maxHealth) health = maxHealth;
            else{
                health = value;
            }
        }
    }

    public bool IsDead{
        get {return !alive;}
    }

    public void Heal(float hp){ // cannot exceed max
        Health += hp;
    }

    public void TakeDamage(float damage){
        Health -= damage;
    }

    protected void ChangeMaxHealth(float newMax){
        maxHealth = newMax;
    }

    public void AddPermanentHealth(float hp){ // adds to max as well
        Heal(hp);
        ChangeMaxHealth(maxHealth + hp);
    }

    public void ReachVoid(){
        reachedFallLimit = true;
    }

    // based on character's current position / rotation
    protected void UpdateSpawnLocation(){
        spawnPoint = transform.position;
        spawnRotation = transform.rotation;
    }

    protected virtual void Respawn(bool heal=true){
        transform.position = spawnPoint;
        transform.rotation = spawnRotation;
        reachedFallLimit = false;
        if (heal){
            Revive();
        }
    }

    protected virtual void Revive(){
        alive = true;
        Health = maxHealth;
        gameObject.SetActive(true);
    }

    protected virtual void Disintegrate(){
        float damage = (maxHealth / voidTime) * Time.deltaTime;
        TakeDamage(damage);
    }

    protected virtual void OnDeath(){
        Destroy(gameObject, decayTime);
    }

    public void Die(){
        if (Health > 0){
            Health = 0;
        }
        alive = false;
        OnDeath();
    }

    protected virtual void CheckDeath(){
        if (Health <= 0 && alive){
            Die();
        }
    }

    // Start
    protected virtual void Start(){
        UpdateSpawnLocation();
        Revive();
    }

    protected virtual void Update(){
        CheckDeath();
        if (reachedFallLimit){
            Disintegrate();
        }
    }
}
