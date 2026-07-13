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

    public FixedString64Bytes module1;
    public FixedString64Bytes module2;
    public FixedString64Bytes module3;
    public FixedString64Bytes classModule;

    public FixedString64Bytes GetModule(int slotIndex)
    {
        return slotIndex switch
        {
            0 => module1,
            1 => module2,
            2 => module3,
            3 => classModule,
            _ => default
        };
    }

    public void SetModule(int slotIndex, FixedString64Bytes moduleId)
    {
        switch (slotIndex)
        {
            case 0:
                module1 = moduleId;
                break;

            case 1:
                module2 = moduleId;
                break;

            case 2:
                module3 = moduleId;
                break;

            case 3:
                classModule = moduleId;
                break;
        }
    }

    public int CountModule(string moduleId)
    {
        int count = 0;

        if (module1.ToString() == moduleId)
            count++;

        if (module2.ToString() == moduleId)
            count++;

        if (module3.ToString() == moduleId)
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

        serializer.SerializeValue(ref module1);
        serializer.SerializeValue(ref module2);
        serializer.SerializeValue(ref module3);
        serializer.SerializeValue(ref classModule);
    }

    public bool Equals(DockedShipData other)
    {
        return instanceId.Equals(other.instanceId)
            && shipId.Equals(other.shipId)
            && module1.Equals(other.module1)
            && module2.Equals(other.module2)
            && module3.Equals(other.module3)
            && classModule.Equals(other.classModule);
    }
}