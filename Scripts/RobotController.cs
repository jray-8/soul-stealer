using UnityEngine;

public class RobotController : EnemyController
{
    [Header("Abilities")]
    [SerializeField] protected float sensorLength = 3f; // distance to detect obstacles from self
    [SerializeField] protected float attackChargeTime = 1.2f;
    [SerializeField] protected float shockRadius = 3f;
    [SerializeField] protected float enhancedFov = 360; // enhanced sight while tracking player
    [SerializeField] protected float enhancedSight = 30f;

    protected Ray forwardRay;

    // store to reuse
    private float defaultFov;
    private float defaultSight;

    // attack
    private float chargeTimer; // time till attack executes
    private bool charging = false;
    protected bool IsCharging{
        get {return charging;}
    }

    protected override void Start(){
        base.Start();
        defaultFov = fov;
        defaultSight = sightRadius;
    }

    protected override void OnAttack(){ // kill player
        if (CloseToPlayer(shockRadius, false)){ // hit by attack radius 3D
            playerStatus.Die();
        }
    }

    protected void StartCharging(){
        if (!charging){
            charging = true;
            chargeTimer = 0;
        }
    }
    protected void PrepareAttack(){
        // cancel charge - outside shock zone
        if (!charging || !CloseToPlayer(shockRadius, false)){
            charging = false;
            return;
        }
        // attack when ready
        if (chargeTimer >= attackChargeTime){
            Attack();
        }
        else{ // charge up
            chargeTimer += Time.fixedDeltaTime;
        }
    }

    protected override void OnDetectPlayer(){
        // enhanced sight
        fov = enhancedFov;
        sightRadius = enhancedSight;
    }

    protected override void OnLosePlayer(){
        charging = false; // cuts off charging
        // restore sight
        fov = defaultFov;
        sightRadius = defaultSight;
    }

    // what counts as an obstacle to this robot
    protected virtual bool ValidObstacle(RaycastHit hit){
        return DirectionHorizontal(hit.normal); // checks for mostly horizontal surface
    }

    protected bool DetectObstacles(Ray scanRay){
        //Debug.DrawRay(forwardRay.origin, forwardRay.direction*sensorLength, Color.green); //!
        RaycastHit[] hitList = Physics.RaycastAll(scanRay, sensorLength);
        for (int i=0; i < hitList.Length; ++i){
            RaycastHit hit = hitList[i];
            if (environmentTags.Contains(hit.transform.tag)){
                return ValidObstacle(hit); // detect if it is considered an obstacle
            }
        }
        return false;
    }

    // robot attack phase
    protected override void ChasePlayer(){
        // trigger attack start
        if (CloseToAttackPlayer(false)){ // 3D distance
            StartCharging();
        }
        // attacks when finished charging - handles charge cancel
        PrepareAttack();
    }
}