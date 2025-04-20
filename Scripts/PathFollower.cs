using UnityEngine;

public abstract class PathFollower : MonoBehaviour
{
    // settings
    [SerializeField] protected WaypointData[] path;
    [SerializeField] protected bool walkingPlane = true;
    [SerializeField] protected int maxCycles = -1; // negative for infinite
    [SerializeField] protected PathFollower nextPathScript = null; // implicit conversion - simply drag a gameobject with the script attached
    [Header("Actions")]
    [SerializeField] protected float restTime = 5f;

    // internal path data
    private int pathIndex;
    private int pathSize;
    private int cyclesComplete;
    private Vector3 destination;
    private bool imaginaryDestination;
    private bool onPath;

    // actions
    protected bool resting;
    protected float timer;

    // properties
    public bool PathExists{
        get {return path.Length > 0;}
    }

    public bool OnPath{
        get {return onPath;}
        protected set {onPath = value;}
    }

    public int PathSize{
        get {return pathSize;}
    }

    public int PathIndex{
        get {return pathIndex;}
        protected set { // loop around
            pathIndex = value;
            if (pathIndex < 0){
                pathIndex = (PathSize - 1);
            }
            else if (pathIndex >= PathSize - 1){
                pathIndex = 0;
            }
        }
    }

    public int CyclesComplete{
        get {return cyclesComplete;}
    }

    public WaypointData Waypoint{
        get {return path[pathIndex];}
    }

    public Vector3 CurrentDestination{
        get {return destination;}
    }

    public WaypointData.Action CurrentAction{
        get {return path[pathIndex].action;}
    }

    public bool ImaginaryDestination{
        get {return imaginaryDestination;}
    }

    // methods
    protected bool IsImaginary(WaypointData.Action action){
        return (action >= WaypointData.Action.Face);
    }

    // current waypoint physical or directional (imaginary)
    protected void CheckVirtualWaypoint(){
        WaypointData.Action action = path[pathIndex].action;
        imaginaryDestination = false;
        if (IsImaginary(action)){
            imaginaryDestination = true; // do not move there
        }
    }

    protected void LoadNextPath(){
        if (nextPathScript == null){
            return;
        }
        TransferNextPath();
    }
    // copy settings from the next path to the new script
    protected virtual void TransferNextPath(){
        PathFollower script = gameObject.AddComponent<PathFollower>();
        CopyDefaultPathSettings(script);
    }
    protected void CopyDefaultPathSettings(PathFollower script){
        script.path = nextPathScript.path;
        script.nextPathScript = nextPathScript.nextPathScript;
        script.maxCycles = nextPathScript.maxCycles;
        script.restTime = nextPathScript.restTime;
    }

    // cast to the walking plane of the follower
    protected void CastDownDestination(){
        if (walkingPlane){ // ignore vertical movement
            destination.y = transform.position.y;
        }
    }

    // choose next target location
    protected void SetDestination(){
        destination = path[pathIndex].position;
        SetApproach();
    }
    // how will the follower approach this destination
    protected virtual void SetApproach(){
        CheckVirtualWaypoint(); // physically move there or not
    }

    protected void FindNextDestination(){
        if (pathIndex >= pathSize - 1){ // finished cycle
            ++cyclesComplete;
            pathIndex = 0;
        }
        else{
            ++pathIndex;
        }
    }

    // completed all cycles - the path terminates
    protected bool CheckPathFinished(){
        if (maxCycles >= 0 && cyclesComplete >= maxCycles){
            LoadNextPath(); // load next pathing script to the follower
            Destroy(this); // remove this script
            return true;
        }
        return false;
    }

    protected virtual void Rest(){
        timer += Time.deltaTime;
        if (timer >= restTime){
            resting = false;
        }
    }

    // activate the actions of the current destination
    protected virtual void TriggerActions(){
        WaypointData.Action a = path[pathIndex].action;
        if (a == WaypointData.Action.Rest){
            resting = true;
            timer = 0;
        }
    }

    // true if any were performed
    protected virtual bool PerformActions(){
        if (resting){
            Rest();
            return true;
        }
        return false;
    }

    // when the target destination is reached
    protected void OnArrival(){
        // perform actions
        TriggerActions();
        // choose next destination
        FindNextDestination();
        SetDestination();
    }

    // move through the defined path cycle
    protected abstract void MoveOnTrack();

    // stop following current path
    public void ExitPath(){
        if (!onPath){return;} // do not exit again
        onPath = false;
        OnExit();
    }
    protected virtual void OnExit(){ // as the follower leaves their path
        resting = false; // terminate previous actions
    }

    // get back on track from the last desired destination
    public void ResumePath(){
        if (!PathExists){return;} // no path to resume
        if (onPath){return;} // already on path
        onPath = true;
        OnResume();
    }
    protected virtual void OnResume(){ // as follower gets back on track
        SetDestination();
    }

    protected virtual void Start(){
        pathIndex = 0;
        pathSize = path.Length;
        if (!PathExists){ // no path exists
            onPath = false; // force exit
            return;
        }
        SetDestination();
        resting = false;
        timer = 0;
        cyclesComplete = 0;
        onPath = true; // enter track
    }

}
