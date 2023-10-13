using Microsoft.EntityFrameworkCore;

class PlanetDB : DbContext
{
    public PlanetDB(DbContextOptions<PlanetDB> options)
        : base(options) { }

    public DbSet<Planet> Planets => Set<Planet>();
}