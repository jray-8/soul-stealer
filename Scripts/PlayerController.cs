using System.Collections.Generic;
using UnityEngine;

public class PlayerController : CharacterMovement
{
    [Header("Attacking")]
    [SerializeField] protected float playerFov = 90f;
    [SerializeField] protected float attackDistance = 1.2f;
    [SerializeField] protected float attackCooldown = 0.5f;
    [SerializeField] protected float assassinateTime = 1.8f;
    [SerializeField] protected float slapDelay = 0.3f; // time until the force is applied
    [SerializeField] protected float strength = 4f;

    [Header("Abilities")]
    [SerializeField] protected float rollSpeed = 14f;
    [SerializeField] protected float rollDistance = 4f;
    [SerializeField] private float ballHeight = 0.5f; // height of reduced collider
    [Tooltip("Time before you can be stunned again.")]
    [SerializeField] protected float stunImmunity = 0.8f;
    [Tooltip("Awaken enemies by touching them.")]
    [SerializeField] protected bool provokeEnemies = true;

    private PlayerStatus myStatus;
    private CapsuleCollider playerCollider;
    private CapsuleCollider jacketCollider; // slippery
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // states
    private bool stunned = false;
    private bool rolling = false;
    private bool attacking = false; // slap attack
    private bool silencing = false;

    // stunned
    private float timeStunned; // how long current stun will last
    private float recoveryTimer;
    private float stunImmunityCountdown;

    public float StunRemaining{
        get {
            float timeLeft = timeStunned - recoveryTimer;
            return timeLeft < 0 ? 0 : timeLeft;
        }
    }

    // attacking
    private bool attackLock; // lock attack button on true - must let go to use again
    private float silenceTimer; // how long unti enemy is executed
    private float slapCountdown;
    private float nextAttackTime; // time until you can attack again
    
    private bool CanAttack{
        get {return nextAttackTime <= 0;}
        set {
                if (value == true){ // instant refresh
                    nextAttackTime = 0;
                }
                else{ // restart cooldown
                    nextAttackTime = attackCooldown;
                }
            } 
    }

    // rolling
    private float rollDuration;
    private float rollTimer;
    private bool isSmall; // reduced collider size

    // moving
    private bool hasTraction; // player can only pick up speed on the ground

    private bool MovingForwards{
        get {return moveVertical > 0;}
    }
    private bool MovingBackwards{
        get {return moveVertical < 0;}
    }

    // controls
    private float moveHorizontal;
    private float moveVertical;
    private bool jumpButton;
    private bool runButton;
    private bool attackButton;
    private bool rollButton;

    // for animations
    private bool slapSwitch = false; // must trigger slap animation

    // animation states
    private const string PLAYER_IDLE = "Idle";
    private const string PLAYER_SNEAK = "StealthWalk";
    private const string PLAYER_WALK_BACKWARDS = "WalkBackwards";
    private const string PLAYER_RUN = "Sprint";
    private const string PLAYER_ASSASSINATE = "Assassinate";
    private const string PLAYER_BACKHAND = "Backhand";
    private const string PLAYER_JUMP = "JumpUp";
    private const string PLAYER_FALL = "Fall";
    private const string PLAYER_FLINCH = "Flinch2";
    private const string PLAYER_DEATH = "Death3";
    private const string PLAYER_STUNNED = "Stunned";
    private const string PLAYER_ROLL = "DiveRoll";

    void ReadControls(){
        // movement
        moveHorizontal = Input.GetAxis("Horizontal");
        moveVertical = Input.GetAxis("Vertical");
        runButton = Input.GetButton("Run");
        if (IsGrounded){ // can start running
            hasTraction = true;
        }
        if (!runButton && !IsGrounded){
            hasTraction = false; // lose traction - can only slow down
        }
        // jump
        jumpButton = Input.GetButton("Jump");
        // attack
        attackButton = Input.GetButton("Attack");
        if (!attackButton){
            attackLock = false; // unlock
        }
        bool silencingNow = false; // check for the current frame - must be held
        if (attackButton && !attackLock && CanAttack){ // cooled down
            // sneaking or still
            if (!runButton && !IsJumping && !rolling){
                silencingNow = true;
            }
            else if (!silencing){ // if silence attack is not currently being cancelled
                attacking = true; // slap attack - push back
                slapSwitch = true;
            }
        }
        // - trigger silence -
        if (silencing && !silencingNow){ // cancelled
            CancelSilenceAttack();
        }
        else{
            silencing = silencingNow;
        }
        // roll - when still or moving forwards on ground
        if (Input.GetButtonDown("Roll")){ // one trigger per press
            if (IsGrounded && !IsJumping && (moveVertical >= 0) && !rolling){
                StartRolling();
            }
        }
    }

    void GetMovementSpeed(){
        if (rolling){
            MoveSpeed = rollSpeed;
            return; // not affected by axis
        }
        else if (runButton && hasTraction){ // cannot start running in air - but can continue running
            MoveSpeed = runSpeed;
        }
        else{
            MoveSpeed = walkSpeed;
        }
        MoveSpeed *= Mathf.Abs(moveVertical); // affected by axis
    }

    protected override void OnJump(){
        StopRolling();
    }
    void PlayerJump(){
        if (jumpButton){
            Jump();
        }
    }

    public override void TakeKnockback(Vector3 knockbackForce){
        base.TakeKnockback(knockbackForce);
        CancelSilenceAttack();
    }

    // is the specified direction inside the player Fov
    bool DirInView(Vector3 dir){
        float dot = Vector3.Dot(dir.normalized, BodyForward); // [-1,1]
        float threshold = (1f - (playerFov/2f) / 180f);
        if (dot >= threshold){
            return true; // in Fov
        }
        return false;
    }

    bool LookingAtEnemy(EnemyController enemy){
        Vector3 dir = enemy.BodyPosition - BodyPosition;
        return DirInView(dir);
    }

    bool BehindEnemy(EnemyController enemy){
        Vector3 enemyDir = enemy.BodyForward;
        return DirInView(enemyDir);
    }

    delegate void AttackMove(EnemyController enemy); // methods that attack an enemy

    void AttackTargets(AttackMove attackMethod, bool singleTarget = false){
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies){
            EnemyController controller = enemy.GetComponent<EnemyController>();
            if (!myStatus.CanTargetEnemy(controller)){
                continue; // cannot hit this enemy
            }
            // in range
            if (controller.CloseToPlayer(attackDistance, true)){
                // perform a specific attack
                attackMethod(controller);
                // do not hit anyone else
                if (singleTarget){
                    break;
                }
            }
        }
    }

    void SlapAttack(EnemyController enemy){
        if (!LookingAtEnemy(enemy)){ // must be facing enemy
            return;
        }
        float force = strength;
        if (!runButton){ // scale down when not running
            force *= 0.5f;
        }
        Knockback(enemy, force, -15f);
        enemy.FindPlayer(); // gain its attention
    }
    void PerformSlap(){ // try to
        if (attacking){
            attacking = false;
            CanAttack = false; // cooldown
            attackLock = true; // must let go of button
            slapCountdown = slapDelay; // slap when countdown finishes
        }
        DelaySlap();
    }
    void DelaySlap(){
        if (slapCountdown > 0){
            slapCountdown -= Time.deltaTime;
            if (slapCountdown <= 0){ // it's time
                AttackTargets(SlapAttack);
            }
        }
    }

    void CooldownAttack(){
        if (nextAttackTime > 0){
            nextAttackTime -= Time.deltaTime;
        }
    }

    // assassinate when finished
    void ChargeSilenceAttack(){
        if (silencing){
            if (silenceTimer >= assassinateTime){
                AttackTargets(Assassinate);
                CancelSilenceAttack();
            }
            else{ // charge
                silenceTimer += Time.deltaTime;
            }
        }
        // restart
        else{
            silenceTimer = 0;
        }
    }
    // executes an enemy you are hidden from
    void Assassinate(EnemyController enemy){
        // if the assassination fails, the enemy is slapped instead
        if (!BehindEnemy(enemy)){ // must be behind enemy
            SlapAttack(enemy);
            return;
        }
        // finally, execute the enemy
        CharacterStatus enemyStatus = enemy.gameObject.GetComponent<CharacterStatus>();
        enemyStatus.Die();
    }
    // cancels assassination attempt
    void CancelSilenceAttack(){
        if (silencing){
            silencing = false;
            CanAttack = false; // cooldown attack
            attackLock = true;
        }
    }

    void MakeSmall(){ // reduce collider size
        float scale = ballHeight / originalColliderHeight;
        playerCollider.height = originalColliderHeight * scale; // half size
        playerCollider.center = originalColliderCenter + (Vector3.down * (originalColliderHeight - playerCollider.height) / 2f); // anchor bottom
        jacketCollider.enabled = false;
        isSmall = true;
    }
    void ResetCollider(){
        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;
        jacketCollider.enabled = true;
        isSmall = false;
    }

    void StartRolling(){
        // exit attacking states
        attacking = false;
        rolling = true;
        rollTimer = 0;
    }
    void PlayerRoll(){
        if (!rolling){
            return;
        }
        rollTimer += Time.deltaTime;
        if (!isSmall){ // make collider small
            MakeSmall();
        }
        if (rollTimer >= rollDuration){ // done
            StopRolling();
        }
    }
    void StopRolling(){
        rolling = false;
        ResetCollider(); // normal size
    }

    public bool Stun(float stunTime){
        if (stunned || stunImmunityCountdown > 0){
            return false; // cooldown not finished
        }
        stunned = true;
        timeStunned = stunTime; // how long it lasts
        recoveryTimer = 0;
        if (rolling){
            StopRolling();
        }
        CancelSilenceAttack();
        return true;
    }
    void ApplyStun(){
        recoveryTimer += Time.deltaTime;
        rb.velocity = Vector3.zero; // cease all motion while stunned
        if (recoveryTimer >= timeStunned){ // end stun
            stunned = false;
            stunImmunityCountdown = stunImmunity;
        }
    }
    void CooldownStunImmunity(){
        if (stunImmunityCountdown > 0){
                stunImmunityCountdown -= Time.deltaTime;
        }
    }

    void PlayerTurn(){
        float degrees = moveHorizontal * rotateSpeed * Time.fixedDeltaTime;
        base.Turn(degrees);
    }

    void PlayerMove(){
        Vector3 dir = transform.forward;
        // can only roll forwards
        if (!rolling && MovingBackwards){ 
            dir *= -1; // move backwards
        }
        base.Move(dir);
    }

    public void ChooseAnimation(){
        // priority play - cut off min animation
        if (status.IsDead){
            animManager.PlayOnce(PLAYER_DEATH);
            return;
        }
        else if (stunned){
            animManager.ChangeAnimationState(PLAYER_STUNNED);
            return;
        }
        else if (IsFlinching){
            if (!animManager.PlayOnce(PLAYER_FLINCH)){
                return;
            }
        }
        else if (rolling){
            animManager.ChangeAnimationState(PLAYER_ROLL, 0.15f);
            return;
        }
        else if (slapSwitch){ // slap attack
            if (!animManager.PlayOnce(PLAYER_BACKHAND)){
                return;
            }
            else{ // finished animation
                slapSwitch = false;
            }
        }
        slapSwitch = false; // cancelled by a higher priority animation
        // main structure - defaults
        if (IsJumping && rb.velocity.y > 0){ // jump up
            animManager.PlayOnce(PLAYER_JUMP);
        }
        else if (!IsGrounded){ // fall down
            animManager.ChangeAnimationState(PLAYER_FALL);
        }
        else if (silencing){
            animManager.PlayOnce(PLAYER_ASSASSINATE);
        }
        // standard movement
        else if (Mathf.Abs(moveVertical) > 0.1f){
            if (MovingBackwards){ // backwards
                animManager.ChangeAnimationState(PLAYER_WALK_BACKWARDS);
            }
            else{ // forwards
                if (runButton){
                    animManager.ChangeAnimationState(PLAYER_RUN);
                }
                else{ // walk
                    animManager.ChangeAnimationState(PLAYER_SNEAK, 0.1f);
                }
            }
        }
        else{ // idle
            animManager.ChangeAnimationState(PLAYER_IDLE);
        }
    }

    // Start is called before the first frame update
    protected override void Start(){
        base.Start();
        myStatus = status as PlayerStatus; // downcast
        if (myStatus == null){ // must have
            Debug.Log("Player is missing a PlayerStatus.");
        }
        CapsuleCollider[] colliders = gameObject.GetComponents<CapsuleCollider>();
        playerCollider = colliders[0];
        jacketCollider = colliders[1];
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;
        isSmall = false;
        // compute roll time: t = d/v
        rollDuration = rollDistance / rollSpeed;
    }

    void Update(){
        // animation
        ChooseAnimation();
        // cooldowns
        CooldownJump();
        CooldownStagger();
        if (stunned){ // do not read controls or cooldown attack
            ApplyStun();
            return;
        }
        else{ // wear off immunity to stun
            CooldownStunImmunity();
        }
        CooldownAttack();
        // perform actions
        if (IsFlinching){
            return;
        }
        ReadControls();
        GetMovementSpeed();
        PlayerRoll();
        PerformSlap();
        ChargeSilenceAttack();
    }

    override protected void FixedUpdate(){
        if (stunned){
            return;
        }
        base.FixedUpdate();
        if (IsFlinching){
            return;
        }
        // attempt events
        PlayerJump();
        PlayerTurn();
        PlayerMove();
    }

    // awake enemies by bump into them
    void OnCollisionEnter(Collision collision){
        if (provokeEnemies && collision.transform.CompareTag("Enemy")){
            EnemyController controller = collision.transform.GetComponent<EnemyController>();
            controller.FindPlayer();
        }
    }
}
