namespace Core.Interfaces;

public interface ITerrainRandomizer
{
    float NextFloat();
    float NextFloat(float min, float max);
    int NextInt(int min, int max);
}