using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementInteractionRule", menuName = "Systems/Elements/Interaction Rule")]
public class ElementInteractionRule : ScriptableObject
{
    public virtual bool IsResistant(ElementType attacker, ElementType defender) {
        // Default logic: same type = resistant
        if (attacker == null || defender == null) return false;
        return attacker == defender;
    }
}
