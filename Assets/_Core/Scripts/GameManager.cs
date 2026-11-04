using NaughtyAttributes;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Stats")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _damagePerMiss = 20;

    private int _score = 0;
    private int _streak = 0;
    private int _currentHealth;

    [ShowNativeProperty] public int Score => _score;
    [ShowNativeProperty] public int Streak => _streak;
    [ShowNativeProperty] public int CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void AddScore()
    {
        _score++;
        _streak++;
    }

    public void RegisterMiss()
    {
        _streak = 0;
        _currentHealth -= _damagePerMiss;
        _currentHealth = Mathf.Max(0, _currentHealth);

        if (_currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log("Game Over! Health reached 0.");
        // Add your game over transition or UI trigger here
    }
}