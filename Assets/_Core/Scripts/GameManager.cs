using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Stats")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _damagePerMiss = 20;
    [SerializeField] private int _healthPerScore = 5;

    [Header("Difficulty / Speed Scaling")]
    [SerializeField] private float _speedIncreaseRate = 0.05f; // How much speed increases per second (0.05 = 5% per sec)
    [SerializeField] private float _maxGameSpeedMultiplier = 3.0f; // Speed cap

    [Header("Roads, Rivals & UI")]
    [SerializeField] private List<InfiniteRoad> _roads = new();
    [SerializeField] private List<RivalRacerController> _rivals = new();
    [SerializeField] private List<AirFreshenerJiggle> _airFresheners = new();
    [SerializeField] private GameObject _gameOverScreen;
    [SerializeField] private float _slowdownDuration = 1f;

    private int _score = 0;
    private int _streak = 0;
    private int _currentHealth;
    private bool _isGameOver = false;
    private float _slowdownTimer = 0f;
    private float _elapsedPlayTime = 0f;

    [ShowNativeProperty] public int Score => _score;
    [ShowNativeProperty] public int Streak => _streak;
    [ShowNativeProperty] public int CurrentHealth => _currentHealth;

    // Returns a multiplier starting at 1.0 and scaling up over time, or drops to 0 on game over
    public float GlobalGameSpeed
    {
        get
        {
            if (_isGameOver)
            {
                float progress = Mathf.Clamp01(_slowdownTimer / _slowdownDuration);
                return Mathf.Lerp(1f, 0f, progress);
            }

            float calculatedSpeed = 1f + (_elapsedPlayTime * _speedIncreaseRate);
            return Mathf.Min(calculatedSpeed, _maxGameSpeedMultiplier);
        }
    }

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    private void Update()
    {
        if (_isGameOver)
        {
            _slowdownTimer += Time.deltaTime;
            float slowdownMultiplier = GlobalGameSpeed;

            foreach (var road in _roads)
            {
                if (road != null) road.UpdateSpeedMultiplier(slowdownMultiplier);
            }

            foreach (var rival in _rivals)
            {
                if (rival != null) rival.UpdateSpeedMultiplier(slowdownMultiplier);
            }

            foreach (var freshener in _airFresheners) { if (freshener != null) freshener.UpdateSpeedMultiplier(slowdownMultiplier); }
        }
        else
        {
            _elapsedPlayTime += Time.deltaTime;

            // Continuously push the current escalating speed to roads and rivals during normal gameplay
            float currentSpeed = GlobalGameSpeed;
            foreach (var road in _roads)
            {
                if (road != null) road.UpdateSpeedMultiplier(currentSpeed);
            }
            foreach (var rival in _rivals)
            {
                if (rival != null) rival.UpdateSpeedMultiplier(currentSpeed);
            }
            foreach (var freshener in _airFresheners) { if (freshener != null) freshener.UpdateSpeedMultiplier(currentSpeed); }
        }
    }

    public void AddScore()
    {
        if (_isGameOver) return;

        _score++;
        _streak++;

        _currentHealth += _healthPerScore;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth);
    }

    public void RegisterMiss()
    {
        if (_isGameOver) return;

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
        _isGameOver = true;
        _slowdownTimer = 0f;
        if (_gameOverScreen != null)
        {
            _gameOverScreen.SetActive(true);
        }
        Debug.Log("Game Over! Health reached 0.");
    }
}