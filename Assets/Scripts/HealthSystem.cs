using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("UI")]
    public Slider healthSlider;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [HideInInspector]
    public bool isBlocking = false;

    void Start()
    {
        Debug.Log($"{gameObject.name} starting with health: {maxHealth}");
        currentHealth = maxHealth;
        Debug.Log($"{gameObject.name} starting with health: {maxHealth}");

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Health slider not assigned!");
        }
    }

    public void TakeDamage(float amount, bool isTargetBlocking)
    {
        if (isTargetBlocking)
        {
            amount *= 0.25f; // Reduce damage if blocking
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");

        if (CompareTag("Player"))
        {
            //FindObjectOfType<UIManager>().ShowGameOver(); // Show Game Over UI
            Time.timeScale = 0f; // Optional: Pause the game
        }
        else
        {
            // Let enemy retreat instead of dying
            GetComponent<EnemyAI>().RetreatAndHeal(); // ✅ You’ll write this method below
        }
    }


    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
