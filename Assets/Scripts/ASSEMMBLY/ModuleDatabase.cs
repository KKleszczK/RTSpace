using System.Collections.Generic;
using UnityEngine;

public class ModuleDatabase : MonoBehaviour
{
    public static ModuleDatabase Instance { get; private set; }

    [SerializeField] private List<ModuleDefinition> modules = new();

    private Dictionary<string, ModuleDefinition> lookup;

    private void Awake()
    {
        Instance = this;
        lookup = new Dictionary<string, ModuleDefinition>();

        foreach (ModuleDefinition module in modules)
        {
            if (module == null)
                continue;

            if (string.IsNullOrWhiteSpace(module.moduleId))
            {
                Debug.LogError(
                    $"ModuleDefinition {module.name} ma pusty moduleId",
                    module);

                continue;
            }

            lookup[module.moduleId] = module;
        }
    }

    public ModuleDefinition GetModule(string moduleId)
    {
        lookup.TryGetValue(moduleId, out ModuleDefinition module);
        return module;
    }
}