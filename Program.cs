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

app.MapGet("/api/planets/all", async () =>
{
    using var httpClient = new HttpClient();
    List<Planet> allPlanets = new List<Planet>();


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
                        

                        allPlanets.Add(planet);

                    }
                    

                    nextUrl = (string)jsonResponse["next"];
                }
                else
                {
                    Console.WriteLine($"Failed to get data: {response.StatusCode}");
                    nextUrl = null;
                }
            }
    
    return Results.Ok(allPlanets);
});

// Get a list of planets based on IDs.

app.MapGet("/api/planets/{ids}", async (string ids) =>
{
    List<int> planetIds = ids.Split(',').Select(int.Parse).ToList();
    List<Planet> allPlanets = new List<Planet>();
    foreach (int id in planetIds){

            try {
                Planet planet = await getPlanetAsync(id);
                allPlanets.Add(planet);    
                }
            catch
                {
                    Console.WriteLine($"Failed to get data");
                }
            }
    
    
    return Results.Ok(allPlanets);
});

// Get all favourite planets


app.MapGet("/api/favouritePlanets/all", async (PlanetDB db) => 
{
    var favouritePlanetsInfo = await db.Planets.Where(p => p.IsFavourite).ToListAsync();
    var favouritePlanets = new List<Planet>();

    foreach (var planetInfo in favouritePlanetsInfo)
    {
        // Deserialize planetInfo.SerializedPlanet into a Planet object
        Planet planet = JsonConvert.DeserializeObject<Planet>(planetInfo.serialisedPlanet);
        
        // Add the Planet object to the list
        favouritePlanets.Add(planet);
    }

    return Results.Ok(favouritePlanets);
});


// Get Planet based off of ID
// Not sure if I'll extend this feature but the idea is to have previously called planets be stored locally
// To minimise api calls.


//app.MapGet("/planets/{id}", async (int id, PlanetDB db) =>
//    await db.Planets.FindAsync(id)
//        is Planet planet
//            ? Results.Ok(planet)
//            : Results.NotFound());


// Make planet a favourite

app.MapPost("/api/addfavouritePlanet/{ids}", async (string ids, PlanetDB db) =>
{
    try 
    {
        
        List<int> planetIds = ids.Split(',').Select(int.Parse).ToList();
        
        foreach (int id in planetIds){

            
            // Check if planet already exists.
            try{
                if (await db.Planets.AnyAsync(p => p.Id == id))
                    return Results.Conflict("Planet already favourited.");
                }
            catch {}
            // If doesn't already exist
            
            Planet planet = await getPlanetAsync(id);
            PlanetInfo planetInfo = new PlanetInfo(id,JsonConvert.SerializeObject(planet),true);
            db.Planets.Add(planetInfo);
        }

        // No need to do several changes.
        await db.SaveChangesAsync();
        return Results.NoContent();

        }
        catch(Exception e) 
        {
        // log the exception
            return Results.Created("An error occurred while processing your request.",500);
        }
    });

// Delete a planet from favourites.

app.MapDelete("/api/deletefavourite/{id}", async (int id, PlanetDB db) =>
{
    // Look for the planet info with the given id
    var planetInfo = await db.Planets.FindAsync(id);

    if (planetInfo != null)
    {
        // Remove the planet info from the database
        db.Planets.Remove(planetInfo);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    return Results.NotFound();
});

app.MapGet("/api/getRandomNonFavourite/",async (PlanetDB db) =>
{
    //Going to implement a Naive solution to this problem 
    // Go through the planetInfo db and get all the id numbers from that
    var favouriteIds = await db.Planets
                                .Select(p => p.Id)
                                .ToListAsync();
    
    
    //randomly generate a number 1-59 and check if it's in the list if it's not call the get planet function to get the
    //planet at that number.
    Random rand = new Random();
    int randomId;
    do
    {
        randomId = rand.Next(1, 59);
    } while (favouriteIds.Contains(randomId));

    Planet randomPlanet = await getPlanetAsync(randomId);

    //return that planet/
    return Results.Ok(randomPlanet);


}  );


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
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "html/index.html" }
});
app.UseStaticFiles();


app.Run();