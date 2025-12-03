public class StandardInteractionRule : IElementInteractionRule
{
    public bool IsResistant(IElementType attacker, IElementType defender) {
        // Default logic: same type = resistant
        if (attacker == null || defender == null) return false;

        // Identity comparison
        return attacker == defender;
    }
}
