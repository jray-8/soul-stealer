using UnityEngine;

public class ArrowController : MonoBehaviour
{
    private bool active;
    private float speed;
    private float damage;
    private float knockback;
    private bool stuns;
    private float stunTime;
    private float despawnTime;
    private Rigidbody rb;

    // remove if missed
    private float maxLifeTime = 10f; // how long can the arrow exist in the air
    private float timer = 0;

    // initialize and shoot
    // defaults: 1.5 knockback, no stun time, 7s to despawn
    public void Fire(float speed, float damage, float knockback, float stunTime, float despawnTime){
        this.speed = speed;
        this.damage = damage;
        this.knockback = knockback;
        if (stunTime <= 0){
            stuns = false;
        }
        else{
            stuns = true;
            this.stunTime = stunTime;
        }
        this.despawnTime = despawnTime;
        rb = gameObject.GetComponent<Rigidbody>();
        rb.drag = 0; // no resistance
        rb.AddForce(transform.forward * speed, ForceMode.VelocityChange);
        active = true;
    }
    public void Fire(float speed, float damage, float knockback, float stunTime){
        Fire(speed, damage, knockback, stunTime, 7f);
    }
    public void Fire(float speed, float damage, float knockback){
        Fire(speed, damage, knockback, 0f, 7f);
    }
    public void Fire(float speed, float damage){
        Fire(speed, damage, 1.5f, 0f, 7f);
    }
    
    void OnCollisionEnter(Collision target){
        if (!active){return;}
        // apply damage
        if (target.transform.tag == "Player"){
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerStatus status = player.GetComponent<PlayerStatus>();
            PlayerController controller = player.GetComponent<PlayerController>();
            status.TakeDamage(damage);
            if (stuns){ // stun
                controller.Stun(stunTime);
            }
            Vector3 knockbackForce = transform.forward * knockback;
            controller.TakeKnockback(knockbackForce);
        }
        // stop moving
        active = false;
        rb.isKinematic = true;

        // stick to target
        transform.SetParent(target.transform);

        // inactivate arrow
        gameObject.GetComponent<Collider>().enabled = false;
        Destroy(gameObject, despawnTime);
    }

    void Update(){
        if (!active){
            return;
        }
        // remove if it never hits anything
        if (timer >= maxLifeTime){
            Destroy(gameObject);
        }
        else{
            timer += Time.deltaTime;
        }
    }
}
