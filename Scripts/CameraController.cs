using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float minZoomDistance = 4f;
    [SerializeField]  private float maxZoomDistance = 12f;

    private float currentZoomDistance;
    private float currentYaw; // rotation around player
    private float currentPitch; // angle up/down

    private Vector3 cameraOffset; // The initial offset between the camera and player

    void Start(){
        cameraOffset = transform.position - player.position; // initial offset from player to camera
        ResetCamera();
    }

    // zoom in/out with the mouse wheel
    void GetZoom(){
        currentZoomDistance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        RestrictZoom();
    }

    void SetZoom(float zoom){
        currentZoomDistance = zoom;
        RestrictZoom();
    }

    void RestrictZoom(){
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
    }

    // relative to player's y-rotation (perfectly behind player)
    void GetRotation(){ // relative to 
        currentYaw += Input.GetAxis("Camera Horizontal") * rotateSpeed * Time.deltaTime;
    }

    // preset settings:
    void ResetRotation(){
        currentYaw = 0; // behind player
    }
    void FaceFront(){
        currentYaw = 180; // in front
    }

    void ResetPitch(){
        currentPitch = transform.eulerAngles.x; // original
    }

    void ResetZoom(){
        SetZoom(cameraOffset.magnitude); // original
    }

    // return camera to original position
    void ResetCamera(){
        ResetRotation();
        ResetPitch();
        ResetZoom();
    }

    void Update(){ // handle input
        GetZoom();
        GetRotation();

        // reset zoom
        if (Input.GetKeyDown(KeyCode.Mouse2)){
            ResetZoom();
        }

        // reset rotation of camera (behind player)
        if (Input.GetKeyDown(KeyCode.DownArrow)){
            ResetRotation();
        }
        // front facing rotation
        else if (Input.GetKeyDown(KeyCode.UpArrow)){
            FaceFront();
        }
    }

    void LateUpdate(){ // orbit around player using world y-axis as pivot
        // update camera rotation relative to the player's
        Quaternion offsetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Quaternion playerVerticalRotation = Quaternion.Euler(0, player.rotation.eulerAngles.y, 0);
        Quaternion cameraRotation = playerVerticalRotation * offsetRotation;
        transform.rotation = cameraRotation;
        // apply level of zoom in the direction of the camera's offset axis (original offset vector), from the origin (player)
        Vector3 zoomDirection = Quaternion.Euler(0,cameraRotation.eulerAngles.y,0) * (cameraOffset.normalized * currentZoomDistance);
        transform.position = player.position + zoomDirection;
    }
}