using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] CharacterStatus status;

    private Camera cam;
    private Transform sliderObject;
    private Slider slider;

    // Start is called before the first frame update
    void Start(){
        cam = Camera.main;
        sliderObject = transform.Find("Slider");
        slider = sliderObject.GetComponent<Slider>();

        slider.value = GetPercentHealth();
        sliderObject.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = GetPercentHealth();
        
        // always face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        // only show when damaged
        if (status.Health < status.MaxHealth){
            sliderObject.gameObject.SetActive(true);
        }
    }

    float GetPercentHealth(){
        if (status.Health < 0){
            return 0;
        }
        return (status.Health/status.MaxHealth);
    }
}
