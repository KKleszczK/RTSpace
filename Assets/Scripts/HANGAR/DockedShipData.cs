using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct DockedShipData :
    INetworkSerializable,
    IEquatable<DockedShipData>
{
    public FixedString64Bytes instanceId;
    public FixedString64Bytes shipId;

    public FixedString64Bytes normalModule1;
    public FixedString64Bytes normalModule2;
    public FixedString64Bytes normalModule3;
    public FixedString64Bytes classModule;

    public FixedString64Bytes GetModule(int slotIndex)
    {
        return slotIndex switch
        {
            0 => normalModule1,
            1 => normalModule2,
            2 => normalModule3,
            3 => classModule,
            _ => default
        };
    }

    public void SetModule(
        int slotIndex,
        FixedString64Bytes moduleId)
    {
        switch (slotIndex)
        {
            case 0:
                normalModule1 = moduleId;
                break;

            case 1:
                normalModule2 = moduleId;
                break;

            case 2:
                normalModule3 = moduleId;
                break;

            case 3:
                classModule = moduleId;
                break;
        }
    }

    public void ClearModule(int slotIndex)
    {
        SetModule(slotIndex, default);
    }

    public bool HasModule(int slotIndex)
    {
        return !GetModule(slotIndex).IsEmpty;
    }

    public int CountModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return 0;

        int count = 0;

        if (normalModule1.ToString() == moduleId)
            count++;

        if (normalModule2.ToString() == moduleId)
            count++;

        if (normalModule3.ToString() == moduleId)
            count++;

        if (classModule.ToString() == moduleId)
            count++;

        return count;
    }

    public void NetworkSerialize<T>(
        BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref instanceId);
        serializer.SerializeValue(ref shipId);

        serializer.SerializeValue(ref normalModule1);
        serializer.SerializeValue(ref normalModule2);
        serializer.SerializeValue(ref normalModule3);
        serializer.SerializeValue(ref classModule);
    }

    public bool Equals(DockedShipData other)
    {
        return instanceId.Equals(other.instanceId)
            && shipId.Equals(other.shipId)
            && normalModule1.Equals(other.normalModule1)
            && normalModule2.Equals(other.normalModule2)
            && normalModule3.Equals(other.normalModule3)
            && classModule.Equals(other.classModule);
    }

    public override bool Equals(object obj)
    {
        return obj is DockedShipData other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            instanceId,
            shipId,
            normalModule1,
            normalModule2,
            normalModule3,
            classModule);
    }
}