using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformController : SimpleMover
{
    [SerializeField] private float boostedSpeed = 4f;
    private float moveSpeed;

    private Vector3 previousPos;
    private List<Transform> passengers;

    // usuable WaypointData.Actions:
    // walk - switch to default speed
    // run - switch to boosted speed
    // rest - wait at location

    protected override void TransferNextPath(){
        MovingPlatformController script = gameObject.AddComponent<MovingPlatformController>();
        CopyDefaultPathSettings(script);
        // copy new settings from the next path
        if (nextPathScript is MovingPlatformController){
            MovingPlatformController nextPlatformPath = (MovingPlatformController) nextPathScript;
            script.boostedSpeed = nextPlatformPath.boostedSpeed;
            // transfer passengers
            script.passengers = nextPlatformPath.passengers;
        }
    }

    protected override void SetApproach(){
        // set speed
        if (CurrentAction == WaypointData.Action.Walk){
            moveSpeed = defaultSpeed;
        }
        else if (CurrentAction == WaypointData.Action.Run){
            moveSpeed = boostedSpeed;
        }
    }

    // let platform carry people
    public void AddPassenger(Transform p){
        passengers.Add(p);
    }

    public void RemovePassenger(Transform p){
        passengers.Remove(p);
    }

    void MovePassengers(Vector3 movement){
        for (int i=0; i < passengers.Count; ++i){
            Transform nextPassenger = passengers[i];
            nextPassenger.position += movement;
        }
    }

    protected override void Start(){
        base.Start();
        previousPos = transform.position;
        passengers = new List<Transform>();
    }

    protected override void MoveOnTrack(){
        // move to the next destination
        transform.position = Vector3.MoveTowards(transform.position, CurrentDestination, moveSpeed * Time.deltaTime);
        Vector3 deltaPosition = transform.position - previousPos;
        MovePassengers(deltaPosition);
        // check if the platform has reached its destination
        if (Vector3.Distance(transform.position, CurrentDestination) < distanceError){
            OnArrival();
        }
    }
}