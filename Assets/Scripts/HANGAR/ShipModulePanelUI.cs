using UnityEngine;
using UnityEngine.UI;

public class ShipModulePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Ship")]
    [SerializeField] private Image shipIcon;

    [Header("Slots")]
    [SerializeField] private ModuleSlotUI[] moduleSlots;

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    public void Refresh(
        ShipDefinition shipDefinition,
        DockedShipData shipData,
        int coreTier)
    {
        if (shipIcon != null)
        {
            shipIcon.sprite =
                shipDefinition != null
                    ? shipDefinition.icon
                    : null;
        }

        coreTier = Mathf.Clamp(coreTier, 1, 3);

        for (int i = 0; i < moduleSlots.Length; i++)
        {
            ModuleSlotUI slot = moduleSlots[i];

            if (slot == null)
                continue;

            /*
             * Sloty:
             * 0 = normalny tier 1
             * 1 = normalny tier 2
             * 2 = normalny tier 3
             * 3 = klasowy, zawsze dostêpny
             */
            bool shouldBeLocked =
                i >= 0 &&
                i <= 2 &&
                i >= coreTier;

            slot.SetLocked(shouldBeLocked);

            string moduleId =
                shipData.GetModule(i).ToString();

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                slot.Clear();
                continue;
            }

            ModuleDefinition module =
                ModuleDatabase.Instance != null
                    ? ModuleDatabase.Instance.GetModule(moduleId)
                    : null;

            slot.SetModule(module);
        }
    }

    public void Clear()
    {
        if (shipIcon != null)
            shipIcon.sprite = null;

        foreach (ModuleSlotUI slot in moduleSlots)
        {
            if (slot == null)
                continue;

            slot.Clear();
            slot.SetLocked(false);
        }
    }

    public ModuleSlotUI[] GetSlots()
    {
        return moduleSlots;
    }
}