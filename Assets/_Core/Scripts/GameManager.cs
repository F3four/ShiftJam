using UnityEngine;
using NaughtyAttributes;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _basePoints = 100;
    [SerializeField] private int _penaltyPoints = 50;

    private int _score = 0;
    private int _streak = 0;

    [ShowNativeProperty] public int Score => _score;
    [ShowNativeProperty] public int Streak => _streak;

    public event System.Action<int, int> OnScoreUpdated; // Score, Streak

    public void AddScore()
    {
        _streak++;
        int multiplier = Mathf.Clamp(1 + (_streak / 5), 1, 5);
        _score += _basePoints * multiplier;
        OnScoreUpdated?.Invoke(_score, _streak);
    }

    public void RegisterMiss()
    {
        _streak = 0;
        _score = Mathf.Max(0, _score - _penaltyPoints);
        OnScoreUpdated?.Invoke(_score, _streak);
    }
}