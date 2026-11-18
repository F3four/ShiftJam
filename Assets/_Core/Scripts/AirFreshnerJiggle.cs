using UnityEngine;

public class AirFreshenerJiggle : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private float _frequency = 5f;
    [SerializeField] private float _maxRotationAngle = 15f;
    [SerializeField] private float _maxPositionalJerk = 0.05f;
    [SerializeField] private float _returnLerpSpeed = 6f;

    private Quaternion _initialRotation;
    private Vector3 _initialPosition;
    private float _noiseSeedX;
    private float _noiseSeedY;
    private float _noiseSeedZ;
    private float _speedMultiplier = 1f;
    private float _currentNoiseTime = 0f;
    private float _smoothIntensity = 1f;

    private void Awake()
    {
        _initialRotation = transform.localRotation;
        _initialPosition = transform.localPosition;

        _noiseSeedX = Random.Range(0f, 100f);
        _noiseSeedY = Random.Range(100f, 200f);
        _noiseSeedZ = Random.Range(200f, 300f);
    }

    private void Update()
    {
        // Smoothly scale intensity toward the target speed multiplier so it returns to rest gracefully
        _smoothIntensity = Mathf.Lerp(_smoothIntensity, _speedMultiplier, Time.deltaTime * _returnLerpSpeed);

        _currentNoiseTime += Time.deltaTime * _frequency * _speedMultiplier;

        float noiseX = (Mathf.PerlinNoise(_currentNoiseTime + _noiseSeedX, 0f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(0f, _currentNoiseTime + _noiseSeedY) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(_currentNoiseTime + _noiseSeedZ, _currentNoiseTime) - 0.5f) * 2f;

        // Scale offsets by smooth intensity (shrinks to 0 when stopped, returning to default transform)
        float rotX = noiseX * _maxRotationAngle * _smoothIntensity;
        float rotY = noiseY * _maxRotationAngle * _smoothIntensity;
        float rotZ = noiseZ * _maxRotationAngle * _smoothIntensity;

        transform.localRotation = _initialRotation * Quaternion.Euler(rotX, rotY, rotZ);

        float posX = noiseX * _maxPositionalJerk * _smoothIntensity;
        float posY = noiseY * _maxPositionalJerk * _smoothIntensity;

        transform.localPosition = _initialPosition + new Vector3(posX, posY, 0f);
    }

    public void UpdateSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }
}