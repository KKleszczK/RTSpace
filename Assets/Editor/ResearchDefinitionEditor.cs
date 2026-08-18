using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ResearchDefinition))]
public class ResearchDefinitionEditor : Editor
{
    private SerializedProperty researchId;
    private SerializedProperty displayName;
    private SerializedProperty description;

    private SerializedProperty icon;
    private SerializedProperty researchedIcon;

    private SerializedProperty tier;

    private SerializedProperty baseMetalCost;
    private SerializedProperty baseEnergyCost;
    private SerializedProperty baseResearchTime;

    private SerializedProperty effectType;
    private SerializedProperty valueType;
    private SerializedProperty value;

    private SerializedProperty unlockedModuleIds;

    private void OnEnable()
    {
        researchId =
            serializedObject.FindProperty("researchId");

        displayName =
            serializedObject.FindProperty("displayName");

        description =
            serializedObject.FindProperty("description");

        icon =
            serializedObject.FindProperty("icon");

        researchedIcon =
            serializedObject.FindProperty("researchedIcon");

        tier =
            serializedObject.FindProperty("tier");

        baseMetalCost =
            serializedObject.FindProperty("baseMetalCost");

        baseEnergyCost =
            serializedObject.FindProperty("baseEnergyCost");

        baseResearchTime =
            serializedObject.FindProperty("baseResearchTime");

        effectType =
            serializedObject.FindProperty("effectType");

        valueType =
            serializedObject.FindProperty("valueType");

        value =
            serializedObject.FindProperty("value");

        unlockedModuleIds =
            serializedObject.FindProperty("unlockedModuleIds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // =====================================================
        // GENERAL
        // =====================================================

        DrawHeader("GENERAL");

        EditorGUILayout.PropertyField(researchId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(description);

        EditorGUILayout.PropertyField(icon);
        EditorGUILayout.PropertyField(researchedIcon);
        EditorGUILayout.PropertyField(tier);

        // =====================================================
        // COST
        // =====================================================

        DrawHeader("COST");

        EditorGUILayout.PropertyField(baseMetalCost);
        EditorGUILayout.PropertyField(baseEnergyCost);
        EditorGUILayout.PropertyField(baseResearchTime);

        // =====================================================
        // EFFECT
        // =====================================================

        DrawHeader("EFFECT");

        EditorGUILayout.PropertyField(effectType);

        ResearchEffectType selectedEffect =
            (ResearchEffectType)effectType.enumValueIndex;

        switch (selectedEffect)
        {
            // =================================================
            // SHIP STATS
            // =================================================

            case ResearchEffectType.ShipSpeed:
            case ResearchEffectType.ShipHp:
            case ResearchEffectType.ShipShield:

                EditorGUILayout.PropertyField(valueType);
                EditorGUILayout.PropertyField(value);

                break;

            // =================================================
            // STATION RESEARCH SPEED
            // =================================================

            case ResearchEffectType.StationResearchSpeed:

                EditorGUILayout.LabelField(
                    "Value Type",
                    "Percent");

                EditorGUILayout.PropertyField(
                    value,
                    new GUIContent("Percent Bonus"));

                break;

            // =================================================
            // STATION HULL
            // =================================================

            case ResearchEffectType.StationHullHp:

                EditorGUILayout.LabelField(
                    "Value Type",
                    "Flat");

                EditorGUILayout.PropertyField(
                    value,
                    new GUIContent("Hull HP Bonus"));

                break;

            // =================================================
            // STATION SHIELD
            // =================================================

            case ResearchEffectType.StationShield:

                EditorGUILayout.LabelField(
                    "Value Type",
                    "Flat");

                EditorGUILayout.PropertyField(
                    value,
                    new GUIContent("Shield Bonus"));

                break;

            // =================================================
            // GENERATOR
            // =================================================

            case ResearchEffectType.GeneratorBoost:

                EditorGUILayout.LabelField(
                    "Value Type",
                    "Percent");

                EditorGUILayout.PropertyField(
                    value,
                    new GUIContent("Generator Power Bonus %"));

                break;

            // =================================================
            // MODULE UNLOCK
            // =================================================

            case ResearchEffectType.UnlockModules:

                EditorGUILayout.PropertyField(
                    unlockedModuleIds,
                    new GUIContent("Unlocked Module IDs"),
                    true);

                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(
        string title)
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel);
    }
}