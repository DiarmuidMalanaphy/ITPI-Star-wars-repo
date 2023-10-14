using Microsoft.EntityFrameworkCore;

class PlanetDB : DbContext
{
    public PlanetDB(DbContextOptions<PlanetDB> options)
        : base(options) { }

    
    public DbSet<PlanetInfo> Planets => Set<PlanetInfo>();
}