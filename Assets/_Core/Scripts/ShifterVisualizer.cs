using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(ShifterController))]
public class ShifterVisualizer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _uiContainer;
    [SerializeField] private GameObject _gateUIPrefab;
    [SerializeField] private GameObject _labelPrefab;

    [Header("Appearance")]
    [SerializeField] private float _gateThickness = 25f;
    [SerializeField] private Color _gateColor = new Color(0f, 1f, 1f, 0.5f);

    [Header("Label Settings")]
    [SerializeField] private float _verticalLabelOffset = 15f; // Extra offset distance outward for odd/even gear labels
    [SerializeField] private Vector2 _labelBaseOffset = Vector2.zero;

    private ShifterController _shifter;
    [SerializeField, HideInInspector] private GameObject _spawnedRoot;

    private void Awake()
    {
        _shifter = GetComponent<ShifterController>();
        if (_uiContainer == null)
        {
            _uiContainer = GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            GenerateVisualizerUI();
        }
    }

#if UNITY_EDITOR
    [Button("Bake UI Visuals")]
    public void BakeVisualsInEditor()
    {
        GenerateVisualizerUI();
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (_spawnedRoot != null)
        {
            UnityEditor.EditorUtility.SetDirty(_spawnedRoot);
        }
        Debug.Log("Shifter UI visuals and TMP gear labels baked successfully in editor.");
    }
#endif

    public void GenerateVisualizerUI()
    {
        if (_shifter == null) _shifter = GetComponent<ShifterController>();
        if (_uiContainer == null) return;

        if (_spawnedRoot != null)
        {
            if (Application.isPlaying) Destroy(_spawnedRoot);
            else DestroyImmediate(_spawnedRoot);
        }

        _spawnedRoot = new GameObject("GeneratedShifterUI", typeof(RectTransform));
        RectTransform rootRect = _spawnedRoot.GetComponent<RectTransform>();
        rootRect.SetParent(_uiContainer, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        float[] slotX = _shifter.GetSlotXPositionsPublic();
        float slotHeight = _shifter.SlotHeight;
        int totalGears = _shifter.TotalGears;
        bool hasOddTotalGears = (totalGears % 2 != 0);

        int gearCounter = 1;

        // 1. Spawn Vertical Slots and Gear Labels
        for (int i = 0; i < slotX.Length; i++)
        {
            float bottomY = -slotHeight;
            bool skipBottom = (hasOddTotalGears && i == slotX.Length - 1);
            if (skipBottom)
            {
                bottomY = 0f;
            }

            float centerY = (slotHeight + bottomY) / 2f;
            float height = Mathf.Abs(slotHeight - bottomY);

            CreateUIElement(rootRect, new Vector2(slotX[i], centerY), new Vector2(_gateThickness, height), "Gate_" + i);

            // Spawn Top Gear Label (Odd gears: 1, 3, 5...) -> Offset Upwards (+Y)
            if (gearCounter <= totalGears)
            {
                Vector2 topPos = new Vector2(slotX[i], slotHeight + _verticalLabelOffset) + _labelBaseOffset;
                CreateGearLabel(rootRect, topPos, gearCounter.ToString());
                gearCounter++;
            }

            // Spawn Bottom Gear Label (Even gears: 2, 4, 6...) -> Offset Downwards (-Y)
            if (!skipBottom && gearCounter <= totalGears)
            {
                Vector2 bottomPos = new Vector2(slotX[i], -slotHeight - _verticalLabelOffset) + _labelBaseOffset;
                CreateGearLabel(rootRect, bottomPos, gearCounter.ToString());
                gearCounter++;
            }
        }

        // 2. Spawn Horizontal Neutral Crossbar
        if (slotX.Length > 0)
        {
            float minX = slotX[0];
            float maxX = slotX[slotX.Length - 1];
            float centerX = (minX + maxX) / 2f;
            float totalWidth = (maxX - minX) + _gateThickness;

            CreateUIElement(rootRect, new Vector2(centerX, 0f), new Vector2(totalWidth, _gateThickness), "Crossbar");
        }
    }

    private void CreateUIElement(RectTransform parent, Vector2 anchoredPos, Vector2 size, string objName)
    {
        GameObject gateObj;

        if (_gateUIPrefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                gateObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_gateUIPrefab, parent);
            else
                gateObj = Instantiate(_gateUIPrefab, parent);
#else
            gateObj = Instantiate(_gateUIPrefab, parent);
#endif
        }
        else
        {
            gateObj = new GameObject(objName, typeof(RectTransform), typeof(Image));
            Image img = gateObj.GetComponent<Image>();
            img.color = _gateColor;
            img.raycastTarget = false;
        }

        RectTransform rect = gateObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private void CreateGearLabel(RectTransform parent, Vector2 anchoredPos, string gearText)
    {
        GameObject labelObj;

        if (_labelPrefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                labelObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_labelPrefab, parent);
            else
                labelObj = Instantiate(_labelPrefab, parent);
#else
            labelObj = Instantiate(_labelPrefab, parent);
#endif
        }
        else
        {
            // If no prefab is assigned, create a root container and a child text object automatically
            labelObj = new GameObject("GearLabelContainer_" + gearText, typeof(RectTransform));

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(labelObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI fallbackTmp = textObj.GetComponent<TextMeshProUGUI>();
            fallbackTmp.alignment = TextAlignmentOptions.Center;
            fallbackTmp.color = Color.white;
            fallbackTmp.fontSize = 24;
            fallbackTmp.raycastTarget = false;
        }

        // Find the TextMeshPro component anywhere in the object or its children
        TextMeshProUGUI tmp = labelObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = gearText;
        }

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(40f, 40f);
    }
}