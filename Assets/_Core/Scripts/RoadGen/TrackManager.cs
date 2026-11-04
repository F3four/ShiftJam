using UnityEngine;
using System.Collections.Generic;

public class TrackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<GameObject> _chunkPrefabs;
    [SerializeField] private Transform _playerTransform;

    [Header("Settings")]
    [SerializeField] private float _chunkSize = 50f;
    [SerializeField] private int _targetChunksAhead = 8;
    [SerializeField] private float _destroyDistance = 50f;
    [SerializeField] private float _maxSpawnDistanceAhead = 150f;

    private Queue<GameObject> _activeChunks = new Queue<GameObject>();
    private Vector3 _nextSpawnPosition = Vector3.zero;
    private Quaternion _nextSpawnRotation = Quaternion.identity;
    private float _cumulativeYawFromStart = 0f;

    private void Start()
    {
        while (_activeChunks.Count < _targetChunksAhead)
        {
            SpawnChunk();
        }
    }

    private void Update()
    {
        if (_playerTransform == null || _activeChunks.Count == 0) return;

        // Continuously spawn chunks ahead as long as we haven't reached the target count 
        // AND the player hasn't wandered too far away / off the track.
        float distanceToNextSpawn = Vector3.Distance(_playerTransform.position, _nextSpawnPosition);

        if (distanceToNextSpawn <= _maxSpawnDistanceAhead)
        {
            while (_activeChunks.Count < _targetChunksAhead)
            {
                SpawnChunk();
            }
        }

        // Cleanup old chunks behind the player
        GameObject oldestChunk = _activeChunks.Peek();
        if (Vector3.Distance(_playerTransform.position, oldestChunk.transform.position) > _destroyDistance + _chunkSize)
        {
            _activeChunks.Dequeue();
            Destroy(oldestChunk);
        }
    }

    private void SpawnChunk()
    {
        if (_chunkPrefabs == null || _chunkPrefabs.Count == 0) return;

        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (var prefab in _chunkPrefabs)
        {
            if (prefab == null) continue;

            TrackChunk chunkScript = prefab.GetComponent<TrackChunk>();
            float turnAngle = chunkScript != null ? chunkScript.TurnAngle : 0f;

            float projectedYaw = _cumulativeYawFromStart + turnAngle;

            if (Mathf.Abs(projectedYaw) < 180f)
            {
                validPrefabs.Add(prefab);
            }
        }

        if (validPrefabs.Count == 0) validPrefabs = _chunkPrefabs;

        int index = Random.Range(0, validPrefabs.Count);
        GameObject selectedPrefab = validPrefabs[index];

        TrackChunk selectedChunkScript = selectedPrefab.GetComponent<TrackChunk>();
        float selectedTurnAngle = selectedChunkScript != null ? selectedChunkScript.TurnAngle : 0f;

        GameObject newChunkObj = Instantiate(selectedPrefab, _nextSpawnPosition, _nextSpawnRotation);

        Quaternion exitRotation = _nextSpawnRotation * Quaternion.Euler(0, selectedTurnAngle, 0);
        Vector3 exitDirection = exitRotation * Vector3.forward;
        _nextSpawnPosition += exitDirection * _chunkSize;

        _nextSpawnRotation = exitRotation;
        _cumulativeYawFromStart += selectedTurnAngle;

        _activeChunks.Enqueue(newChunkObj);
    }
}