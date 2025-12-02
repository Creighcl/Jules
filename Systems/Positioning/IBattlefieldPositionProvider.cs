using System.Collections.Generic;
using UnityEngine;

public interface IBattlefieldPositionProvider
{
    BattlefieldPosition GetNextOpenBattlefieldPositionForTeam(List<int> takenSpotIds, TeamType team);
    Character InstantiateNewCharacterForConfig(CharacterConfig config);
    CharacterConfig GetConfigForUnitType(SummonableUnit unitType);
}
