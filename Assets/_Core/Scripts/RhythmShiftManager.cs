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
    [SerializeField] private TextMeshProUGUI _txtScore;
    [SerializeField] private TextMeshProUGUI _txtMultiplier;

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

    [Header("Scoring Values")]
    [SerializeField] private int _grannyShiftPoints = 10;
    [SerializeField] private int _goodPoints = 20;
    [SerializeField] private int _greatPoints = 40;
    [SerializeField] private int _perfectPoints = 80;
    [SerializeField] private int _doubleClutchBonusMultiplier = 2;

    private List<ShiftNote> _activeNotes = new List<ShiftNote>();
    private float _spawnTimer = 0f;
    private float _holdTickTimer = 0f;
    private float _feedbackTimer = 0f;
    private int _currentHeldGear = 0;

    private int _lastHitGear = -1;
    private bool _neutralVisitedSinceLastHit = false;
    private int _currentMultiplier = 1;

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
        UpdateUI();
    }

    private void Update()
    {
        HandleSpawning();
        MoveAndEvaluateNotes();
        HandleFeedbackFade();
        UpdateUI();
    }

    private void HandleGearChanged(int gear)
    {
        _currentHeldGear = gear;

        if (gear == 0)
        {
            _neutralVisitedSinceLastHit = true;
            return;
        }

        ShiftNote lowestNote = GetLowestUnhitNote();
        if (lowestNote != null && !lowestNote.IsHeadHit)
        {
            float distance = Mathf.Abs(lowestNote.RectTransform.anchoredPosition.y - _hitLineY);
            if (distance <= _goodWindow)
            {
                if (lowestNote.Gear == gear)
                {
                    lowestNote.IsHeadHit = true;

                    bool isDoubleClutch = (lowestNote.Gear == _lastHitGear && _neutralVisitedSinceLastHit);

                    if (isDoubleClutch)
                    {
                        AwardDoubleClutchScore(distance);
                    }
                    else
                    {
                        AwardPrecisionScore(distance);
                    }

                    _lastHitGear = lowestNote.Gear;
                    _neutralVisitedSinceLastHit = false;
                }
                else
                {
                    lowestNote.IsHeadHit = true;
                    RegisterMissAction();
                    _lastHitGear = -1;
                    _neutralVisitedSinceLastHit = false;
                }
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

                        // Granny shift gives points with NO multiplier change
                        AddScoreCustom(_grannyShiftPoints * _currentMultiplier);

                        ShowFeedback("GRANNY SHIFT", new Color(1f, 0.5f, 0f), allowOverwrite: true);

                        _lastHitGear = note.Gear;
                        _neutralVisitedSinceLastHit = false;
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
                    RegisterMissAction();
                    _lastHitGear = -1;
                    _neutralVisitedSinceLastHit = false;
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
                AddScoreCustom((_grannyShiftPoints / 2) * _currentMultiplier);
            }
        }
    }

    private void RegisterMissAction()
    {
        if (_gameManager != null) _gameManager.RegisterMiss();
        ShowFeedback("MISS", Color.red, allowOverwrite: false);
        _currentMultiplier = 1;
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
        _feedbackTimer = 0f;
        int basePts = _goodPoints;

        if (distance <= _perfectWindow)
        {
            basePts = _perfectPoints;
            _currentMultiplier += 2; // Perfect increases multiplier by 2
            ShowFeedback("PERFECT!", Color.cyan, allowOverwrite: true);
        }
        else if (distance <= _greatWindow)
        {
            basePts = _greatPoints;
            // Great does NOT increase multiplier
            ShowFeedback("GREAT!", Color.green, allowOverwrite: true);
        }
        else
        {
            // Good does NOT increase multiplier
            ShowFeedback("GOOD", Color.yellow, allowOverwrite: true);
        }

        AddScoreCustom(basePts * _currentMultiplier);
    }

    private void AwardDoubleClutchScore(float distance)
    {
        _feedbackTimer = 0f;

        int basePts = _goodPoints;
        if (distance <= _perfectWindow)
        {
            basePts = _perfectPoints;
            _currentMultiplier += 2; // Perfect Double Clutch increases multiplier by 2
        }
        else if (distance <= _greatWindow)
        {
            basePts = _greatPoints;
            _currentMultiplier += 1; // Great Double Clutch increases multiplier by 1
        }
        else
        {
            _currentMultiplier += 1; // Good Double Clutch increases multiplier by 1
        }

        int finalPts = (basePts * _doubleClutchBonusMultiplier) * _currentMultiplier;

        AddScoreCustom(finalPts);
        ShowFeedback("DOUBLE CLUTCH!", Color.magenta, allowOverwrite: true);
    }

    private void AddScoreCustom(int amount)
    {
        if (_gameManager == null) return;
        for (int i = 0; i < amount; i++)
        {
            _gameManager.AddScore();
        }
    }

    private void UpdateUI()
    {
        if (_gameManager != null)
        {
            if (_txtScore != null)
            {
                _txtScore.text = $"Score: {_gameManager.Score}";
            }
            if (_txtMultiplier != null)
            {
                if (_currentMultiplier <= 1)
                {
                    _txtMultiplier.text = "";
                }
                else
                {
                    _txtMultiplier.text = $"{_currentMultiplier}x";
                }
            }
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