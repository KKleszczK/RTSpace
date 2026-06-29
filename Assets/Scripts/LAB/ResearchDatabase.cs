using System.Collections.Generic;
using UnityEngine;

public class ResearchDatabase : MonoBehaviour
{
    public static ResearchDatabase Instance;

    [SerializeField] private List<ResearchDefinition> researches = new();

    private void Awake()
    {
        Instance = this;
    }

    public ResearchDefinition GetResearch(string id)
    {
        foreach (ResearchDefinition research in researches)
        {
            if (research.researchId == id)
                return research;
        }

        return null;
    }
}