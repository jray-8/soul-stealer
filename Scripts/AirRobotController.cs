using UnityEngine;

public class AirRobotController : RobotController
{
    // abilities
    [Header("Flight")]
    [SerializeField] private Transform scanPlane; // xz plane that this robot will fly in
    [SerializeField] private float descentRadius = 7f; // start to descend
    [SerializeField] private float climbSpeed = 6f; // to ascend over objects
    private float scanHeight;

    private bool returningToPlane;

    // flying above objects
    private float ascentTime = 0.7f; // how long will the upwards thrust last
    private float ascentCountdown;

    private bool IsAscending{
        get {return ascentCountdown > 0;}
    }

    // animation states
    private const string ROBOT_FLOAT = "Float";

    override protected void Start(){
        base.Start();
        MakeLevitate(); // float
        if (scanPlane == null){
            scanPlane = bodyTransform; // current height
        }
        scanHeight = scanPlane.position.y;
        returningToPlane = true;
        ascentCountdown = 0;
    }

    protected override void OnLosePlayer(){
        base.OnLosePlayer();
        returningToPlane = true;
        ResetTilt();
    }

    protected override void OnDetectPlayer(){
        base.OnDetectPlayer();
        returningToPlane = false;
        TiltDown();
    }

    private void SetTilt(float degrees){
        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.x = degrees;
        transform.rotation = Quaternion.Euler(rotation);
    }
    private void TiltDown(float degrees=22f){
        SetTilt(degrees);
    }
    private void TiltUp(float degrees=22f){
        SetTilt(-degrees);
    }
    private void ResetTilt(){
        SetTilt(0);
    }

    // handle default vertical height
    private void FlyToScanPlane(){
        Vector3 verticalTarget = BodyPosition;
        verticalTarget.y = scanHeight;
        if (MoveWithSpeed(verticalTarget, climbSpeed, 1.5f, false, RotationStyle.None)){ // do not face pos
            returningToPlane = false;
        }
    }

    // attack phase
    protected override void ChasePlayer(){
        SetMovementSpeed(Speed.Run);
        // handle descent
        if (CloseToPlayer(descentRadius) && (!IsAscending)){
            MoveTowards(player.BodyPosition, false, RotationStyle.WalkingPlane); // unbound 3D motion
        }
        else{ // chase from plane
            MoveTowards(player.BodyPosition);
        }
        base.ChasePlayer();
    }

    protected override void EnemyFixedUpdate(){
        if (returningToPlane){ // set elevation
            FlyToScanPlane();
        }
        // detect blocks - from lower body, slightly upwards (detects walls it might get stuck on)
        Vector3 sensorOrigin = EyeLevel - (2f * Vector3.up) - bodyTransform.forward;
        forwardRay = new Ray(sensorOrigin, BodyForward + (0.6f * Vector3.up));
        if (DetectObstacles(forwardRay)){ // must start ascending
            ascentCountdown = ascentTime;
        }
        // handle ascent
        if (IsAscending){
            ascentCountdown -= Time.fixedDeltaTime;
            // independent of other motion
            MoveWithSpeed(BodyPosition + (Vector3.up * 3f), climbSpeed, false, RotationStyle.None); // do not rotate
        }
    }

    protected override void ChooseAnimation(){
        // just float
    }

	protected override void EnemyUpdate(){
		base.EnemyUpdate();
	}
}