using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShifterController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _handleTransform;
    [SerializeField] private TextMeshProUGUI txt_currentGear;
    [SerializeField] private float _channelWidth = 100f;
    [SerializeField] private float _slotHeight = 100f;
    [SerializeField, Range(1, 18)] private int _totalGears = 6;
    [SerializeField] private float _maxDragDistance = 120f;
    [SerializeField] private float _lerpSpeed = 20f; // Adjustable lerp speed for shifter movement

    private Vector2 _centerPosition;
    private Vector2 _targetOffset;
    private Vector2 _currentOffset;
    private int _currentGear = 0;
    private bool _isDragging = false;
    private float _activeColumnX = 0f;

    [ShowNativeProperty]
    public int CurrentGear => _currentGear;

    // Event fired whenever the current gear changes
    public event System.Action<int> OnGearChanged;

    // Public properties/methods for ShifterVisualizer
    public float ChannelWidth => _channelWidth;
    public float SlotHeight => _slotHeight;
    public int TotalGears => _totalGears;
    public RectTransform HandleTransform => _handleTransform;

    private void Start()
    {
        if (_handleTransform != null)
        {
            _centerPosition = _handleTransform.anchoredPosition;
            _currentOffset = _handleTransform.anchoredPosition - _centerPosition;
            _targetOffset = _currentOffset;
        }
    }

    private void Update()
    {
        if (_handleTransform == null) return;

        // Smoothly lerp the shifter's physical position toward the target position
        _currentOffset = Vector2.Lerp(_currentOffset, _targetOffset, Time.deltaTime * _lerpSpeed);
        _handleTransform.anchoredPosition = _centerPosition + _currentOffset;

        // Gear state is evaluated based on where the shifter ACTUALLY is (arrived position)
        UpdateGearState(_currentOffset);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // vibrate or someth
        _isDragging = true;

        if (_handleTransform != null)
        {
            float[] slotX = GetSlotXPositionsPublic();
            _activeColumnX = slotX[0];
            float minDist = Mathf.Abs(_currentOffset.x - slotX[0]);
            for (int i = 1; i < slotX.Length; i++)
            {
                float dist = Mathf.Abs(_currentOffset.x - slotX[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    _activeColumnX = slotX[i];
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_handleTransform == null || !_isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_handleTransform.parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        Vector2 rawOffset = localPoint - _centerPosition;

        float distanceFromHandle = Vector2.Distance(_currentOffset, rawOffset);
        if (distanceFromHandle > _maxDragDistance)
        {
            DropShifter();
            return;
        }

        // Set the target offset; the Update loop will smoothly lerp the handle toward it
        _targetOffset = ConstrainToStrictHPattern(rawOffset, _currentOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        DropShifter();
    }

    private void DropShifter()
    {
        _isDragging = false;
        if (_handleTransform == null) return;

        _targetOffset = SnapToGate(_currentOffset);
    }

    public float[] GetSlotXPositionsPublic()
    {
        int columns = Mathf.CeilToInt(_totalGears / 2f);
        if (columns < 1) columns = 1;

        float[] slotX = new float[columns];

        if (columns == 1)
        {
            slotX[0] = 0f;
        }
        else
        {
            float totalWidthSpan = _channelWidth * (columns - 1);
            float startX = -totalWidthSpan / 2f;
            for (int i = 0; i < columns; i++)
            {
                slotX[i] = startX + (i * _channelWidth);
            }
        }

        return slotX;
    }

    private Vector2 ConstrainToStrictHPattern(Vector2 rawOffset, Vector2 currentOffset)
    {
        float[] slotX = GetSlotXPositionsPublic();
        float neutralThresholdY = _slotHeight * 0.35f;

        bool isInNeutralChannel = Mathf.Abs(currentOffset.y) <= neutralThresholdY;

        if (isInNeutralChannel)
        {
            float closestX = slotX[0];
            float minDist = Mathf.Abs(rawOffset.x - slotX[0]);
            for (int i = 1; i < slotX.Length; i++)
            {
                float dist = Mathf.Abs(rawOffset.x - slotX[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestX = slotX[i];
                }
            }
            _activeColumnX = closestX;

            float targetY = Mathf.Abs(rawOffset.y) < neutralThresholdY ? rawOffset.y : Mathf.Clamp(rawOffset.y, -_slotHeight, _slotHeight);
            if (Mathf.Abs(targetY) > neutralThresholdY)
            {
                int colIdx = System.Array.IndexOf(slotX, _activeColumnX);
                if (_totalGears % 2 != 0 && colIdx == slotX.Length - 1 && targetY < 0)
                {
                    targetY = 0f;
                }
            }

            return new Vector2(_activeColumnX, targetY);
        }
        else
        {
            int columnIdx = System.Array.IndexOf(slotX, _activeColumnX);
            bool isLastColumn = (columnIdx == slotX.Length - 1);
            bool hasOddTotalGears = (_totalGears % 2 != 0);

            float maxY = _slotHeight;
            float minY = -_slotHeight;
            if (hasOddTotalGears && isLastColumn)
            {
                minY = 0f;
            }

            float targetY = Mathf.Clamp(rawOffset.y, minY, maxY);
            return new Vector2(_activeColumnX, targetY);
        }
    }

    private Vector2 SnapToGate(Vector2 offset)
    {
        float[] slotX = GetSlotXPositionsPublic();

        float closestX = slotX[0];
        float minDistance = Mathf.Abs(offset.x - slotX[0]);
        for (int i = 1; i < slotX.Length; i++)
        {
            float dist = Mathf.Abs(offset.x - slotX[i]);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestX = slotX[i];
            }
        }

        int columnIdx = System.Array.IndexOf(slotX, closestX);
        bool isLastColumn = (columnIdx == slotX.Length - 1);
        bool hasOddTotalGears = (_totalGears % 2 != 0);

        if (offset.y > _slotHeight * 0.3f)
        {
            return new Vector2(closestX, _slotHeight);
        }
        else if (offset.y < -_slotHeight * 0.3f)
        {
            if (!(hasOddTotalGears && isLastColumn))
            {
                return new Vector2(closestX, -_slotHeight);
            }
        }

        return new Vector2(closestX, 0f);
    }

    private void UpdateGearState(Vector2 offset)
    {
        float[] slotX = GetSlotXPositionsPublic();
        float thresholdY = _slotHeight * 0.4f;

        int previousGear = _currentGear;
        int calculatedGear = 0;

        if (Mathf.Abs(offset.y) >= thresholdY)
        {
            bool isTop = offset.y > 0;
            int columnIdx = 0;

            for (int i = 0; i < slotX.Length; i++)
            {
                if (Mathf.Abs(offset.x - slotX[i]) < 10f)
                {
                    columnIdx = i;
                    break;
                }
            }

            calculatedGear = isTop ? (columnIdx * 2 + 1) : (columnIdx * 2 + 2);

            if (calculatedGear <= _totalGears)
            {
                _currentGear = calculatedGear;
            }
            else
            {
                _currentGear = 0;
            }
        }
        else
        {
            _currentGear = 0; // Neutral
        }

        if (txt_currentGear != null)
        {
            txt_currentGear.text = _currentGear == 0 ? "N" : _currentGear.ToString();
        }

        // Fire event only when gear changes
        if (_currentGear != previousGear)
        {
            OnGearChanged?.Invoke(_currentGear);
        }
    }
}