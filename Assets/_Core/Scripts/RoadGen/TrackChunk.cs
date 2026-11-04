using UnityEngine;

public class TrackChunk : MonoBehaviour
{
    [SerializeField] private float _turnAngle = 0f; // e.g., 0 for straight, 30 for right, -30 for left
    public float TurnAngle => _turnAngle;
}