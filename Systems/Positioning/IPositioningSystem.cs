using System.Collections.Generic;

public interface IPositioningSystem
{
    int GetDistance(ICombatPosition a, ICombatPosition b);
    List<Character> GetNeighbors(Character center, List<Character> candidates);
    List<Character> GetNearbyAllies(Character center, List<Character> candidates);
    ICombatPosition GetPositionForCharacter(Character character);
}
