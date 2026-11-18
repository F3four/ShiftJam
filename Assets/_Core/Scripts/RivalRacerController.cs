using UnityEngine;

public class RivalRacerController : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Transform _playerCar;
    [SerializeField] private Transform _rivalCar;
    [SerializeField] private float _maxDistanceOffset = 8f;
    [SerializeField] private float _moveLerpSpeed = 5f;
    [SerializeField] private float _gameOverOffscreenOffset = 100f;

    [Header("Road Vibration / Noise")]
    [SerializeField] private float _vibrationFrequency = 5f;
    [SerializeField] private float _rivalVibrationMagnitude = 0.1f;
    [SerializeField] private float _playerVibrationMagnitude = 0.08f;

    private Vector3 _initialPlayerPos;
    private Vector3 _initialRivalPos;
    private float _noiseSeedX;
    private float _noiseSeedY;
    private float _speedMultiplier = 1f;
    private float _currentNoiseTime = 0f;
    private float _currentRivalBaseX;

    private void Awake()
    {
        if (_playerCar != null) _initialPlayerPos = _playerCar.position;
        if (_rivalCar != null)
        {
            _initialRivalPos = _rivalCar.position;
            _currentRivalBaseX = _initialRivalPos.x;
        }

        _noiseSeedX = Random.Range(0f, 100f);
        _noiseSeedY = Random.Range(100f, 200f);
    }

    private void Update()
    {
        if (_gameManager == null || _playerCar == null || _rivalCar == null) return;

        _currentNoiseTime += Time.deltaTime * _vibrationFrequency * _speedMultiplier;

        float healthPercent = Mathf.Clamp01((float)_gameManager.CurrentHealth / 100f);
        float healthDeficit = 1f - healthPercent;

        // Add extra off-screen offset when the game over speed multiplier drops to zero / game ends
        float extraOffset = (_speedMultiplier <= 0f) ? _gameOverOffscreenOffset : 0f;

        float targetBaseX = _initialPlayerPos.x + (healthDeficit * _maxDistanceOffset) + extraOffset;
        _currentRivalBaseX = Mathf.Lerp(_currentRivalBaseX, targetBaseX, Time.deltaTime * _moveLerpSpeed);

        float playerNoiseY = (Mathf.PerlinNoise(0f, _currentNoiseTime + _noiseSeedY) - 0.5f) * 2f * _playerVibrationMagnitude;
        float rivalNoiseX = (Mathf.PerlinNoise(_currentNoiseTime + _noiseSeedX, 0f) - 0.5f) * 2f * _rivalVibrationMagnitude;
        float rivalNoiseY = (Mathf.PerlinNoise(0f, _currentNoiseTime + _noiseSeedY + 50f) - 0.5f) * 2f * _rivalVibrationMagnitude;

        _playerCar.position = new Vector3(_initialPlayerPos.x, _initialPlayerPos.y + playerNoiseY, _initialPlayerPos.z);

        float finalRivalX = _currentRivalBaseX + rivalNoiseX;
        float finalRivalY = _initialRivalPos.y + rivalNoiseY;

        _rivalCar.position = new Vector3(finalRivalX, finalRivalY, _initialRivalPos.z);
    }

    public void UpdateSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }
}