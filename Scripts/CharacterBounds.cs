using UnityEngine;

public class CharacterBounds : MonoBehaviour
{
    // get the total bounds of all rendered components inside a game object
    public static Bounds GetBounds(GameObject target){
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers){
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }
}
