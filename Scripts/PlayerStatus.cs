using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : CharacterStatus
{
    [SerializeField] private GameObject victoryScreenPrefab;
    private Text hpText;
    private Text soulsText;
    private Text detectedText;
    private PlayerController playerController;

    private int totalSouls;
    private int soulsCollected;
    private bool soulsHidden;
    private GameObject[] souls;

    private bool portalOpened;
    private PortalController portal;

    private int enemyCount = 0; // how many enemies can see me

    private static string safeTag = "SafeEnvironment";
    private bool safety;

    public bool IsSafe{
        get {return safety;}
    }

    public void GainDetection(){
        enemyCount += 1;
    }

    public void LoseDetection(){
        enemyCount -= 1;
        if (enemyCount < 0){
            enemyCount = 0;
        }
    }

    public void LoseAllDetection(){
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies){
            EnemyController controller = enemy.GetComponent<EnemyController>();
            controller.ForgetPlayer(); // only if enemy can see me
        }
    }

    public bool CanTargetEnemy(EnemyController enemy){
        if (enemy is RobotController){
            return false; // cannot hit
        }
        return true;
    }

    public List<GameObject> FindCloseEnemies(float radius = 30f){
        Collider[] colliders = Physics.OverlapSphere(playerController.BodyPosition, radius);
        List<GameObject> enemies = new List<GameObject>();
        for (int i=0; i < colliders.Length; ++i){
            if (colliders[i].CompareTag("Enemy")){
                enemies.Add(colliders[i].gameObject);
            }
        }
        return enemies;
    }

    public void AttractEnemies(float radius = 40f){
        List<GameObject> enemies = FindCloseEnemies(radius);
        foreach (GameObject enemy in enemies){
            EnemyController controller = enemy.GetComponent<EnemyController>();
            controller.InvestigateScene(transform.position);
        }
    }

    private void CheckSafety(){ // on a safe platform
        Transform ground = playerController.CurrentGround;
        if (ground == null){ // in air, current safety remains
            return;
        }
        safety = false;
        if (ground.CompareTag(safeTag)){
            safety = true;
        }
    }

    private void OpenPortal(){
        if (!portalOpened){
            portal.Activate();
            portalOpened = true;
        }
    }

    public void collectSoul(){
        ++soulsCollected;
    }

    private bool CollectedAllSouls(){ // collected every soul in this level
        if (soulsCollected >= totalSouls){
            return true;
        }
        return false;
    }

    private void SetSoulsVisibility(bool visible){ // true to show
        if (visible == soulsHidden){
            for (int i = 0; i < souls.Length; i++) {
                GameObject currentSoul = souls[i];
                if (!currentSoul.GetComponent<SoulController>().IsCloaked){ // cannot change visibility
                    continue;
                }
                currentSoul.SetActive(visible); // hide or show
            }
            soulsHidden = !visible;
        }
    }
    private void HideSouls(){
        SetSoulsVisibility(false);
    }
    private void RevealSouls(){
        SetSoulsVisibility(true);
    }

    public void FindSouls(){ // update souls on scene
        souls = GameObject.FindGameObjectsWithTag("Soul");
    }

    private void InitializeSouls(){
        soulsCollected = 0;
        FindSouls(); // find all souls for this level
        totalSouls = souls.Length;
        soulsHidden = false;
    }

    private void FindPortal(){ // get reference
        GameObject portalObject = GameObject.FindWithTag("Portal");
        portal = null;
        if (portalObject){
            portal = portalObject.GetComponent<PortalController>();
        }
        portalOpened = (portal == null); // treat portal as open if it DNE
    }
    
    override protected void OnDeath(){
        playerController.ChooseAnimation(); // start death animation
        GameObject deathScreen = new GameObject("DeathScreen");
        DeathScreen script = deathScreen.AddComponent<DeathScreen>();
        script.Initialize(1.5f, decayTime, 0.95f);
    }
    
    private void UpdateTextStatus(){
        int hp = (int) Mathf.Ceil(Health);
        if (hp < 0){hp = 0;}
        hpText.text = $"HP:  {hp}/{Mathf.Ceil(maxHealth)}";
        detectedText.text = $"Detected:  {enemyCount, 2}";
        if (soulsCollected >= totalSouls){
            soulsText.text = "Complete";
        }
        else{
            soulsText.text = $"Souls:  {soulsCollected}/{totalSouls}";
        }
    }

    public void LoadVictoryScreen(){
        GameObject.Instantiate(victoryScreenPrefab);
    }

    // Start is called before the first frame update
    override protected void Start(){
        base.Start();
        // get text references
        GameObject statsObj = GameObject.Find("PlayerStats");
        Text[] textComponents =  statsObj.GetComponentsInChildren<Text>();
        hpText = textComponents[0];
        detectedText = textComponents[1];
        soulsText = textComponents[2];
        playerController = gameObject.GetComponent<PlayerController>();
        InitializeSouls();
        FindPortal();
    }

    // Update is called once per frame
    override protected void Update(){
        base.Update();
        UpdateTextStatus();

        // handle safe platforms
        CheckSafety();
        if (IsSafe && enemyCount > 0){
            LoseAllDetection();
        }
        
        // handle souls
        if (enemyCount > 0){
            HideSouls();
        }
        else{
            RevealSouls();
        }

        // open portal
        if (CollectedAllSouls()){
            OpenPortal();
        }
    }
}

// Gameover Class
public class DeathScreen : MonoBehaviour
{
    private Canvas blackScreen;
    private Image background;
    private Color bgColor;
    private Text gameOverText;
    private float fadeDuration;
    private float lingerDuration;
    private float maxAlpha;
    private float timer;
    private int phase;

    public void Initialize(float fadeDuration, float lingerDuration, float maxAlpha){
        this.fadeDuration = fadeDuration;
        this.lingerDuration = lingerDuration;
        this.maxAlpha = maxAlpha;
        timer = 0;
        phase = 0;
        // set up the screen
        blackScreen = gameObject.AddComponent<Canvas>();
        blackScreen.renderMode = RenderMode.ScreenSpaceOverlay;
        background = gameObject.AddComponent<Image>();
        bgColor = new Color(0, 0, 0, 0);
        background.color = bgColor;
        // create text
        GameObject textObj = new GameObject("GameOver");
        textObj.transform.SetParent(transform);
        gameOverText = textObj.AddComponent<Text>();
        gameOverText.color = new Color(0.74f, 0f, 0f, 1f); // Dark red
        gameOverText.fontSize = 90;
        gameOverText.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // get default font
        gameOverText.text = "Game Over";
        gameOverText.rectTransform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        gameOverText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gameOverText.rectTransform.sizeDelta = new Vector2(1000, 400); // Set the size of the bounding box
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.enabled = false;
    }
    public void Initialize(float fadeDuration, float lingerDuration){
        Initialize(fadeDuration, lingerDuration, 1f);
    }
    public void Initialize(float fadeDuration){
        Initialize(fadeDuration, 3f, 1f);
    }
    public void Initialize(){
        Initialize(1.5f, 3f, 1f);
    }

    private bool IsFinished(){
        return (phase > 1);
    }

    private void Tick(){
        if (IsFinished()){
            return;
        }
        timer += Time.unscaledDeltaTime;
        if (phase == 0){
            if (timer >= fadeDuration){
                timer = 0;
                phase += 1;
            }
        }
        else{
            if (timer >= lingerDuration){
                phase += 1;
            }
        }
    }

    private void Fade(){
        float alpha = maxAlpha;
        if (phase == 0){
            alpha *= (timer / fadeDuration);
        }
        bgColor.a = alpha;
        background.color = bgColor;
    }

    void Start(){
        if (blackScreen == null){
            Initialize();
        }
        Time.timeScale = 0; // freeze game
    }

    void Update(){
        Tick(); // timer
        Fade();
        if (phase > 0){
            gameOverText.enabled = true;
        }
        // remove
        if (IsFinished()){
            Time.timeScale = 1; // unfreeze game
            LevelManager.ReloadLevel();
        }
    }
}