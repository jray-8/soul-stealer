using UnityEngine;

public class GroundRobotController : RobotController
{
    // animation states
    private const string ROBOT_IDLE = "Idle2";
    private const string ROBOT_WALK = "Walk";
    private const string ROBOT_SPRINT = "Sprint";
    private const string ROBOT_JUMP = "Jump";
    private const string ROBOT_FALL = "Fall2";
    private const string ROBOT_STOMP = "Stomp";
    private const string ROBOT_LAND = "Land";
    private bool landSwitch = false;

    protected override void Start(){
        base.Start();
        nextJumpTime = 0;
    }

    protected override void OnAttack(){
        if (CloseToPlayer(shockRadius, false)){ // hit by shock radius
            Knockback(player, 9f, -80f);
            playerStatus.Die();
        }
    }

    // robot land
    protected override void OnLand(){ // attack on impact
        Attack(); // send shockwaves
        landSwitch = true;
    }

    // robot attack phase
    protected override void ChasePlayer(){
        if (!IsCharging){
            DirectChase(); // move directly to player
        }
        base.ChasePlayer();
    }

    protected override void EnemyFixedUpdate(){
        // from feet, slightly downwards
        forwardRay = new Ray(BodyPosition, BodyForward + (0.6f * Vector3.down));
        if (DetectObstacles(forwardRay)){
            Jump(); // try to jump
        }
    }

    protected override void ChooseAnimation(){
        // priority
        if (landSwitch){
            if (!animManager.PlayOnce(ROBOT_LAND)){
                return;
            }
            else{
                landSwitch = false;
            }
        }
        if (IsCharging){
            animManager.ChangeAnimationState(ROBOT_STOMP);
            return;
        }
        // main states
        if (IsJumping && rb.velocity.y > 0){
            animManager.PlayOnce(ROBOT_JUMP);
        }
        else if (!IsGrounded){
            animManager.ChangeAnimationState(ROBOT_FALL);
        }
        // moving
        else if (HorizontalSpeed > 0.2f){
            if (MoveSpeed >= runSpeed){ // runing
                animManager.ChangeAnimationState(ROBOT_SPRINT);
            }
            else{ // walking
                animManager.ChangeAnimationState(ROBOT_WALK);
            }
        }
        // still
        else{
            animManager.ChangeAnimationState(ROBOT_IDLE);
        }
    }

	protected override void EnemyUpdate(){
		base.EnemyUpdate();
	}
}