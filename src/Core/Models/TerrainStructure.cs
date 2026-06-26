namespace Core.Models;

public enum StructureType
{
    None = 0,

    // Settlements
    Village,
    Town,
    City,
    Ruins,

    // Military
    Fortress,
    Watchtower,
    Outpost,

    // Resource / Economy
    Mine,
    Lumbermill,
    Farm,
    Fishery,
    Port,

    // Dungeon / Special
    Shrine,
    Cave,
    Dungeon,
}
