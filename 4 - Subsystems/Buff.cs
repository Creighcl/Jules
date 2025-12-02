public abstract class Buff
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public string PortraitArt { get; protected set; }
    public int TurnsRemaining { get; private set; }
    public Character Source { get; protected set; }
    public Character Target { get; protected set; }
    public bool isDebuff = false;
    public int Charges = 1;
    // i suspect this needs to be preflight or cleanup only!
    public CombatPhase AgingPhase { get; protected set; }

    protected Buff(Character src, Character tgt, int duration, int charges = 1)
    {
        Source = src;
        Target = tgt;
        TurnsRemaining = duration;
        Charges = charges;
    }

    public void Tick() // Decrease the duration of the buff
    {
        if (TurnsRemaining > 0)
        {
            TurnsRemaining--;
        }
    }

    public virtual EffectPlan ResolvePreflightEffects(){ return null; }

    // New Hooks for Resource Modification Pipeline

    // 1. Modify Outgoing: Called on the SOURCE's buffs when they initiate a resource change
    public virtual int ModifyOutgoingResourceAmount(ResourceChangeOrder order, int currentAmount) {
        return currentAmount;
    }

    // 2. Modify Incoming: Called on the TARGET's buffs when they receive a resource change
    public virtual int ModifyIncomingResourceAmount(ResourceChangeOrder order, int currentAmount) {
        return currentAmount;
    }
}
