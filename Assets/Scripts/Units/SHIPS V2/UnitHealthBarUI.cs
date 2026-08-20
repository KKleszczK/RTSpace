using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBarUI : MonoBehaviour
{
    // =========================================================
    // HP
    // =========================================================

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private RectTransform hpSegmentsContainer;

    // =========================================================
    // SHIELD
    // =========================================================

    [Header("Shield")]
    [SerializeField] private GameObject shieldRoot;
    [SerializeField] private Image shieldFill;
    [SerializeField] private RectTransform shieldSegmentsContainer;

    // =========================================================
    // SEGMENTS
    // =========================================================

    [Header("Segments")]
    [SerializeField] private GameObject segmentPrefab;

    [SerializeField]
    private int hpSegmentSize = 100;

    [SerializeField]
    private int shieldSegmentSize = 100;

    // =========================================================
    // UNIT REFERENCES
    // =========================================================

    [Header("Unit")]
    [SerializeField] private ShipUnit ship;
    [SerializeField] private BaseUnit baseUnit;

    // =========================================================
    // CACHE
    // =========================================================

    private int lastMaxHp = -1;
    private int lastMaxShield = -1;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (ship == null)
        {
            ship =
                GetComponentInParent<ShipUnit>();
        }

        if (baseUnit == null)
        {
            baseUnit =
                GetComponentInParent<BaseUnit>();
        }
    }

    private void OnEnable()
    {
        if (ship != null)
        {
            ship.hp.OnValueChanged +=
                OnHealthChanged;

            ship.maxHp.OnValueChanged +=
                OnHealthChanged;

            ship.shield.OnValueChanged +=
                OnHealthChanged;

            ship.maxShield.OnValueChanged +=
                OnHealthChanged;

            RefreshFromShip();

            return;
        }

        if (baseUnit != null)
        {
            baseUnit.hp.OnValueChanged +=
                OnHealthChanged;

            baseUnit.maxHp.OnValueChanged +=
                OnHealthChanged;

            baseUnit.shield.OnValueChanged +=
                OnHealthChanged;

            baseUnit.maxShield.OnValueChanged +=
                OnHealthChanged;

            RefreshFromBase();
        }
    }

    private void OnDisable()
    {
        if (ship != null)
        {
            ship.hp.OnValueChanged -=
                OnHealthChanged;

            ship.maxHp.OnValueChanged -=
                OnHealthChanged;

            ship.shield.OnValueChanged -=
                OnHealthChanged;

            ship.maxShield.OnValueChanged -=
                OnHealthChanged;
        }

        if (baseUnit != null)
        {
            baseUnit.hp.OnValueChanged -=
                OnHealthChanged;

            baseUnit.maxHp.OnValueChanged -=
                OnHealthChanged;

            baseUnit.shield.OnValueChanged -=
                OnHealthChanged;

            baseUnit.maxShield.OnValueChanged -=
                OnHealthChanged;
        }
    }

    // =========================================================
    // NETWORK CHANGES
    // =========================================================

    private void OnHealthChanged(
        int oldValue,
        int newValue)
    {
        RefreshFromSource();
    }

    private void RefreshFromSource()
    {
        if (ship != null)
        {
            RefreshFromShip();
            return;
        }

        if (baseUnit != null)
        {
            RefreshFromBase();
        }
    }

    // =========================================================
    // SHIP
    // =========================================================

    private void RefreshFromShip()
    {
        if (ship == null)
            return;

        Refresh(
            ship.hp.Value,
            ship.maxHp.Value,
            ship.shield.Value,
            ship.maxShield.Value);
    }

    // =========================================================
    // BASE
    // =========================================================

    private void RefreshFromBase()
    {
        if (baseUnit == null)
            return;

        Refresh(
            baseUnit.hp.Value,
            baseUnit.maxHp.Value,
            baseUnit.shield.Value,
            baseUnit.maxShield.Value);
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh(
        int currentHp,
        int maxHp,
        int currentShield,
        int maxShield)
    {
        UpdateHp(
            currentHp,
            maxHp);

        UpdateShield(
            currentShield,
            maxShield);
    }

    // =========================================================
    // HP
    // =========================================================

    private void UpdateHp(
        int currentHp,
        int maxHp)
    {
        if (hpFill != null)
        {
            hpFill.fillAmount =
                maxHp > 0
                    ? Mathf.Clamp01(
                        (float)currentHp /
                        maxHp)
                    : 0f;
        }

        /*
         * Segmenty przebudowujemy tylko,
         * gdy zmieni³o siê maksymalne HP.
         */
        if (maxHp != lastMaxHp)
        {
            lastMaxHp = maxHp;

            RebuildSegments(
                hpSegmentsContainer,
                maxHp,
                hpSegmentSize);
        }
    }

    // =========================================================
    // SHIELD
    // =========================================================

    private void UpdateShield(
        int currentShield,
        int maxShield)
    {
        bool hasShield =
            maxShield > 0;

        /*
         * Jednostka bez tarczy nie pokazuje
         * ca³ego paska Shield.
         */
        if (shieldRoot != null)
        {
            shieldRoot.SetActive(
                hasShield);
        }

        if (!hasShield)
        {
            if (lastMaxShield != maxShield)
            {
                lastMaxShield =
                    maxShield;

                ClearSegments(
                    shieldSegmentsContainer);
            }

            return;
        }

        if (shieldFill != null)
        {
            shieldFill.fillAmount =
                Mathf.Clamp01(
                    (float)currentShield /
                    maxShield);
        }

        /*
         * Segmenty przebudowujemy tylko,
         * gdy zmieni³o siê maksimum Shield.
         */
        if (maxShield != lastMaxShield)
        {
            lastMaxShield =
                maxShield;

            RebuildSegments(
                shieldSegmentsContainer,
                maxShield,
                shieldSegmentSize);
        }
    }

    // =========================================================
    // SEGMENTS
    // =========================================================

    private void RebuildSegments(
        RectTransform container,
        int maxValue,
        int segmentSize)
    {
        if (container == null)
            return;

        ClearSegments(
            container);

        if (segmentPrefab == null)
        {
            Debug.LogError(
                "[HEALTH UI] Brak Segment Prefab.",
                this);

            return;
        }

        if (maxValue <= 0)
            return;

        if (segmentSize <= 0)
            return;

        /*
         * Przyk³ad:
         *
         * maxValue = 250
         * segmentSize = 100
         *
         * powstan¹:
         *
         * 100 / 250 = 0.4
         * 200 / 250 = 0.8
         */

        for (int value = segmentSize;
             value < maxValue;
             value += segmentSize)
        {
            float normalizedPosition =
                (float)value /
                maxValue;

            // =================================================
            // CREATE ROOT
            // =================================================

            GameObject markerObject =
                Instantiate(
                    segmentPrefab,
                    container);

            Transform markerRoot =
                markerObject.transform;

            markerRoot.localRotation =
                Quaternion.identity;

            markerRoot.localScale =
                Vector3.one;

            /*
             * Root prefabu ma zwyk³y Transform.
             *
             * Dlatego jego pozycjê liczymy
             * rêcznie na podstawie szerokoœci
             * RectTransform kontenera.
             *
             * Uwzglêdniamy równie¿ pivot
             * kontenera.
             */
            float xPosition =
                (
                    normalizedPosition -
                    container.pivot.x
                ) *
                container.rect.width;

            markerRoot.localPosition =
                new Vector3(
                    xPosition,
                    0f,
                    0f);

            // =================================================
            // FIND IMAGE RECT
            // =================================================

            RectTransform marker =
                markerObject
                    .GetComponentInChildren<
                        RectTransform>();

            if (marker == null)
            {
                Debug.LogError(
                    "[HEALTH UI] " +
                    "Segment prefab nie zawiera " +
                    "RectTransform.",
                    markerObject);

                Destroy(
                    markerObject);

                continue;
            }

            /*
             * Sam obrazek separatora
             * ustawiamy centralnie wzglêdem
             * jego roota.
             */
            marker.anchorMin =
                new Vector2(
                    0.5f,
                    0f);

            marker.anchorMax =
                new Vector2(
                    0.5f,
                    1f);

            marker.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            marker.anchoredPosition =
                Vector2.zero;

            /*
             * Zachowujemy szerokoœæ
             * ustawion¹ w prefabie.
             *
             * Wysokoœæ = wysokoœæ kontenera,
             * poniewa¿ anchor Y jest Stretch.
             */
            Vector2 size =
                marker.sizeDelta;

            size.y = 0f;

            marker.sizeDelta =
                size;

            marker.localRotation =
                Quaternion.identity;

            marker.localScale =
                Vector3.one;
        }
    }

    // =========================================================
    // CLEAR SEGMENTS
    // =========================================================

    private void ClearSegments(
        RectTransform container)
    {
        if (container == null)
            return;

        for (int i =
             container.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                container
                    .GetChild(i)
                    .gameObject);
        }
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void SetSegmentSizes(
        int newHpSegmentSize,
        int newShieldSegmentSize)
    {
        hpSegmentSize =
            Mathf.Max(
                1,
                newHpSegmentSize);

        shieldSegmentSize =
            Mathf.Max(
                1,
                newShieldSegmentSize);

        /*
         * Wymusza przebudowanie segmentów
         * przy nastêpnym Refresh().
         */
        lastMaxHp = -1;
        lastMaxShield = -1;

        RefreshFromSource();
    }
}