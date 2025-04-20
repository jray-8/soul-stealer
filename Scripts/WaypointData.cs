using System;
using UnityEngine;

[Serializable]
public class WaypointData
{
    public enum Action{
        // movement actions
        Walk = 0,
        Run,
        Rest, // waits at destination
        Scan, // stops to look around at destination
        Jump, // jumps straight up at the destination
        Leap, // jump towards the destination and runs there

        // stationary actions
        Face, // look at destination and rest
        FaceScan,
        Spin, // rotate in place
    }

    [SerializeField] private Transform waypoint;
    [SerializeField] public Action action;

    public Vector3 position { // hides waypoint and uses position instead
        get {return waypoint.position;}
    }

    // constructor if creating manually
    public WaypointData(Transform w, Action a){
        waypoint = w;
        action = a;
    }
    public WaypointData(Transform w) : this(w, 0){}
}