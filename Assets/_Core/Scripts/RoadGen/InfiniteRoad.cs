using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoad : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _segmentLength = 8f;
    [SerializeField] private List<Transform> _roadSegments = new();

    private float _speedMultiplier = 1f;

    private void Update()
    {
        MoveSegments();
        RecycleSegments();
    }

    private void MoveSegments()
    {
        float movement = _moveSpeed * _speedMultiplier * Time.deltaTime;

        foreach (Transform segment in _roadSegments)
        {
            segment.Translate(Vector3.left * movement, Space.World);
        }
    }

    private void RecycleSegments()
    {
        if (_roadSegments.Count == 0) return;

        Transform firstSegment = _roadSegments[0];

        if (firstSegment.position.x <= -_segmentLength)
        {
            Transform lastSegment = _roadSegments[_roadSegments.Count - 1];
            float newX = lastSegment.position.x + _segmentLength;

            firstSegment.position = new Vector3(newX, firstSegment.position.y, firstSegment.position.z);

            _roadSegments.RemoveAt(0);
            _roadSegments.Add(firstSegment);
        }
    }

    public void UpdateSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }
}