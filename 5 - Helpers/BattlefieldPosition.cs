using UnityEngine;

public class BattlefieldPosition : ICombatPosition
{
    public BattlefieldPosition(Vector3 position, int spotId, int relationalReferenceId)
    {
        Position = position;
        SpotId = spotId;
        RelationalReferenceId = relationalReferenceId;
    }

    private Vector3 _position;
    public Vector3 Position
    {
        get { return _position; }
        set { _position = value; }
    }

    public int SpotId = -10;
    public int RelationalReferenceId = -10;
}
