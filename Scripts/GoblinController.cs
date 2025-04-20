using UnityEngine;

public class GoblinController : EnemyController
{
    [Header("Abilities")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 21f;
    [SerializeField] private float arrowKnockback = 1.5f;

    private Collider myCollider;

    // animation states
    private const string GOBLIN_IDLE = "Idle3";
    private const string GOBLIN_SCAN = "IdleTurnAround";
    private const string GOBLIN_FLINCH = "Flinch4";
    private const string GOBLIN_SHOOT = "BowShot";
    private const string GOBLIN_DEATH = "Death2";
    private const string GOBLIN_JUMP = "JumpUp";
    private const string GOBLIN_FALL = "JumpAir";
    private const string GOBLIN_SPRINT = "Sprint";
    private const string GOBLIN_WALK = "Walk";

    override protected void Start(){
        base.Start();
        myCollider = gameObject.GetComponent<Collider>();
    }

    protected override void OnAttack(){
        // shoot arrow
        GameObject arrow = GameObject.Instantiate(arrowPrefab, BodyPosition, bodyTransform.rotation);
        arrow.transform.LookAt(player.BodyPosition);
        arrow.transform.position += arrow.transform.forward; 
        ArrowController script = arrow.GetComponent<ArrowController>();
        Physics.IgnoreCollision(myCollider, arrow.GetComponent<Collider>()); // prevent collision with shooter
        if (stunOnHit){ // stun arrow
            script.Fire(arrowSpeed, attackDamage, arrowKnockback, stunTime);
        }
        else{
            script.Fire(arrowSpeed, attackDamage, arrowKnockback, 0f);
        }
        //Debug.DrawLine(arrow.transform.position, player.BodyPosition, Color.red, 0.5f); //!
    }

    // goblin attack phase
    protected override void ChasePlayer(){
        FaceTarget(player.transform.position); // do not move
        if (CloseToAttackPlayer()){ // horizontal radius - infinite height
            Attack();
        }
    }

    protected override void ChooseAnimation(){
        // priority
        if (status.IsDead){
            animManager.PlayOnce(GOBLIN_DEATH);
            return;
        }
        else if (IsFlinching){
            if (!animManager.PlayOnce(GOBLIN_FLINCH)){
                return;
            }
        }
        else if (IsAttacking){
            if (!animManager.PlayOnce(GOBLIN_SHOOT)){
                return;
            }
        }
        // main states
        if (IsJumping && rb.velocity.y > 0){
            animManager.PlayOnce(GOBLIN_JUMP);
        }
        else if (!IsGrounded){
            animManager.ChangeAnimationState(GOBLIN_FALL);
        }
        // moving
        else if (HorizontalSpeed > 0.5f){
            if (MoveSpeed >= runSpeed){ // runing
                animManager.ChangeAnimationState(GOBLIN_SPRINT);
            }
            else{ // walking
                animManager.ChangeAnimationState(GOBLIN_WALK);
            }
        }
        // still
        else{
            if (pathController && pathController.IsScanning){
                animManager.ChangeAnimationState(GOBLIN_SCAN);
            }
            else{
                animManager.ChangeAnimationState(GOBLIN_IDLE);
            }
        }
    }

	protected override void EnemyUpdate(){
		base.EnemyUpdate();
	}
}