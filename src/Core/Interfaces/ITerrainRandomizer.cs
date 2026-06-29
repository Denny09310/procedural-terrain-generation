namespace Core.Interfaces;

public interface ITerrainRandomizer
{
    void SetSeed(int seed);
    float NextFloat();
    float NextFloat(float min, float max);
    int NextInt(int min, int max);
}