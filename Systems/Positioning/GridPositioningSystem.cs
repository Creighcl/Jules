using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GridPositioningSystem : IPositioningSystem
{
    public int? GetDistance(ICombatPosition a, ICombatPosition b)
    {
        BattlefieldPosition posA = a as BattlefieldPosition;
        BattlefieldPosition posB = b as BattlefieldPosition;

        if (posA == null || posB == null) return null;

        return Mathf.Abs(posA.RelationalReferenceId - posB.RelationalReferenceId);
    }

    public List<Character> GetNeighbors(Character center, List<Character> candidates)
    {
        return candidates.Where(c => GetDistance(center.PositionInfo, c.PositionInfo) != null && GetDistance(center.PositionInfo, c.PositionInfo) == 1).ToList();
    }

    public List<Character> GetNearbyAllies(Character center, List<Character> candidates)
    {
        return candidates.Where(c => GetDistance(center.PositionInfo, c.PositionInfo) != null && GetDistance(center.PositionInfo, c.PositionInfo) <= 1).ToList();
    }

    public ICombatPosition GetPositionForCharacter(Character character)
    {
        return character.PositionInfo;
    }
}
