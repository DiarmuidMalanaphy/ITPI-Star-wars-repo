using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using System;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;



//This section is fairly boiler plate.

var builder = WebApplication.CreateBuilder(args);
//Creates a database called Planetlist
builder.Services.AddDbContext<PlanetDB>(opt => opt.UseInMemoryDatabase("PlanetList"));



var app = builder.Build();

//API begins


// Get all planets from swapi

app.MapGet("/api/planets", async () =>
{
    using var httpClient = new HttpClient();
    List<Dictionary<string, object>> allPlanets = new List<Dictionary<string, object>>();

    string nextUrl = "https://swapi.dev/api/planets/";
    
    while (nextUrl != null)
            {
                HttpResponseMessage response = await httpClient.GetAsync(nextUrl);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JObject jsonResponse = JObject.Parse(content);

                    JArray planets = (JArray)jsonResponse["results"];
                    foreach (JObject Jplanet in planets){
                        Planet planet = JsonConvert.DeserializeObject<Planet>(Jplanet.ToString());
                        var planetDict = new Dictionary<string, object>
                        {
                            {"Name", planet.Name},
                            {"RotationPeriod", planet.rotation_period},
                            {"OrbitalPeriod", planet.orbital_period},
                            {"Diameter", planet.Diameter},
                            {"Climate", planet.Climate},
                            {"Gravity", planet.Gravity},
                            {"Terrain", planet.Terrain},
                            {"SurfaceWater", planet.surface_water},
                            {"Population", planet.Population},
                            {"Residents", planet.Residents},
                            {"Url", planet.Url}
                        };

                        allPlanets.Add(planetDict);

                    }
                    

                    nextUrl = (string)jsonResponse["next"];
                }
                else
                {
                    Console.WriteLine($"Failed to get data: {response.StatusCode}");
                    nextUrl = null;
                }
            }
    //Console.WriteLine(JsonConvert.SerializeObject(allPlanets, Formatting.Indented));
    return Results.Ok(allPlanets);
});

// Get a list of planets.
app.MapGet("/api/planets/{ids}", async (string ids) =>
{
    List<int> planetIds = ids.Split(',').Select(int.Parse).ToList();

    //List<Planet> allPlanets = new List<Planet>();
    //List<Dictionary<string, object>> allPlanets = new List<Dictionary<string, object>>();
    List<Planet> allPlanets = new List<Planet>();
    foreach (int id in planetIds){

    
                
                
            try {
                Planet planet = await getPlanetAsync(id);
                /*
                //var planetDict = new Dictionary<string, object>
                //{
                //        {"Name", planet.Name},
                //        {"RotationPeriod", planet.rotation_period},
                //        {"OrbitalPeriod", planet.orbital_period},
                //        {"Diameter", planet.Diameter},
                        // {"Climate", planet.Climate},
                        {"Gravity", planet.Gravity},
                        {"Terrain", planet.Terrain},
                        {"SurfaceWater", planet.surface_water},
                        {"Population", planet.Population},
                        {"Residents",  planet.Residents},
                        {"Url", planet.Url}
                    };

                    allPlanets.Add(planetDict);
                */
                allPlanets.Add(planet);    

                    
                }
            catch
                {
                    Console.WriteLine($"Failed to get data");
                }
            }
    
    Console.WriteLine(allPlanets);
    return Results.Ok(allPlanets);
});


// Get all planets.
app.MapGet("/api/planets", async (PlanetDB db) =>
    await db.Planets.ToListAsync());

// Get favourite planets


app.MapGet("/api/favouritePlanets", async (PlanetDB db) =>
    await db.Planets.Where(p => p.IsFavourite).ToListAsync());


// Get Planet based off of ID
// Not sure if I'll extend this feature but the idea is to have previously called planets be stored locally
// To minimise api calls.


//app.MapGet("/planets/{id}", async (int id, PlanetDB db) =>
//    await db.Planets.FindAsync(id)
//        is Planet planet
//            ? Results.Ok(planet)
//            : Results.NotFound());


// Make planet a favourite

app.MapPost("/api/addfavouritePlanet/{id}", async (int id, PlanetDB db) =>
{
    try 
        {
        //Check if planet already exists.
        try{
            if (await db.Planets.AnyAsync(p => p.Id == id))
        
                return Results.Conflict("Planet already favourited.");
        }
        catch{
            
        }
        //If not
        
        Planet planet = await getPlanetAsync(id);
        PlanetInfo planetInfo = new PlanetInfo(id,JsonConvert.SerializeObject(planet),true);
       
        db.Planets.Add(planetInfo);
        await db.SaveChangesAsync();

        return Results.Created($"/favouritePlanets/{id}", planet);
        }
    catch(Exception e) 
    {
    // log the exception
    return Results.Created("An error occurred while processing your request.",500);
}
});

// Delete a planet from favourites.
/*
app.MapDelete("/api/favouritePlanets/{id}", async (int id, PlanetDB db) =>
{
    if (await db.Planets.FindAsync(id) is Planet planet)
    {
        db.Planets.Remove(planet);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});
*/

//Function to get planets based on an API id, may expand this to look at db first

async Task<Planet> getPlanetAsync (int APIid){
    using var httpClient = new HttpClient();
    
    string url = "https://swapi.dev/api/planets/";
    HttpResponseMessage response = await httpClient.GetAsync(url+APIid);
    
    if (response.IsSuccessStatusCode)
        {
        string content = await response.Content.ReadAsStringAsync();

        Planet planet = JsonConvert.DeserializeObject<Planet>(content);
        
        
        

        return(planet);
        }
    throw new HttpRequestException($"Failed to fetch planet. Status code: {response.StatusCode}");
}

app.Run();