using UnityEngine;

public class EnemyPathController : PathFollower
{
    [SerializeField] private float lookTime = 2f;
    [SerializeField] private float spinTime = 3f;
    [SerializeField] private float scanArc = 90f; // degrees 
    [SerializeField] private bool scanLeftToRight = true; // or right to left

    private bool looking;
    private bool spinning;
    private bool scanning;
    private Vector3[] scanDirections; // 0-left, 1-center, 2-right
    private int scanIndex;
    private bool waitingToJump;
    private bool waitingToLand;
    private bool leaping;

    private EnemyController controller;

    public bool IsScanning{
        get {return scanning;}
    }
    public bool IsSpinning{
        get {return spinning;}
    }
    public bool IsLooking{
        get {return looking;}
    }

    protected override void TransferNextPath(){
        EnemyPathController script = gameObject.AddComponent<EnemyPathController>();
        CopyDefaultPathSettings(script);
        // copy new settings from the next path
        if (nextPathScript is EnemyPathController){
            EnemyPathController nextEnemyPath = (EnemyPathController) nextPathScript;
            script.lookTime = nextEnemyPath.lookTime;
            script.scanArc = nextEnemyPath.scanArc;
        }
        controller.UpdatePathController(script); // share reference to the new script
    }

	protected override void SetApproach(){
        // speed
        if (CurrentAction == WaypointData.Action.Run || CurrentAction == WaypointData.Action.Leap){
            controller.SetMovementSpeed(EnemyController.Speed.Run);
        }
        else{ // walk instead
            controller.SetMovementSpeed(EnemyController.Speed.Walk);
        }
        // leap there
        if (CurrentAction == WaypointData.Action.Leap){
            leaping = true;
        }
        CheckVirtualWaypoint(); // move there?
    }

    // sets path index to the first previous position that was not imaginary
    private void FindLastRealDestination(){
        int maxChecks = PathSize;
        int checks = 0;
        WaypointData.Action a = CurrentAction;
        while (IsImaginary(a)){
            --PathIndex; // backtrack
            ++checks;
            if (checks >= maxChecks){ // back to where we started - no real waypoints found
                return;
            }
            a = CurrentAction; // action of new waypoint
        }
    }

    private void JumpWhenReady(bool wait = false){ // wait for jump to complete (land)
        if (controller.CanJump){
            controller.Jump();
            // done
            if (wait){
                waitingToJump = false;
                waitingToLand = true;
            }
            else{
                leaping = false;
            }
        }
    }
    private void WaitForLand(){
        if (controller.IsGrounded){
            waitingToLand = false;
        }
    }

    protected override void Rest(){ // fixed rest
        timer += Time.fixedDeltaTime;
        if (timer >= restTime){
            resting = false;
        }
    }

    private void Look(){
        timer += Time.fixedDeltaTime;
        if (timer >= lookTime){
            looking = false;
        }
    }

    private void Spin(){
        controller.FixedTurn(scanLeftToRight);
        timer += Time.fixedDeltaTime;
        if (timer >= spinTime){
            spinning = false;
        }
    }

    private void Scan(){
        if (controller.TurnTowards(scanDirections[scanIndex], walkingPlane)){ // reached direction
            // choose next direction
            if (scanLeftToRight){
                NextScanLeftToRight();
            }
            else{
                NextScanRightToLeft();
            }
        }
    }

    private void NextScan(int from, int to, int end){
        // custom mapping: from -> to -> end
        if (scanIndex == from){
            scanIndex = to;
        }
        else if (scanIndex == to){
            scanIndex = end;
        }
        else if (scanIndex == end){
            scanning = false;
        }
        else{ // unknown
            Debug.Log("undefined scanIndex");
            scanning = false;
        }
    }
    private void NextScanLeftToRight(){ // clear name
        NextScan(0,2,1);
    }
    private void NextScanRightToLeft(){
        NextScan(2,0,1);
    }

    private void SetScanDirections(){
        Vector3 originDir = CurrentDestination - controller.EyeLevel; // sight to waypoint
        float angle = scanArc/2;
        Quaternion leftRotation = Quaternion.Euler(Vector3.up * -angle);
        Quaternion rightRotation = Quaternion.Euler(Vector3.up * angle);
        scanDirections[0] = leftRotation * originDir; // world rotation
        scanDirections[1] = originDir;
        scanDirections[2] = rightRotation * originDir;
        //DebugScanDirections(); //!
    }
    private void DebugScanDirections(){
        Debug.DrawRay(controller.BodyPosition, scanDirections[0], Color.magenta, 2f);
        Debug.DrawRay(controller.BodyPosition, scanDirections[1], Color.yellow, 2f);
        Debug.DrawRay(controller.BodyPosition, scanDirections[2], Color.magenta, 2f);
    }

    protected override void OnExit(){
        resting = false; // previous actions terminate when the path resumes
        looking = false;
        scanning = false;
    }

    protected override void OnResume(){ // get back on track from the last desired destination
        FindLastRealDestination(); // where to re-enter the track
        SetDestination();
    }

    protected override void Start(){
        controller = gameObject.GetComponent<EnemyController>();
        base.Start();
        resting = false;
        looking = false;
        scanning = false;
        scanDirections = new Vector3[3];
        scanIndex = 0;
        timer = 0;
        waitingToJump = false;
        waitingToLand = false;
        leaping = false;
    }

    protected override void TriggerActions(){
        WaypointData.Action action = CurrentAction;
        // face direction of waypoint
        if (action == WaypointData.Action.Face || action == WaypointData.Action.FaceScan){
            controller.FaceTarget(CurrentDestination);
        }
        // perform action
        if (action == WaypointData.Action.Rest){
            resting = true;
            timer = 0;
        }
        else if (action == WaypointData.Action.Face){
            looking = true;
            timer = 0;
        }
        else if (action == WaypointData.Action.Scan || action == WaypointData.Action.FaceScan){
            scanning = true;
            scanIndex = 0;
            SetScanDirections();
        }
        else if (action == WaypointData.Action.Spin){
            spinning = true;
            timer = 0;
        }
        else if (action == WaypointData.Action.Jump){
            waitingToJump = true;
        }
        // if still trying to leap - cancel
        leaping = false;
    }

    protected override bool PerformActions(){
        bool actionPerformed = true;
        if (resting){
            Rest();
        }
        else if (looking){
            Look();
        }
        else if (scanning){
            Scan();
        }
        else if (spinning){
            Spin();
        }
        else if (waitingToJump){
            JumpWhenReady(wait: true);
        }
        else if (waitingToLand){
            WaitForLand();
        }
        // no action was performed
        else{
            actionPerformed = false;
        }
        return actionPerformed;
    }

    protected override void MoveOnTrack(){
        // reached its destination
        if (ImaginaryDestination || controller.MoveTowards(CurrentDestination, walkingPlane)){
            OnArrival();
        }
        // perform a single leap there
        if (leaping){
            JumpWhenReady(wait: false);
        }
    }

    void FixedUpdate(){
        if (!OnPath){return;}
        // non-moving actions
        if (PerformActions()){
            return;
        }
        if (CheckPathFinished()){
            return;
        }
        MoveOnTrack();
    }
}
