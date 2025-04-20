using UnityEngine;

public class StingerController : MonoBehaviour
{
    [SerializeField] private float stunTime = 1.2f;
    [SerializeField] private bool stopOnStun = true; // stops while the player is stunned
    [Header("Surface Detection")]
    [SerializeField] private float scanRadius = 2.5f;
    [SerializeField] private bool stickToSurface = true;

    private PlayerController player;
    private SimpleMover mover;
    private float timer;
    private Vector3 ScanOrigin{ // center of scan sphere - not stinger
        get {return transform.position + ((scanRadius / 2f) * transform.up);}
    }
    private Vector3 Center{ // center of stinger (portal)
        get {return transform.position;}
    }

    void GetSurface(){
        Collider[] colliders = Physics.OverlapSphere(ScanOrigin, scanRadius);
        // choose closest surface to stick to
        float closestDistance = Mathf.Infinity;
        Vector3 surfaceNormal = Vector3.zero;
        Vector3 surfacePos = Vector3.zero;
        foreach (Collider col in colliders){
            if (col.CompareTag("Environment")){ // ignore safe platforms
                // find normal
                RaycastHit hit;
                Vector3 closestPoint = col.ClosestPoint(ScanOrigin); // point on collider that is closest to the scanner
                Vector3 scanDir = closestPoint - ScanOrigin;
                //Debug.DrawRay(ScanOrigin, scanDir.normalized * scanRadius, Color.cyan); //!
                if (Physics.Raycast(ScanOrigin, scanDir, out hit, scanRadius)){ // does not fire if inside collider - dir=Vector3.Zero
                    float distance = scanDir.magnitude; // distance to stinger
                    if (distance < closestDistance){ // new closest surface
                        closestDistance = distance;
                        surfaceNormal = hit.normal;
                        surfacePos = hit.point;
                    }
                }
            }
        }
        // set normal to align with surface and attach to it
        if (surfaceNormal != Vector3.zero){
            Quaternion deltaRotation = Quaternion.FromToRotation(transform.up, surfaceNormal);
            transform.rotation = deltaRotation * transform.rotation;
            transform.position = surfacePos; // attach
        }
    }

    void Start(){
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        mover = gameObject.GetComponent<SimpleMover>();
        timer = 0;
    }

    void Update(){
        if (timer > 0){ // wait with player
            timer -= Time.deltaTime;
        }
        else{
            // continue path
            if (mover != null && !mover.OnPath){
                mover.ResumePath();
            }
        }
    }

    void FixedUpdate(){
        if (stickToSurface){
            GetSurface();
        }
    }

    void OnTriggerEnter(Collider target){
        if (target.CompareTag("Player")){
            if (stopOnStun && player.Stun(stunTime)){ // was stunned
                timer = player.StunRemaining;
                mover.ExitPath();
            }
        }
    }
}
