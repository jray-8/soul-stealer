using UnityEngine;

public class EnemyController : CharacterMovement
{
    // movement
    [Tooltip("How close before a target point is reached?")]
    [SerializeField] protected float distanceError = 0.5f; // how close to position before it is 'reached'
    protected float rotationError = 1f;

    [Header("Attacking")]
    [Tooltip("Attack while moving (do not leave path).")]
    [SerializeField] protected bool attackOnPath = false; // attack while moving (do not leave path)
    [SerializeField] protected float attackDistance = 1.2f;
    [SerializeField] protected float attackDamage = 5f;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected bool stunOnHit = false;
    [SerializeField] protected float stunTime = 0.3f;

    [Header("Sight")]
    [SerializeField] protected float fov = 90f;
    [SerializeField] protected float sightRadius = 8f;
    [SerializeField] protected float maxSearchTime = 3f; // time until enemy loses interest in player (out of sight)
    [SerializeField] protected float observationTime = 2f; // how long to inspect the crime scene

    protected PlayerController player;
    protected PlayerStatus playerStatus;
    protected EnemyPathController pathController;

    protected bool ControllerDead{
        get {return status && status.IsDead;}
    }

    // sight
    protected Ray sightRay;
    private bool playerVisible = false;
    private bool previouslyDetected = false; // switch - trigger once player detection changes

    public bool PlayerDetected {
        get {return playerVisible;}
        set {playerVisible = value;}
    }

    // movement
    public enum Speed{
        Walk = 0,
        Run
    }

    public enum RotationStyle{
        None = 0,
        WalkingPlane,
        Space3D
    }

    // chase
    private float searchTimer;
    public bool TrackingPlayer{ // currently sees or is hunting the player
        get {return (previouslyDetected);}
    }

    // attack
    protected float nextAttackTime; // time until this enemy can attack again
    private bool attacking;
    protected bool IsAttacking{
        get {return attacking;}
    }

    // investigation
    protected bool investigating;
    protected bool arrivedAtScene;
    protected Vector3 crimeScene;
    protected float crimeSceneRadius = 4f; // how far from target to be on scene
    protected float investigateTimer;

    // update the player's level of detection - only once per change of detection
    protected void HandleDetection(){
        if (PlayerDetected){
            if (!previouslyDetected){
                DetectPlayer(); // switch -> detects player
            }
        }
        else{ // cannot see player
            if (previouslyDetected){ // start losing player
                if (attackOnPath || searchTimer >= maxSearchTime){ // give up interest in player
                    LosePlayer(); // switch -> lost player
                }
                searchTimer += Time.deltaTime;
            }
        }
    }
    private void DetectPlayer(){
        playerStatus.GainDetection();
        previouslyDetected = true;
        if (!attackOnPath){ // leave path to attack player
            searchTimer = 0;
            pathController?.ExitPath(); // leave default pattern
        }
        if (investigating){
            FinishInvestigation(); // found him
        }
        OnDetectPlayer();
    }
    private void LosePlayer(){
        playerStatus.LoseDetection();
        previouslyDetected = false;
        if (!attackOnPath){
            pathController?.ResumePath(); // back to the path
        }
        OnLosePlayer();
    }
    // for additional behaviour
    protected virtual void OnDetectPlayer(){}
    protected virtual void OnLosePlayer(){}

    // make this enemy automatically find the player
    public void FindPlayer(){
        if (ControllerDead){return;} // cannot find player from the underworld...
        PlayerDetected = true;
        HandleDetection(); // prevent finding/losing the player twice
    }

    // make this enemy instantly lose the player
    public void ForgetPlayer(){
        searchTimer = maxSearchTime; // do not search, you forget them completely
        PlayerDetected = false;
        HandleDetection();
    }

    // true if the investigation could be started
    private bool StartInvestigation(Vector3 location){ // helper only
        if (!PlayerDetected && !attackOnPath){ // no need to investigate
            crimeScene = location;
            investigating = true;
            investigateTimer = 0;
            if (!attackOnPath){
                pathController?.ExitPath();
            }
            return true;
        }
        return false;
    }
    protected void FinishInvestigation(){
        investigating = false;
        if (!PlayerDetected){
            if (!attackOnPath){
                pathController?.ResumePath();
            }
        }
    }

    // this enemy will look at the crime scene from a distance
    public void InvestigateDistance(Vector3 location){
        if (StartInvestigation(location)){
            arrivedAtScene = true; // do not move
            FaceTarget(location); // look at the crime scene
        }
    }

    // this enemy will walk to the crime scene
    public void InvestigateScene(Vector3 location, float sceneDistance){ // new radius
        if (StartInvestigation(location)){
            crimeSceneRadius = sceneDistance;
            arrivedAtScene = false; // walk there
        }
    }
    public void InvestigateScene(Vector3 location){
        InvestigateScene(location, crimeSceneRadius);
    }

    virtual protected void Investigate(){
        if (!arrivedAtScene){ // move to location
            if (MoveTowards(crimeScene, crimeSceneRadius)){
                arrivedAtScene = true;
            }
            else{ // don't start timer until the crime scene is reached
                return;
            }
        }
        investigateTimer += Time.deltaTime;
        if (investigateTimer >= observationTime){ // finished investigation
            FinishInvestigation();
        }
    }

    // tries to detect the player from current position - sets PlayerDetected
    protected bool LookForPlayer(bool castDown=true){
        if (playerStatus.IsSafe){ // cannot be seen
            PlayerDetected = false;
            return false;
        }
        PlayerDetected = CanSeePlayer(castDown);
        return PlayerDetected;
    }

    // castDown makes sightRadius cylindrical (infinite height)
    protected bool CanSeePlayer(bool castDown=true){ // checks if the player can be seen
        // make sure player is in field of view
        if (!PlayerInFOV()){
            return false;
        }
        // make sure player in sight radius
        Vector3 dir = player.EyeLevel - EyeLevel;
        Vector3 distance = dir;
        if (castDown){
            distance.y = 0;
        }
        if (distance.magnitude > sightRadius){
            return false;
        }
        // now check if anything is blocking the view
        sightRay = new Ray(EyeLevel, dir);
        Debug.DrawRay(sightRay.origin, sightRay.direction * dir.magnitude, Color.blue); //!
        RaycastHit[] hitList = Physics.RaycastAll(sightRay, (dir.magnitude + 0.2f));
        for (int i = 0; i < hitList.Length; ++i){
            RaycastHit hit = hitList[i];
            if (environmentTags.Contains(hit.collider.tag)){
                return false;
            }
        }
        // finally, the player can be seen
        return true;
    }

    // castDown makes FOV cylindrical (infinite height) - otherwise it is conical
    protected bool PlayerInFOV(bool castDown=true){
		bool inFOV = false;
		Vector3 displacement = player.transform.position - transform.position; // from self to target
        if (castDown){
            displacement.y = 0;
        }
		float dot = Vector3.Dot(transform.forward, displacement.normalized);
		// restrict range [-1,1] - floating precision error
		if (dot > 1){
			dot = 1;
		}
		else if (dot < -1){
			dot = -1;
		}
		float angle = Mathf.Acos(dot)*180f/Mathf.PI; // degrees
		if (angle <= fov/2f){
			inFOV = true;
		}
		return inFOV;
	}

    // true if the point has been reached (within targetDistance)
    // defaults: move/turn in walking plane, turn to face destination, move as close as possible
    public bool MoveTowards(Vector3 point, float targetDistance, bool walkingPlane, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        Vector3 dir = point - BodyPosition;
        if (walkingPlane){ // cast to walking plane
            dir.y = 0;
        }
        // reached destination
        if (targetDistance <= 0){ // override closeness
            targetDistance = distanceError;
        }
        if (dir.magnitude < targetDistance){
            return true;
        }
        FaceTarget(point, rotateMode); // look at destination
        base.Move(dir, walkingPlane);
        return false;
    }
    // close as possible
    public bool MoveTowards(Vector3 point, bool walkingPlane, RotationStyle rotateMode = RotationStyle.WalkingPlane){ 
        return MoveTowards(point, distanceError, walkingPlane, rotateMode);
    }
    // cast to walking plane
    public bool MoveTowards(Vector3 point, float taretDistance, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        return MoveTowards(point, taretDistance, true, rotateMode);
    }
    // close as possible, cast to walking plane
    public bool MoveTowards(Vector3 point, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        return MoveTowards(point, distanceError, true, rotateMode);
    }

    // move with a specific speed during the motion
    public bool MoveWithSpeed(Vector3 point, float speed, float targetDistance, bool walkingPlane, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        float saveSpeed = MoveSpeed; // current
        MoveSpeed = speed;
        bool reached = MoveTowards(point, targetDistance, walkingPlane, rotateMode);
        MoveSpeed = saveSpeed; // restore speed
        return reached;
    }
    public bool MoveWithSpeed(Vector3 point, float speed, float targetDistance, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        return MoveWithSpeed(point, speed, targetDistance, true, rotateMode);
    }
    public bool MoveWithSpeed(Vector3 point, float speed, bool walkingPlane, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        return MoveWithSpeed(point, speed, distanceError, walkingPlane, rotateMode);
    }
    public bool MoveWithSpeed(Vector3 point, float speed, RotationStyle rotateMode = RotationStyle.WalkingPlane){
        return MoveWithSpeed(point, speed, distanceError, true, rotateMode);
    }

    // turn in an instant
    public void FaceTarget(Vector3 pos, RotationStyle mode){
        if (mode == RotationStyle.None){return;}
        // get direction
        Vector3 dir = pos - EyeLevel;
        if (dir.sqrMagnitude < 0.01f){ // magnitude within 0.1
            return;
        }
        // unbounds 3D space
        if (mode == RotationStyle.Space3D){
            Quaternion newRotation = Quaternion.LookRotation(dir, bodyTransform.up);
            Quaternion deltaRotation = newRotation * Quaternion.Inverse(bodyTransform.rotation); // to rotate body transform to face dir
            RotateAroundPivot(BodyPosition, deltaRotation);
        }
        // turn around y-axis
        else if (mode == RotationStyle.WalkingPlane){
            float yRotation = Quaternion.LookRotation(dir, bodyTransform.up).eulerAngles.y;
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.y = yRotation;
            Quaternion deltaRotation = Quaternion.Euler(rotation) * Quaternion.Inverse(bodyTransform.rotation);
            RotateAroundPivot(BodyPosition, deltaRotation);
        }
    }
    public void FaceTarget(Vector3 pos){ // only turn around y-axis
        FaceTarget(pos, RotationStyle.WalkingPlane);
    }

    // true if the direction has been reached
    public bool TurnTowards(Vector3 direction, bool walkingPlane){
        bool inFront = true;
        Vector3 myForward = transform.forward;
        // cast to walking plane
        if (walkingPlane){
            direction.y = 0;
            myForward.y = 0;
        }
        // get angle between this character's forward and the desired direction
        float dot = Vector3.Dot(direction.normalized, myForward.normalized); // [-1, 1]
        dot = Mathf.Clamp(dot, -1f, 1f); // precision error
        inFront = dot >= 0 ? true : false;
        float rotationOffset = Mathf.Acos(dot) * (180/Mathf.PI); // degrees [0, 180]
        // already facing direction
        if (rotationOffset < rotationError){
            return true;
        }
        // rotate to face towards the direction
        Vector3 cross = Vector3.Cross(myForward, direction); // axis defines direction of rotation
        float degrees = rotateSpeed * Time.fixedDeltaTime;
        AnchoredTurn(degrees, BodyPosition, cross); // pivot from body
        return false;
    }
    public bool TurnTowards(Vector3 direction){
        return TurnTowards(direction, true);
    }

    public void FixedTurn(bool clockwise = true){ // around y-axis
        float degrees = rotateSpeed * Time.fixedDeltaTime;
        if (!clockwise){
            degrees *= -1;
        }
        Turn(degrees);
    }

    public void SetMovementSpeed(Speed speedIndex){
        if (speedIndex == Speed.Walk){
            MoveSpeed = walkSpeed;
        }
        else{
            MoveSpeed = runSpeed;
        }
    }

    // how will the enemy attack while on path
    protected virtual void PathAttack(){
        if (CloseToAttackPlayer()){
            Attack();
        }
    }

    // what happens when the enemy finds the player
    protected virtual void ChasePlayer(){
        DirectChase();
    }
    protected void DirectChase(){ // straight to player
        SetMovementSpeed(Speed.Run);
        MoveTowards(player.transform.position);
    }

    public bool CloseTo(Vector3 target, float distance, bool walkingPlane){
        Vector3 myPos = BodyPosition;
        if (walkingPlane){
            myPos.y = 0;
            target.y = 0;
        }
        return Vector3.Distance(myPos, target) <= distance;
    }
    public bool CloseTo(Vector3 target, float distance){ // cast to walking plane
        return CloseTo(target, distance, true);
    }
    public bool CloseTo(Vector3 target, bool walkingPlane){ // close as possible
        return CloseTo(target, distanceError, walkingPlane);
    }
    public bool CloseTo(Vector3 target){ // walking plane, close as possible
        return CloseTo(target, distanceError, true);
    }

    public bool CloseToPlayer(float distance, bool walkingPlane = true){
        return CloseTo(player.BodyPosition, distance, walkingPlane);
    }
    protected bool CloseToAttackPlayer(bool walkingPlane = true){ // within attack range
        return CloseToPlayer(attackDistance, walkingPlane);
    }

    protected void Attack(){
        if (nextAttackTime <= 0){
            OnAttack();
            nextAttackTime = attackCooldown;
            attacking = true;
        }
    }
    protected virtual void OnAttack(){ // can customize
        DamagePlayer();
        if (stunOnHit){ // stun on impact
            StunPlayer();
        }
    }

    protected void CooldownAttack(){
        if (nextAttackTime > 0){
            nextAttackTime -= Time.deltaTime;
            if (nextAttackTime <= 0){
                attacking = false;
            }
        }
    }

    protected void DamagePlayer(){
        playerStatus.TakeDamage(attackDamage);
    }

    protected void StunPlayer(){
        player.Stun(stunTime);
    }

    public void UpdatePathController(EnemyPathController script){
        pathController = script;
        // check for empty path
        if (pathController && !pathController.PathExists){
            pathController = null;
        }
    }
    protected void UpdatePathController(){
        // get attached - if exists
        EnemyPathController script = gameObject.GetComponent<EnemyPathController>();
        UpdatePathController(script);
    }

    public void DisablePhysics(){
        ForgetPlayer(); // stop detecting player
        rb.isKinematic = true; // stop moving
        // leave path for good
        pathController?.ExitPath();
        pathController = null;
        // disable colliders
        Collider[] colliders = gameObject.GetComponents<Collider>();
        for (int i=0; i < colliders.Length; ++i){
            colliders[i].enabled = false;
        }
    }

    // Start is called before the first frame update
    protected override void Start(){
        base.Start();
        GameObject playerObj = GameObject.FindWithTag("Player");
        player = playerObj.GetComponent<PlayerController>();
        playerStatus = playerObj.GetComponent<PlayerStatus>();
        UpdatePathController();        
        searchTimer = 0;
        nextAttackTime = 0;
        attacking = false;
    }

    protected virtual void ChooseAnimation(){}

    // Update is called once per frame
    protected void Update(){
        ChooseAnimation();
        if (ControllerDead){
            return;
        }
        if (!playerStatus.IsSafe){ // do not even try to find him if safe
            LookForPlayer();
            HandleDetection(); // player gains or loses detection
        }
        CooldownJump();
        CooldownAttack();
        CooldownStagger();
        EnemyUpdate();
    }
    protected virtual void EnemyUpdate(){} // additional updates

	protected sealed override void FixedUpdate(){
		base.FixedUpdate();
        if (ControllerDead || IsFlinching){
            return; // cannot move
        }
        if (TrackingPlayer){ // attack phase
            if (!attackOnPath){
                ChasePlayer();
            }
            else{ // attack while following path
                PathAttack();
            }
        }
        else if (investigating){
            Investigate();
        }
        EnemyFixedUpdate();
	}
    protected virtual void EnemyFixedUpdate(){} // additional fixed updates
}
