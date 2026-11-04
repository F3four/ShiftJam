using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;

public class RhythmShiftManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShifterController _shifterController;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private RectTransform _highwayContainer;
    [SerializeField] private GameObject _notePrefab;
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    [Header("Highway Settings")]
    [SerializeField] private float _spawnY = 400f;
    [SerializeField] private float _hitLineY = -150f;
    [SerializeField] private float _noteSpeed = 300f;
    [SerializeField] private float _spawnInterval = 1.5f;
    [SerializeField] private float _defaultNoteLength = 100f;
    [SerializeField] private float _holdTickInterval = 0.2f;
    [SerializeField] private float _highwayWidth = 400f;

    [Header("Timing / Accuracy Windows (Pixels)")]
    [SerializeField] private float _perfectWindow = 15f;
    [SerializeField] private float _greatWindow = 35f;
    [SerializeField] private float _goodWindow = 60f;
    [SerializeField] private float _missThreshold = -300f;

    private List<ShiftNote> _activeNotes = new List<ShiftNote>();
    private float _spawnTimer = 0f;
    private float _holdTickTimer = 0f;
    private float _feedbackTimer = 0f;
    private int _currentHeldGear = 0;

    private void OnEnable()
    {
        if (_shifterController != null)
        {
            _shifterController.OnGearChanged += HandleGearChanged;
        }
    }

    private void OnDisable()
    {
        if (_shifterController != null)
        {
            _shifterController.OnGearChanged -= HandleGearChanged;
        }
    }

    private void Start()
    {
        if (_shifterController != null)
        {
            _currentHeldGear = _shifterController.CurrentGear;
        }
        if (_txtFeedback != null) _txtFeedback.text = "";
    }

    private void Update()
    {
        HandleSpawning();
        MoveAndEvaluateNotes();
        HandleFeedbackFade();
    }

    private void HandleGearChanged(int gear)
    {
        _currentHeldGear = gear;

        ShiftNote lowestNote = GetLowestUnhitNote();
        if (lowestNote != null && !lowestNote.IsHeadHit)
        {
            float distance = Mathf.Abs(lowestNote.RectTransform.anchoredPosition.y - _hitLineY);
            if (lowestNote.Gear == gear && distance <= _goodWindow)
            {
                lowestNote.IsHeadHit = true;
                AwardPrecisionScore(distance);
            }
        }
    }

    private void HandleSpawning()
    {
        if (_shifterController == null || _notePrefab == null) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnNote();
        }
    }

    private void SpawnNote()
    {
        int totalGears = _shifterController != null ? _shifterController.TotalGears : 6;
        int randomGear = Random.Range(1, totalGears + 1);

        GameObject noteObj = Instantiate(_notePrefab, _highwayContainer);
        ShiftNote note = noteObj.GetComponent<ShiftNote>();

        if (note == null)
        {
            Destroy(noteObj);
            return;
        }

        RectTransform rect = note.RectTransform;
        rect.anchoredPosition = new Vector2(GetXPositionForGear(randomGear), _spawnY);

        note.Initialize(randomGear, _noteSpeed, _defaultNoteLength);
        _activeNotes.Add(note);
    }

    private void MoveAndEvaluateNotes()
    {
        bool isHoldingNote = false;
        ShiftNote lowestUnhitNote = GetLowestUnhitNote();

        for (int i = _activeNotes.Count - 1; i >= 0; i--)
        {
            ShiftNote note = _activeNotes[i];
            if (note == null)
            {
                _activeNotes.RemoveAt(i);
                continue;
            }

            note.Move(Time.deltaTime);
            float noteHeadY = note.RectTransform.anchoredPosition.y;

            if (note == lowestUnhitNote && !note.IsHeadHit)
            {
                if (noteHeadY <= _hitLineY && Mathf.Abs(noteHeadY - _hitLineY) <= _goodWindow)
                {
                    if (_currentHeldGear == note.Gear)
                    {
                        note.IsHeadHit = true;
                        if (_gameManager != null) _gameManager.AddScore();
                        ShowFeedback("GOOD", Color.yellow, allowOverwrite: true);
                    }
                }
            }

            if (note.IsHeadHit && note.OverlapsHitLine(_hitLineY, 10f))
            {
                if (_currentHeldGear == note.Gear)
                {
                    isHoldingNote = true;
                }
            }

            if (noteHeadY < _missThreshold)
            {
                if (!note.IsHeadHit)
                {
                    if (_gameManager != null) _gameManager.RegisterMiss();
                    ShowFeedback("MISS", Color.red, allowOverwrite: false);
                }

                _activeNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
        }

        if (isHoldingNote)
        {
            _holdTickTimer += Time.deltaTime;
            if (_holdTickTimer >= _holdTickInterval)
            {
                _holdTickTimer = 0f;
                if (_gameManager != null) _gameManager.AddScore();
            }
        }
    }

    private ShiftNote GetLowestUnhitNote()
    {
        ShiftNote lowest = null;
        float lowestY = float.MaxValue;

        foreach (var note in _activeNotes)
        {
            if (note == null || note.IsHeadHit) continue;
            float y = note.RectTransform.anchoredPosition.y;
            if (y < lowestY)
            {
                lowestY = y;
                lowest = note;
            }
        }
        return lowest;
    }

    private void HandleFeedbackFade()
    {
        if (_txtFeedback != null && _txtFeedback.text != "")
        {
            _feedbackTimer += Time.deltaTime;
            if (_feedbackTimer >= 0.5f)
            {
                _txtFeedback.text = "";
            }
        }
    }

    private float GetXPositionForGear(int gear)
    {
        int totalGears = _shifterController != null ? _shifterController.TotalGears : 6;
        if (totalGears <= 1) return 0f;

        float gearIndex = Mathf.Clamp(gear - 1, 0, totalGears - 1);
        float step = _highwayWidth / (totalGears - 1);
        return (gearIndex * step) - (_highwayWidth / 2f);
    }

    private void AwardPrecisionScore(float distance)
    {
        if (_gameManager == null) return;
        _feedbackTimer = 0f;

        if (distance <= _perfectWindow)
        {
            for (int j = 0; j < (_gameManager.Streak >= 5 ? 2 : 1); j++) _gameManager.AddScore();
            ShowFeedback("PERFECT!", Color.cyan, allowOverwrite: true);
        }
        else if (distance <= _greatWindow)
        {
            _gameManager.AddScore();
            ShowFeedback("GREAT!", Color.green, allowOverwrite: true);
        }
        else
        {
            _gameManager.AddScore();
            ShowFeedback("GOOD", Color.yellow, allowOverwrite: true);
        }
    }

    private void ShowFeedback(string message, Color color, bool allowOverwrite)
    {
        if (_txtFeedback != null)
        {
            if (!allowOverwrite && _txtFeedback.text != "" && _txtFeedback.text != "MISS")
            {
                return;
            }

            _feedbackTimer = 0f;
            _txtFeedback.text = message;
            _txtFeedback.color = color;
        }
    }
}