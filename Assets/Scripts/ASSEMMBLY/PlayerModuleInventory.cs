using Unity.Collections;
using Unity.Netcode;

public class PlayerModuleInventory : NetworkBehaviour
{
    public NetworkList<FixedString64Bytes> modules;

    private void Awake()
    {
        modules = new NetworkList<FixedString64Bytes>();
    }

    public bool HasModule(string moduleId)
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i].ToString() == moduleId)
                return true;
        }

        return false;
    }

    public int GetModuleCount(string moduleId)
    {
        int count = 0;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i].ToString() == moduleId)
                count++;
        }

        return count;
    }

    public void AddModule(string moduleId)
    {
        if (!IsServer)
            return;

        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        modules.Add(new FixedString64Bytes(moduleId));
    }

    public bool RemoveOneModule(string moduleId)
    {
        if (!IsServer)
            return false;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i].ToString() == moduleId)
            {
                modules.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void ClearInventory()
    {
        if (!IsServer)
            return;

        modules.Clear();
    }
}