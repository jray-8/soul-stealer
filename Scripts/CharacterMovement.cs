using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private float moveSpeed;
    [Header("Movement")]
    [SerializeField] protected float walkSpeed = 4f;
    [SerializeField] protected float runSpeed = 10f;
    [SerializeField] protected float rotateSpeed = 60f; // degrees/sec
    [SerializeField] protected float jumpForce = 6.4f;
    [Range(0.2f, 20f)]
    [SerializeField] protected float jumpCooldown = 0.4f; // prevent character from jumping multiple times before leaving the ground
    [SerializeField] protected float gravity = -21f;
    [SerializeField] protected float airDrag = 1f;
    [SerializeField] protected float groundDrag = 5f;
    [SerializeField] protected float flinchTime = 0.4f;
    [SerializeField] protected Transform bodyTransform; // the offset transform of the character
    [SerializeField] protected float eyeHeight; // where are the eyes relative to the body

    public float MoveSpeed{
        get {return moveSpeed;}
        protected set {
            if (value <= 0){moveSpeed = 0;}
            else {moveSpeed = value;}
        }
    }

    public Vector3 FlatVelocity{
        get {return FlattenVelocity(rb.velocity);}
    }

    public float HorizontalSpeed{
        get {return FlatVelocity.magnitude;}
    }

    // recoil
    protected float flinchRecovery; // countdown
    protected bool IsFlinching{
        get {return flinchRecovery > 0;}
    }

    // jumping
    private bool jumping = true;
    protected float nextJumpTime; // time until the character can jump again
    public bool IsJumping{
        get {return jumping;}
    }

    // jump is cooled down
    public bool JumpReady{
        get {return nextJumpTime <= 0;}
        protected set {
            if (value == true){ // instant refresh
                nextJumpTime = 0;
            }
            else{
                nextJumpTime = jumpCooldown;
            }
        }
    }

    // able to launch from ground
    public bool CanJump{
        get {return IsGrounded && JumpReady;}
    }

    private float fallHeightLimit = -50f;
    private float impulseScale = 3f; // scale all instant forces by this (overcome drag)

    protected Rigidbody rb;
    protected AnimationManager animManager;
    protected CharacterStatus status;

    private bool levitates = false; // floats in air (no gravity)

    public bool Levitates{
        get {return levitates;}
    }

    // ground detection
    private bool grounded = false;
    private float groundDistance = 0.1f; // distance character's feet can be from the ground to be grounded
    private Ray downwardRay;
    private Vector3 previousPosition; // from last frame
    private Transform currentGround = null;

    public Transform CurrentGround{
        get {return currentGround;}
    }

    // list of tags that represent physical objects (can be stood on, blocks line of sight)
    protected static List<string> environmentTags = new List<string>() {"Environment", "SafeEnvironment"};

    // initialization
    virtual protected void Start(){
        previousPosition = transform.position;
        moveSpeed = walkSpeed;
        rb = gameObject.GetComponent<Rigidbody>();
        animManager = gameObject.GetComponent<AnimationManager>();
        status = gameObject.GetComponent<CharacterStatus>(); // or any descendent of CharacterStatus
        nextJumpTime = 0;
    }

    // properties
    public bool IsGrounded{
        get {return grounded;}
    }

    public Vector3 BodyPosition{
        get {return bodyTransform.position;}
    }

    public Vector3 BodyForward{
        get {return bodyTransform.forward;}
    }

    public Vector3 EyeLevel{
        get {return (bodyTransform.position + (bodyTransform.up * eyeHeight));}
    }

    // physics
    protected void MakeLevitate(){
        levitates = true;
        grounded = false;
    }

    protected void ApplyDrag(){
        if (IsGrounded){
            rb.drag = groundDrag;
        }
        else{
            rb.drag = airDrag;
        }
    }

    protected void Knockback(CharacterMovement target, float power, float pitch = -45f){
        Vector3 direction = Quaternion.AngleAxis(pitch, bodyTransform.right) * BodyForward;
        Vector3 force = direction.normalized * power;
        //Debug.DrawRay(BodyPosition, force, Color.yellow, 0.5f); //!
        target.TakeKnockback(force);
    }
    public virtual void TakeKnockback(Vector3 knockbackForce){
        Stagger();
        rb.AddForce(knockbackForce * impulseScale, ForceMode.VelocityChange);
    }

    public virtual void Stagger(){ // momentary inability to act
        flinchRecovery = flinchTime;
        rb.velocity = Vector3.zero; // cease all motion
    }
    protected void CooldownStagger(){
        flinchRecovery -= Time.deltaTime;
    }

    protected void JumpTakeOff(){
        StopFalling(); // stop downward y velocity
        Vector3 jumpVelocity = Vector3.up * jumpForce * impulseScale;
        rb.AddForce(jumpVelocity, ForceMode.VelocityChange);
    }
    protected void CooldownJump(){
        if (nextJumpTime > 0){
            nextJumpTime -= Time.deltaTime;
        }
    }
    public void Jump(){ // tries to jump
        if (CanJump){
            JumpTakeOff(); // launch
            nextJumpTime = jumpCooldown; // start cooldown
            jumping = true;
            OnJump(); // additional features
        }
    }
    protected virtual void OnJump(){}

    protected void Land(){
        if (jumping && CanJump){ // landed
            jumping = false;
            OnLand();
        }
    }
    protected virtual void OnLand(){}

    public void StopFalling(){ // restricts negative y-velocity
        if (rb.velocity.y < 0){
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
    }

    protected void Fall(){
        // free fall
        if (transform.position.y > fallHeightLimit){
            Vector3 gForce = Vector3.up * gravity * Time.fixedDeltaTime;
            rb.AddForce(gForce, ForceMode.VelocityChange);
        }
    }

    protected void RotateAroundPivot(Vector3 pivot, Quaternion deltaRotation){
        // apply world rotation
        rb.MoveRotation(deltaRotation * rb.rotation);
        // maintain distance from pivot
        Vector3 relativePosition = (rb.position - pivot);
        if (relativePosition.sqrMagnitude > 0.04f){ // must be a considerable pivot
            rb.position = (deltaRotation * relativePosition) + pivot;
        }
    }

    // applies world rotation around an axis
    protected void Turn(float degrees, Vector3 axis){
        // right turn - rotate clockwise [+]
        // left turn - rotate ccw [-]
        Quaternion deltaRotation = Quaternion.AngleAxis(degrees, axis);
        rb.MoveRotation(deltaRotation * rb.rotation); // does not apply immediately
    }
    protected void Turn(float yDegrees){ // rotate around world y-axis
        Turn(yDegrees, Vector3.up);
    }

    // turn relative to a pivot point
    protected void AnchoredTurn(float degrees, Vector3 pivot, Vector3 axis){
        Quaternion deltaRotation = Quaternion.AngleAxis(degrees, axis);
        RotateAroundPivot(pivot, deltaRotation);
    }
    protected void AnchoredTurn(float degrees, Vector3 pivot){ // world y-axis
        AnchoredTurn(degrees, pivot, Vector3.up);
    }

    // check if the player is moving too fast for movment in a certain direction
    protected Vector3 RestrictControlSpeed(Vector3 moveVel, bool walkingPlane = true){
        Vector3 currentVelocity = rb.velocity;
        if (walkingPlane){ // use ground velocity - can move at full speed horizontally, independednt of vertical forces
            currentVelocity = FlattenVelocity(rb.velocity);
        }
        // stop the velocity's motion in this direction
        if (currentVelocity.magnitude >= moveSpeed){
            return RemoveComponent(moveVel, currentVelocity);
        }
        return moveVel;
    }

    // cast velocity to the walking plane
    protected Vector3 FlattenVelocity(Vector3 v){
        return new Vector3(v.x, 0, v.z);
    }

    // check if a direction is mostly horizontal / vertical
    private bool CheckDirection(Vector3 dir, bool isVertical, float threshold){
        float dot = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up));
        if (isVertical){
            return (dot >= threshold); // mostly vertical
        }
        else{
            return (dot < threshold); // mostly horizontal
        }
    }
    protected bool DirectionHorizontal(Vector3 dir, float threshold = 0.5f){
        return CheckDirection(dir, false, threshold);
    }
    protected bool DirectionVertical(Vector3 dir, float threshold = 0.5f){
        return CheckDirection(dir, true, 0.5f);
    }

    // get the component of a velocity in some direction
    protected float GetProjection(Vector3 velocity, Vector3 dir){
        return Vector3.Dot(velocity, dir.normalized); 
    }

    // remove the component of a velocity in some direction
    protected Vector3 RemoveComponent(Vector3 velocity, Vector3 dir){
        // magnitude of velocity in a particular direction
        float alignedMagnitude = GetProjection(velocity, dir);
        if (alignedMagnitude <= 0){ // no component is aligned in this direction
            // we have an anti-parallel component that would boost the velocity in this (negative) direction
            return velocity;
        }
        Vector3 parallelComponent = alignedMagnitude * dir.normalized;
        return (velocity - parallelComponent);
    }

    // move in desired direction. speed is non-negative.
    protected virtual void Move(Vector3 direction, bool walkingPlane=true){
        if (walkingPlane){
            direction.y = 0;
        }
        Vector3 deltaVelocity = direction.normalized * moveSpeed * Time.fixedDeltaTime * 10f;
        // already moving too fast in a component of this direction (prevent that motion)
        deltaVelocity = RestrictControlSpeed(deltaVelocity, walkingPlane);
        // apply velocity to rigid body
        rb.AddForce(deltaVelocity, ForceMode.VelocityChange);
    }

    // become a child of some object (move with it)
    protected void StandOn(Transform ground){
        if (ground != currentGround){ // prevent over-adding passengers
            if (currentGround != null){ // remove from old platform
                MovingPlatformController mpOld = currentGround.GetComponent<MovingPlatformController>();
                if (mpOld != null){
                    mpOld.RemovePassenger(transform);
                }
            }
            if (ground != null){ // add to new platform
                MovingPlatformController mpNew = ground.GetComponent<MovingPlatformController>();
                if (mpNew != null){ 
                    mpNew.AddPassenger(transform);
                }
            }
            currentGround = ground; // update ground
        }
    }

    protected void SetVertical(float horizontalPlane){
        transform.position = new Vector3(transform.position.x, horizontalPlane, transform.position.z);
    }

    protected void DetectGround(){
        grounded = false;
        downwardRay = new Ray(bodyTransform.position, -transform.up);
        Vector3 toFeet = transform.position - bodyTransform.position;
        float hitLength = Mathf.Abs(toFeet.y) + groundDistance; // length of ray that constitutes a collision from the character
        //Debug.DrawRay(downwardRay.origin, downwardRay.direction.normalized*hitLength, Color.red); //!
        // check ground collision
        RaycastHit[] hitList = Physics.RaycastAll(downwardRay, hitLength);
        for (int i=0; i < hitList.Length; ++i){
            RaycastHit hit = hitList[i];
            if (environmentTags.Contains(hit.collider.tag)){
                grounded = true;
                StandOn(hit.transform);
                break;
            }
            // else, a non-environment collider was hit
        }
        // check passed through ground
        // ray source has fallen below ground plane without a collision last frame - due to high speeds
        if (!grounded){
            Vector3 fallDistance = (transform.position - previousPosition);
            downwardRay = new Ray(previousPosition, fallDistance); // ray from previous position to current position
            hitList = Physics.RaycastAll(downwardRay, fallDistance.magnitude);
            for (int i=0; i < hitList.Length; ++i){
                RaycastHit hit = hitList[i];
                if (environmentTags.Contains(hit.collider.tag)){
                    grounded = true;
                    // move back to the ground surface
                    SetVertical(hit.point.y + groundDistance);
                    StandOn(hit.transform);
                    break;
                }
            }
        }
        // update position
        previousPosition = transform.position;
        // free from ground
        if (!grounded){
            StandOn(null); // air
        }
    }
    
    protected void CheckWorldLimits(){
        // passed world fall limit
        if (transform.position.y <= fallHeightLimit){
            status?.ReachVoid();
            StopFalling(); // prevent any downward motion
        }
    }

    protected virtual void FixedUpdate(){
        CheckWorldLimits();
        ApplyDrag();
        if (!levitates){ // falls
            DetectGround();
            Fall();
            Land(); // end jump
        }
    }
}
