using UnityEngine;

public class EnemyStatus : CharacterStatus
{
    [SerializeField] GameObject itemDrop;

    protected override void OnDeath(){
        if (itemDrop != null){ // drop item
            Vector3 location = transform.position + (1.5f * Vector3.up);
            GameObject item = Instantiate(itemDrop, location, Quaternion.identity);
            // deactivate the controller
            EnemyController controller = gameObject.GetComponent<EnemyController>();
            controller.DisablePhysics();
        }
        base.OnDeath();
    }
}
