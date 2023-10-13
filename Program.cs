using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using System;
using System.Net.Http.Json;




//This section is fairly boiler plate.
//Fairly sure this builds the webpage
var builder = WebApplication.CreateBuilder(args);
//Creates a database called Planetlist
builder.Services.AddDbContext<PlanetDB>(opt => opt.UseInMemoryDatabase("PlanetList"));


var app = builder.Build();

//API begins

app.MapGet("/api/planets", async () =>
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync("https://swapi.dev/api/planets/");
    
    
    return Results.Ok(response);
});



// Get all planets.
app.MapGet("/planets", async (PlanetDB db) =>
    await db.Planets.ToListAsync());

// Get favourite planets

app.MapGet("/favouritePlanets", async (PlanetDB db) =>
    await db.Planets.Where(p => p.IsFavourited).ToListAsync());


// Get Planet based off of ID

app.MapGet("/planets/{id}", async (int id, PlanetDB db) =>
    await db.Planets.FindAsync(id)
        is Planet planet
            ? Results.Ok(planet)
            : Results.NotFound());


// Make planet a favourite

app.MapPost("/favouritePlanets", async (Planet planet, PlanetDB db) =>
{
    //Check if planet already exists.
    if (await db.Planets.AnyAsync(p => p.Id == planet.Id))
        return Results.Conflict("Planet already favourited.");
    //If not
    planet.IsFavourited = true;
    db.Planets.Add(planet);
    await db.SaveChangesAsync();

    return Results.Created($"/favouritePlanets/{planet.Id}", planet);
});

// Delete a planet from favourites.

app.MapDelete("/favoritePlanets/{id}", async (int id, PlanetDB db) =>
{
    if (await db.Planets.FindAsync(id) is Planet planet)
    {
        db.Planets.Remove(planet);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();