using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementInteractionRule", menuName = "Systems/Elements/Interaction Rule")]
public class ElementInteractionRule : ScriptableObject, IElementInteractionRule
{
    public virtual bool IsResistant(IElementType attacker, IElementType defender) {
        // Default logic: same type = resistant
        if (attacker == null || defender == null) return false;

        // Identity comparison works for ScriptableObjects if they are the same asset
        // It also works for POCOs if they are the same instance
        return attacker == defender;
    }
}
