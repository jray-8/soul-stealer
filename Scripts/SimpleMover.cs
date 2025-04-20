using UnityEngine;

public class SimpleMover : PathFollower
{
    [SerializeField] protected float defaultSpeed = 2f;
    protected float distanceError = 0.15f;

    protected override void TransferNextPath(){
        SimpleMover script = gameObject.AddComponent<SimpleMover>();
        CopyDefaultPathSettings(script);
        // copy new settings from the next path
        if (nextPathScript is SimpleMover){
            SimpleMover nextMover = (SimpleMover) nextPathScript;
            script.defaultSpeed = nextMover.defaultSpeed;
        }
    }

    protected override void MoveOnTrack(){
        // move to the next destination
        CastDownDestination(); // if walking plane
        transform.position = Vector3.MoveTowards(transform.position, CurrentDestination, defaultSpeed * Time.deltaTime);
        // check if the platform has reached its destination
        if (Vector3.Distance(transform.position, CurrentDestination) < distanceError){
            OnArrival();
        }
    }

    protected virtual void Update(){
        if (!OnPath){return;}
        if (PerformActions()){
            return;
        }
        if (CheckPathFinished()){
            return;
        }
        MoveOnTrack();
    }
}