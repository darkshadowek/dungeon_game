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
    public delegate void DeathEvent(int xp);
    public static event DeathEvent OnEnemyDeath;

    private void Start()
    {
        InitializeHealthBar();
    }
    private void OnHealthChanged(float previousValue, float newValue)
    {
        // Natychmiast aktualizuj health bar
        UpdateHealthBarImmediate(newValue);

        // Sprawdü czy monster umar≥
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

            // Opcjonalnie: ukryj health bar gdy øycie jest pe≥ne
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

    // Efekty wizualne przy otrzymaniu obraøeÒ


    // Metody dla kompatybilnoúci
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
        // Implementacja dropowania obiektÛw
    }

    protected void Dead()
    {
        OnEnemyDeath?.Invoke(experienceValue);
        Destroy(gameObject);
    }

    private void OnMonsterDeathVisual()
    {
        // Efekty wizualne úmierci:
        // - animacje úmierci
        // - czπsteczki
        // - düwiÍki
    }

    public void TakeDamage(float amount)
    {
        float previousHealth = health;
        health = Mathf.Max(0, health - amount);

        // Natychmiastowy event o otrzymaniu obraøeÒ
        UpdateHealthBarImmediate(health);

        // Wywo≥aj event zmiany zdrowia
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

        // Wywo≥aj event zmiany zdrowia
        OnHealthChanged(previousHealth, health);
    }

    private void OnHealReceivedVisual()
    {
        // Efekty wizualne healowania (zielone czπsteczki, etc.)
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