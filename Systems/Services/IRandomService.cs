public interface IRandomService
{
    int Range(int min, int max);
    float Range(float min, float max);
    bool TryChance(int percentChance);
}

public class SystemRandomService : IRandomService
{
    private System.Random _random;

    public SystemRandomService(int seed)
    {
        _random = new System.Random(seed);
    }

    public SystemRandomService()
    {
        _random = new System.Random();
    }

    public int Range(int min, int max)
    {
        return _random.Next(min, max);
    }

    public float Range(float min, float max)
    {
        return (float)(_random.NextDouble() * (max - min) + min);
    }

    public bool TryChance(int percentChance)
    {
        return _random.Next(0, 100) < percentChance;
    }
}
