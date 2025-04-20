using UnityEngine;
using UnityEngine.AI;

public class MonsterController : EnemyController
{
    [Header("Abilities")]
    [SerializeField] float strength = 7f;

    private NavMeshAgent agent;
    private bool returningToPath;

    // animation states
    private const string MONSTER_IDLE = "Idle";
    private const string MONSTER_SCAN = "IdleScan";
    private const string MONSTER_WALK = "Walk";
    private const string MONSTER_RUN = "Run";
    private const string MONSTER_PRIMAL_WALK = "GorillaWalk";
    private const string MONSTER_JUMP = "Jump";
    private const string MONSTER_FALL = "Fall2";
    private const string MONSTER_FLINCH = "Flinch";
    private const string MONSTER_BACKHAND = "Backhand";
    private const string MONSTER_DEATH = "Death";

    protected override void Start(){
        base.Start();
        returningToPath = false;
        agent = gameObject.GetComponent<NavMeshAgent>();
        if (agent){
            agent.speed = runSpeed;
            agent.angularSpeed = rotateSpeed;
        }
    }

    protected override void OnLosePlayer(){
        if (agent && agent.isOnNavMesh){
            pathController?.ExitPath(); // do not return to pathing yet
            agent.SetDestination(pathController.CurrentDestination);
            returningToPath = true;
        }
    }

    protected override void OnAttack(){
        base.OnAttack();
        Knockback(player, strength);
    }

    // specific jump type
    protected void MonsterJump(){
        Vector3 dir = player.BodyPosition - BodyPosition;
        if (DirectionVertical(dir, 0.4f) && JumpReady){
            Jump();
        }
    }

    // monster attack phase
    protected override void ChasePlayer(){
        bool foundPath = false;
        if (agent.isOnNavMesh){
            if (agent.SetDestination(player.transform.position)){ // find path to player
                foundPath = true;
            }
        }
        // could not find a complete path
        if (!foundPath){
            DirectChase();
        }
        // attack
        if (CloseToAttackPlayer(false) && PlayerDetected){ // do not attack through walls - close enough but cannot see
            Attack();
        }
        // jump
        MonsterJump();
    }

	protected override void EnemyFixedUpdate(){
        if (returningToPath){ // using agent to get back to the track
            if (CloseTo(pathController.CurrentDestination)){
                // path controller will take it from here
                agent.ResetPath(); // stop agent
                returningToPath = false;
                pathController?.ResumePath();
            }
        }
	}

    protected override void ChooseAnimation(){
        // priority
        if (status.IsDead){
            animManager.PlayOnce(MONSTER_DEATH);
            return;
        }
        else if (IsFlinching){
            if (!animManager.PlayOnce(MONSTER_FLINCH)){
                return;
            }
        }
        else if (IsAttacking){
            if (!animManager.PlayOnce(MONSTER_BACKHAND)){
                return;
            }
        }
        // main states
        if (IsJumping && rb.velocity.y > 0){
            animManager.PlayOnce(MONSTER_JUMP); // jump up
        }
        else if (!IsGrounded){
            animManager.ChangeAnimationState(MONSTER_FALL);
        }
        // moving
        else if (HorizontalSpeed > 0.2f){
            if (MoveSpeed >= runSpeed){ // runing
                animManager.ChangeAnimationState(MONSTER_RUN, 0.1f);
            }
            else{ // walking
                animManager.ChangeAnimationState(MONSTER_WALK, 0.4f);
            }
        }
        else if (agent && agent.velocity.magnitude > 0.1f){ // agent motion
            animManager.ChangeAnimationState(MONSTER_RUN, 0.1f);
        }
        // still
        else{
            if (pathController && pathController.IsScanning){
                animManager.ChangeAnimationState(MONSTER_SCAN);
            }
            else{
                animManager.ChangeAnimationState(MONSTER_IDLE);
            }
        }
    }

	protected override void EnemyUpdate(){
		base.EnemyUpdate();
	}
}