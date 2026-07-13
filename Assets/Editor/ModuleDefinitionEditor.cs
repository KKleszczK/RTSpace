using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleDefinition))]
public class ModuleDefinitionEditor : Editor
{
    private SerializedProperty moduleId;
    private SerializedProperty displayName;
    private SerializedProperty description;
    private SerializedProperty icon;
    private SerializedProperty tier;
    private SerializedProperty type;

    private SerializedProperty metalCost;
    private SerializedProperty energyCost;
    private SerializedProperty craftTime;

    private SerializedProperty shieldFlat;
    private SerializedProperty shieldPercent;
    private SerializedProperty hpFlat;
    private SerializedProperty hpPercent;
    private SerializedProperty moveSpeedFlat;
    private SerializedProperty moveSpeedPercent;
    private SerializedProperty allWeaponsDamagePercent;
    private SerializedProperty allWeaponsAttackSpeedPercent;

    private SerializedProperty haveAura;
    private SerializedProperty auraRange;
    private SerializedProperty auraEffect;
    private SerializedProperty auraEffectValue;

    private SerializedProperty haveBaseBooster;
    private SerializedProperty baseBoosterEffect;
    private SerializedProperty baseBoosterValue;

    private SerializedProperty isSocketMiner;
    private SerializedProperty mineInterval;
    private SerializedProperty miningAmountAtFullDensity;
    private SerializedProperty densityRemovedPerHit;
    private SerializedProperty miningModuleDurability;

    private SerializedProperty isWeapon;
    private SerializedProperty weaponType;
    private SerializedProperty weaponRange;
    private SerializedProperty weaponAttackInterval;
    private SerializedProperty weaponHullDamage;
    private SerializedProperty weaponShieldDamage;
    private SerializedProperty projectileSpeed;

    private SerializedProperty weaponHasMagazine;
    private SerializedProperty magazineCapacity;
    private SerializedProperty magazineCanReload;
    private SerializedProperty magazineReloadTime;

    private SerializedProperty weaponHasAoe;
    private SerializedProperty weaponAoeRange;
    private SerializedProperty weaponAoeDamageMultiplier;

    private SerializedProperty weaponIsStacking;
    private SerializedProperty weaponMaxStacks;
    private SerializedProperty stackMovementSpeedPercent;
    private SerializedProperty stackDamagePercent;
    private SerializedProperty stackRangePercent;
    private SerializedProperty stackAttackSpeedPercent;
    private SerializedProperty stackInactiveTimeToReset;

    private SerializedProperty maxTargets;
    private SerializedProperty canChainAttack;
    private SerializedProperty chainJumps;
    private SerializedProperty chainDamageMultiplier;

    private SerializedProperty canSlowOnHit;
    private SerializedProperty slowPercent;
    private SerializedProperty slowDuration;

    private SerializedProperty selfHarmOnAttack;
    private SerializedProperty selfDamage;

    private void OnEnable()
    {
        moduleId = serializedObject.FindProperty("moduleId");
        displayName = serializedObject.FindProperty("displayName");
        description = serializedObject.FindProperty("description");
        icon = serializedObject.FindProperty("icon");
        tier = serializedObject.FindProperty("tier");
        type = serializedObject.FindProperty("type");

        metalCost = serializedObject.FindProperty("metalCost");
        energyCost = serializedObject.FindProperty("energyCost");
        craftTime = serializedObject.FindProperty("craftTime");

        shieldFlat = serializedObject.FindProperty("shieldFlat");
        shieldPercent = serializedObject.FindProperty("shieldPercent");
        hpFlat = serializedObject.FindProperty("hpFlat");
        hpPercent = serializedObject.FindProperty("hpPercent");
        moveSpeedFlat = serializedObject.FindProperty("moveSpeedFlat");
        moveSpeedPercent = serializedObject.FindProperty("moveSpeedPercent");
        allWeaponsDamagePercent = serializedObject.FindProperty("allWeaponsDamagePercent");
        allWeaponsAttackSpeedPercent = serializedObject.FindProperty("allWeaponsAttackSpeedPercent");

        haveAura = serializedObject.FindProperty("haveAura");
        auraRange = serializedObject.FindProperty("auraRange");
        auraEffect = serializedObject.FindProperty("auraEffect");
        auraEffectValue = serializedObject.FindProperty("auraEffectValue");

        haveBaseBooster = serializedObject.FindProperty("haveBaseBooster");
        baseBoosterEffect = serializedObject.FindProperty("baseBoosterEffect");
        baseBoosterValue = serializedObject.FindProperty("baseBoosterValue");

        isSocketMiner = serializedObject.FindProperty("isSocketMiner");
        mineInterval = serializedObject.FindProperty("mineInterval");
        miningAmountAtFullDensity = serializedObject.FindProperty("miningAmountAtFullDensity");
        densityRemovedPerHit = serializedObject.FindProperty("densityRemovedPerHit");
        miningModuleDurability = serializedObject.FindProperty("miningModuleDurability");

        isWeapon = serializedObject.FindProperty("isWeapon");
        weaponType = serializedObject.FindProperty("weaponType");
        weaponRange = serializedObject.FindProperty("weaponRange");
        weaponAttackInterval = serializedObject.FindProperty("weaponAttackInterval");
        weaponHullDamage = serializedObject.FindProperty("weaponHullDamage");
        weaponShieldDamage = serializedObject.FindProperty("weaponShieldDamage");
        projectileSpeed = serializedObject.FindProperty("projectileSpeed");

        weaponHasMagazine = serializedObject.FindProperty("weaponHasMagazine");
        magazineCapacity = serializedObject.FindProperty("magazineCapacity");
        magazineCanReload = serializedObject.FindProperty("magazineCanReload");
        magazineReloadTime = serializedObject.FindProperty("magazineReloadTime");

        weaponHasAoe = serializedObject.FindProperty("weaponHasAoe");
        weaponAoeRange = serializedObject.FindProperty("weaponAoeRange");
        weaponAoeDamageMultiplier = serializedObject.FindProperty("weaponAoeDamageMultiplier");

        weaponIsStacking = serializedObject.FindProperty("weaponIsStacking");
        weaponMaxStacks = serializedObject.FindProperty("weaponMaxStacks");
        stackMovementSpeedPercent = serializedObject.FindProperty("stackMovementSpeedPercent");
        stackDamagePercent = serializedObject.FindProperty("stackDamagePercent");
        stackRangePercent = serializedObject.FindProperty("stackRangePercent");
        stackAttackSpeedPercent = serializedObject.FindProperty("stackAttackSpeedPercent");
        stackInactiveTimeToReset = serializedObject.FindProperty("stackInactiveTimeToReset");

        maxTargets = serializedObject.FindProperty("maxTargets");
        canChainAttack = serializedObject.FindProperty("canChainAttack");
        chainJumps = serializedObject.FindProperty("chainJumps");
        chainDamageMultiplier = serializedObject.FindProperty("chainDamageMultiplier");

        canSlowOnHit = serializedObject.FindProperty("canSlowOnHit");
        slowPercent = serializedObject.FindProperty("slowPercent");
        slowDuration = serializedObject.FindProperty("slowDuration");

        selfHarmOnAttack = serializedObject.FindProperty("selfHarmOnAttack");
        selfDamage = serializedObject.FindProperty("selfDamage");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("GENERAL");
        EditorGUILayout.PropertyField(moduleId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(description);
        EditorGUILayout.PropertyField(icon);
        EditorGUILayout.PropertyField(tier);
        EditorGUILayout.PropertyField(type);

        DrawHeader("CRAFTING");
        EditorGUILayout.PropertyField(metalCost);
        EditorGUILayout.PropertyField(energyCost);
        EditorGUILayout.PropertyField(craftTime);

        DrawHeader("STAT MODIFIERS");
        EditorGUILayout.PropertyField(shieldFlat);
        EditorGUILayout.PropertyField(shieldPercent);
        EditorGUILayout.PropertyField(hpFlat);
        EditorGUILayout.PropertyField(hpPercent);
        EditorGUILayout.PropertyField(moveSpeedFlat);
        EditorGUILayout.PropertyField(moveSpeedPercent);
        EditorGUILayout.PropertyField(allWeaponsDamagePercent);
        EditorGUILayout.PropertyField(allWeaponsAttackSpeedPercent);

        DrawHeader("AURA");
        EditorGUILayout.PropertyField(haveAura);

        if (haveAura.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(auraRange);
            EditorGUILayout.PropertyField(auraEffect);
            EditorGUILayout.PropertyField(auraEffectValue);
            EditorGUI.indentLevel--;
        }

        DrawHeader("BASE BOOSTER");
        EditorGUILayout.PropertyField(haveBaseBooster);

        if (haveBaseBooster.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(baseBoosterEffect);
            EditorGUILayout.PropertyField(baseBoosterValue);
            EditorGUI.indentLevel--;
        }

        DrawHeader("MINING");
        EditorGUILayout.PropertyField(isSocketMiner);

        if (isSocketMiner.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mineInterval);
            EditorGUILayout.PropertyField(miningAmountAtFullDensity);
            EditorGUILayout.PropertyField(densityRemovedPerHit);
            EditorGUILayout.PropertyField(miningModuleDurability);
            EditorGUI.indentLevel--;
        }

        DrawHeader("WEAPON");
        EditorGUILayout.PropertyField(isWeapon);

        if (isWeapon.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(weaponType);
            EditorGUILayout.PropertyField(weaponRange);
            EditorGUILayout.PropertyField(weaponAttackInterval);
            EditorGUILayout.PropertyField(weaponHullDamage);
            EditorGUILayout.PropertyField(weaponShieldDamage);

            if ((WeaponType)weaponType.enumValueIndex == WeaponType.Projectile)
                EditorGUILayout.PropertyField(projectileSpeed);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(weaponHasMagazine);

            if (weaponHasMagazine.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(magazineCapacity);
                EditorGUILayout.PropertyField(magazineCanReload);

                if (magazineCanReload.boolValue)
                    EditorGUILayout.PropertyField(magazineReloadTime);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(weaponHasAoe);

            if (weaponHasAoe.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(weaponAoeRange);
                EditorGUILayout.PropertyField(weaponAoeDamageMultiplier);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(weaponIsStacking);

            if (weaponIsStacking.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(weaponMaxStacks);
                EditorGUILayout.PropertyField(stackMovementSpeedPercent);
                EditorGUILayout.PropertyField(stackDamagePercent);
                EditorGUILayout.PropertyField(stackRangePercent);
                EditorGUILayout.PropertyField(stackAttackSpeedPercent);
                EditorGUILayout.PropertyField(stackInactiveTimeToReset);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(maxTargets);
            EditorGUILayout.PropertyField(canChainAttack);

            if (canChainAttack.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(chainJumps);
                EditorGUILayout.PropertyField(chainDamageMultiplier);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(canSlowOnHit);

            if (canSlowOnHit.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(slowPercent);
                EditorGUILayout.PropertyField(slowDuration);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(selfHarmOnAttack);

            if (selfHarmOnAttack.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(selfDamage);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}