using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ShiftNote : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtGearLabel;
    [SerializeField] private RectTransform _tailTransform; // Optional tail image for visual length

    private int _gear;
    private float _speed;
    private float _height;
    private RectTransform _rectTransform;
    private bool _isHeadHit = false;

    public int Gear => _gear;
    public bool IsHeadHit { get => _isHeadHit; set => _isHeadHit = value; }

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    public void Initialize(int gear, float speed, float length)
    {
        _gear = gear;
        _speed = speed;
        _height = length;

        if (_txtGearLabel != null)
        {
            _txtGearLabel.text = _gear.ToString();
        }

        // Set the height of the note container/tail to represent duration
        if (_tailTransform != null)
        {
            _tailTransform.sizeDelta = new Vector2(_tailTransform.sizeDelta.x, _height);
        }
    }

    public void Move(float deltaTime)
    {
        RectTransform.anchoredPosition += Vector2.down * _speed * deltaTime;
    }

    // Checks if the hit line currently overlaps the note's body
    public bool OverlapsHitLine(float hitLineY, float tolerance)
    {
        float noteTop = RectTransform.anchoredPosition.y;
        float noteBottom = noteTop - _height;
        return hitLineY <= (noteTop + tolerance) && hitLineY >= (noteBottom - tolerance);
    }
}