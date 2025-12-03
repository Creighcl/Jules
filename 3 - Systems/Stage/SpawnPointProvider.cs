using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SpawnPointProvider : MonoBehaviour, IBattlefieldPositionProvider
{
    public GameObject CharacterPrefab; // Needs to have CharacterBehavior component
    public List<Transform> PlayerSpawnPoints;
    public List<Transform> EnemySpawnPoints;

    // Config mapping for enemies usually
    public CharacterConfig ConfigGrunt;
    public CharacterConfig ConfigBrute;

    public BattlefieldPosition GetNextOpenBattlefieldPositionForTeam(List<int> takenSpotIds, TeamType team)
    {
        List<Transform> points = team == TeamType.PLAYER ? PlayerSpawnPoints : EnemySpawnPoints;

        for (int i = 0; i < points.Count; i++)
        {
            if (!takenSpotIds.Contains(i))
            {
                // SpotId matches index in list
                return new BattlefieldPosition(points[i].position, i, i);
            }
        }
        return null;
    }

    public Character InstantiateNewCharacterForConfig(CharacterConfig config)
    {
        if (CharacterPrefab == null)
        {
            Debug.LogError("SpawnPointProvider: CharacterPrefab is null!");
            return null;
        }

        GameObject go = Instantiate(CharacterPrefab);
        CharacterBehavior behavior = go.GetComponent<CharacterBehavior>();

        if (behavior == null)
        {
             Debug.LogError("SpawnPointProvider: CharacterPrefab missing CharacterBehavior!");
             return null;
        }

        behavior.InitializeModel(config);

        // Return the pure model
        return behavior.Model;
    }

    public CharacterConfig GetConfigForUnitType(SummonableUnit unitType)
    {
        // Simple mapping
        if (unitType == SummonableUnit.GRUNT) return ConfigGrunt;
        if (unitType == SummonableUnit.BRUTE) return ConfigBrute;
        return ConfigGrunt;
    }
}
