using UnityEngine;
using UnityEngine.UI;

public class MonsterScript : MonoBehaviour, IAttackable
{
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float speed;
    [SerializeField] protected float damage;
    [SerializeField] protected bool canAttackPlayer;
    [SerializeField] private Slider healthSlider;
    [SerializeField] public int experienceValue;
    [SerializeField] protected float health;
    [SerializeField] protected Item dropItem;
    public delegate void DeathEvent(int xp);
    public static event DeathEvent OnEnemyDeath;
    [SerializeField] private float dropChance = 20;
    private void Start()
    {
        InitializeHealthBar();
    }
    private void OnHealthChanged(float previousValue, float newValue)
    {
        // Natychmiast aktualizuj health bar
        UpdateHealthBarImmediate(newValue);

        // SprawdŸ czy monster umar³
        if (newValue <= 0)
        {
            DropObject();
            Dead();
        }
    }

    private void UpdateHealthBarImmediate(float healthValue)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = healthValue;

            // Opcjonalnie: ukryj health bar gdy ¿ycie jest pe³ne
            // healthSlider.gameObject.SetActive(healthValue < maxHealth);
        }
    }

    private void InitializeHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
    }

    private void UpdateHealthBar()
    {
        UpdateHealthBarImmediate(health);
    }

    // Efekty wizualne przy otrzymaniu obra¿eñ


    // Metody dla kompatybilnoœci
    protected void UpdateHealthVisualisation()
    {
        UpdateHealthBar();
    }

    protected void HealthVisualisation()
    {
        UpdateHealthBar();
    }

    protected virtual void DropObject()
    {
        // Implementacja dropowania obiektów
    }

    protected void Dead()
    {
        OnEnemyDeath?.Invoke(experienceValue);
        if(dropChance >= Random.Range(0, 100) && dropItem != null)
        {
            Instantiate(dropItem.worldPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void OnMonsterDeathVisual()
    {
        // Efekty wizualne œmierci:
        // - animacje œmierci
        // - cz¹steczki
        // - dŸwiêki
    }

    public void TakeDamage(float amount)
    {
        float previousHealth = health;
        health = Mathf.Max(0, health - amount);

        // Natychmiastowy event o otrzymaniu obra¿eñ
        UpdateHealthBarImmediate(health);

        // Wywo³aj event zmiany zdrowia
        OnHealthChanged(previousHealth, health);
    }

    // Dodatkowa metoda dla healowania
    public void Heal(float amount)
    {
        float previousHealth = health;
        health = Mathf.Min(maxHealth, health + amount);

        // Event healowania
        UpdateHealthBarImmediate(health);
        OnHealReceivedVisual();

        // Wywo³aj event zmiany zdrowia
        OnHealthChanged(previousHealth, health);
    }

    private void OnHealReceivedVisual()
    {
        // Efekty wizualne healowania (zielone cz¹steczki, etc.)
        if (healthSlider != null)
        {
            StartCoroutine(FlashHealthBarGreen());
        }
    }

    private System.Collections.IEnumerator FlashHealthBarGreen()
    {
        var fillImage = healthSlider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            var originalColor = fillImage.color;
            fillImage.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            fillImage.color = originalColor;
        }
    }
}