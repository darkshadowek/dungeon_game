using UnityEngine;
using UnityEngine.UI;

public class PlayerBody : MonoBehaviour, IAttackable
{
    [SerializeField] private float maxHealth;
    public float attackSpeed;
    private float health;
    private Slider healthSlider;
    private bool healthSliderFound = false;
    void Start()
    {
        // Ustaw pocz�tkowe zdrowie
        health = maxHealth;

        // Znajd� health slider
        FindHealthSlider();

        // Inicjalizuj paski zdrowia
        InitializeHealthBars();
    }

    private void FindHealthSlider()
    {
        if (healthSlider == null)
        {
            GameObject healthBarObject = GameObject.FindGameObjectWithTag("HealthBar");
            if (healthBarObject != null)
            {
                healthSlider = healthBarObject.GetComponent<Slider>();
                healthSliderFound = true;
            }
        }
    }

    private void InitializeHealthBars()
    {
        UpdateHealthVisualization();
    }

    private void Update()
    {
        // Sprawd� �mier�
        if (health <= 0)
        {
            Dead();
        }

        // Je�li nie znale�li�my jeszcze health slidera, spr�buj ponownie
        if (!healthSliderFound)
        {
            FindHealthSlider();
            if (healthSliderFound)
            {
                InitializeHealthBars();
            }
        }

        // Aktualizuj paski zdrowia
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
        // Tutaj mo�esz doda� logik� �mierci (restart poziomu, menu game over, itp.)
        Destroy(gameObject);
    }

    private void UpdateHealthVisualization()
    {
        // Aktualizuj UI health bar
        if (healthSlider != null)
        {
            if (Mathf.Abs(healthSlider.value - health) > 0.01f)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = health;
                Debug.Log($"Health Updated: {health}/{maxHealth}");
            }
        }
    }
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    // Publiczne w�a�ciwo�ci dla �atwego dost�pu
    public float Health => health;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => health / maxHealth;
}