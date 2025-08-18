using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UI;
using static PotionItem;

public class PlayerBody : MonoBehaviour, IAttackable
{
    public GameObject Player;

    public float maxHealth;
    public float attackSpeed;
    public float health;
    public float speed;
    public float mana;
    public float radius = 5f;
    public float damage = 1;
    private Slider healthSlider;
    private Slider ExpSlider;
    private bool healthSliderFound = false;
    public int experience = 0;
    public int level = 0;
    private int points;
    [SerializeField] private int experienceToNextlevel;
    public int maxLevel = 100;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;
    private PlayerMovement playerMove;
    private HandController playerhand;

    public static PlayerBody PlayerInstance;
    private void Awake()
    {
        if (PlayerInstance == null)
        {
            PlayerInstance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    void Start()
    {
        playerMove = GetComponent<PlayerMovement>();
        playerhand = GetComponent<HandController>();
        health = maxHealth;

        FindSliders();

        InitializeHealthBars();

        UpdateExperienceBar();

    }

    private void FindSliders()
    {
        if (healthSlider == null)
        {
            GameObject healthBarObject = GameObject.FindGameObjectWithTag("HealthBar");
            GameObject Experiencebar = GameObject.FindGameObjectWithTag("ExpBar");
            if (healthBarObject != null || Experiencebar != null)
            {
                healthSlider = healthBarObject.GetComponent<Slider>();
                ExpSlider = Experiencebar.GetComponent<Slider>();
                healthSliderFound = true;
            }
        }
    }
    void OnEnable()
    {
        MonsterScript.OnEnemyDeath += GainXP;
    }
    void OnDisable()
    {
        MonsterScript.OnEnemyDeath -= GainXP;
    }


    private void InitializeHealthBars()
    {
        UpdateHealthVisualization();
    }

    private void Update()
    {
        
        if (health <= 0)
        {
            Dead();
        }

        if (!healthSliderFound)
        {
            FindSliders();
            if (healthSliderFound)
            {
                InitializeHealthBars();
            }
        }

        UpdateHealthVisualization();
    }

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0, health - amount);
        Debug.Log($"Player took {amount} damage, health now: {health}");
    }

    private void Dead()
    {
        Debug.Log("Player died!");
        Destroy(gameObject);
    }

    private void UpdateHealthVisualization()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
            Debug.Log($"Health Updated: {health}/{maxHealth}");
        }
    }
    private void UpdateExperienceBar()
    {
        if (ExpSlider != null)
        {
            ExpSlider.maxValue = experienceToNextlevel;
            ExpSlider.value = experience;
            if (experience >= experienceToNextlevel)
            {
                Levelup();
            }                   
        }
    }
    void GainXP(int amount)
    {
        experience += amount;
        UpdateExperienceBar();
    }
    private void Levelup()
    {
        level++;
        points++;
        levelText.text = "Level " + level.ToString(); 
        experience -= experienceToNextlevel;
        experienceToNextlevel = Mathf.RoundToInt(experienceToNextlevel * 1.1f + 10f);
        UpdateExperienceBar();
    }
    public void StatisticUpdate(string abilityName)
    {
        if ( points <= 0 )
        {
            return;
        }
        switch (abilityName)
        {
            case "Health":
                print("sigma");
                MaxHealthLevelUp();
                break;

            case "Stronge":
                StrongeLevelUp();
                break;

            case "Speed":
                SpeedLevelUp();
                break;

            case "AtkSpeed":
                AtkSpeedLevelUp();
                break;

            default:
                Debug.LogWarning("Unknown ability: " + abilityName);
                break;
        }
    }
    private void MaxHealthLevelUp()
    {
        points--;
        maxHealth++;
        print(maxHealth.ToString());
    }
    private void StrongeLevelUp()
    {
        points--;
        damage++;
    }
    private void SpeedLevelUp()
    {
        points--;
        playerMove.playerSpeed++;
    }
    private void AtkSpeedLevelUp()
    {
        points--;
        attackSpeed++;
    }
    public void PotionUse(float amount, PotionsType type)
    {
        switch (type)
        {   
            case PotionsType.Healthpotion:
                if(health == maxHealth)
                {
                    break;
                }
                if (health + amount > maxHealth)
                {
                    health = maxHealth;
                    break;
                }
                else
                {
                    health += amount;
                }
                break;
            case PotionsType.Manapotion:
                mana += amount;
                break;
            default:
                break;
        }
    }
}