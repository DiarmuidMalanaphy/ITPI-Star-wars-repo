public class PlanetInfo
{
    public int Id { get; set; } // Unique identifier for database
    public string serialisedPlanet { get; set; } // Serialized Planet object as a JSON string
    public bool IsFavourite { get; set; } // Flag to mark if the planet is a favorite

    public PlanetInfo(int id, string serializedPlanet, bool isFavourite)
    {
        Id = id;
        serialisedPlanet = serializedPlanet;
        IsFavourite = isFavourite;
    }
    public PlanetInfo() {}
}