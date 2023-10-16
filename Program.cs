using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


/// To-dos to improve performance
/// 1. Minimise calls to API
///  Could implement this by downloading the entire planet set - Fast but could call way too much data, the user might only require one or two
///  Could only download the planet if the user has specifically requested it beforehand, minimises unneccessary calls but
///  batch calling is significantly faster than calling one at a time, I'm fairly sure the API limits you to 10000 calls a day too.
///   Notes - I've partially implemented this already in the Planet Info database, the only thing left to implement is the ability
///           to check the database before calling the API.
/// 2. Minimise the amount of deserialisation
///   The only way to do this is to mess around with JSON a bit, because I know there's a feature that allows you to ignore the lists when converting into db.
///   Issue is that it gets a bit finicky, a lot simpler to handle it as it is now.
///   
/// Essentially the main issue right now is that it's bottlenecked on the internet.

//This section is fairly boiler plate.

var builder = WebApplication.CreateBuilder(args);
//Creates a database called Planetlist
builder.Services.AddDbContext<PlanetDB>(opt => opt.UseInMemoryDatabase("PlanetList"));



var app = builder.Build();

///         API begins


/// Get all planets from swapi

app.MapGet("/api/planets/all", async () =>
{
    using var httpClient = new HttpClient();
    List<Planet> allPlanets = new List<Planet>();
    // The API returns a sort of linked list structure, where the final entry is the next entry, and it only returns
    // 10 planets per entry.
    // The nextURL represents the URL we search.
    // We always start with this.
    string nextUrl = "https://swapi.dev/api/planets/";
    //When it's over the final element redirects you to null.
    while (nextUrl != null)
            {
                HttpResponseMessage response = await httpClient.GetAsync(nextUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Go through the page scanning each page for the planets and putting them in a planet object.
                    
                    try{
                        string content = await response.Content.ReadAsStringAsync();
                        JObject jsonResponse = JObject.Parse(content);
                        JArray planets = (JArray)jsonResponse["results"];
                        
                        foreach (JObject Jplanet in planets){
                            Planet planet = JsonConvert.DeserializeObject<Planet>(Jplanet.ToString());
                            allPlanets.Add(planet);

                        }
                    

                        nextUrl = (string)jsonResponse["next"];
                    }
                    catch{
                        return Results.Created("An error occurred while processing your request.",500);
                    }
                    
                }
                else
                {
                    Console.WriteLine($"Failed to get data: {response.StatusCode}");
                    nextUrl = null;
                }
            }
    
    return Results.Ok(allPlanets);
});


/// Get a list of planets based on IDs works for one or a multiplicity.

app.MapGet("/api/planets/{ids}", async (string ids) =>
{
    List<int> planetIds = ids.Split(',').Select(int.Parse).ToList();
    List<Planet> allPlanets = new List<Planet>();
    foreach (int id in planetIds){
            try {
                Planet planet = await getPlanetAsync(id);
                allPlanets.Add(planet);    
            }
            catch {
                    Console.WriteLine($"Failed to get data");
                }
            }
    
    
    return Results.Ok(allPlanets);
});

/// Get all favourite planets

app.MapGet("/api/planets/favourites/all", async (PlanetDB db) => 
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





/// Make planet a favourite

app.MapPost("/api/planets/favourites/add/{ids}", async (string ids, PlanetDB db) =>
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

/// Delete a planet from favourites.

app.MapDelete("/api/planets/favourites/delete/{id}", async (int id, PlanetDB db) =>
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

/// Generate random non favourite
// This function isn't great to be honest, there's a lot of room for improvement but it works and the optimisations would honestly 
// minimal time saves.

app.MapGet("/api/planets/favourites/randomnon",async (PlanetDB db) =>
{
    // Going to implement a Naive solution to this problem 
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


/// Function to get planets based on an API ID
// To-do if going for performance gains make this scan the database first.
// Getting a bunch of random IDs is horribly inefficient otherwise.
// The favourite function is quite slow.

async Task<Planet> getPlanetAsync (int APIid){
    using var httpClient = new HttpClient();
    
    string url = "https://swapi.dev/api/planets/";
    HttpResponseMessage response = await httpClient.GetAsync(url+APIid);
    try{
    
    
        if (response.IsSuccessStatusCode)
            {
            string content = await response.Content.ReadAsStringAsync();

            Planet planet = JsonConvert.DeserializeObject<Planet>(content);

            return(planet);
            }
    }
    catch{}
    
    throw new HttpRequestException($"Failed to fetch planet. Status code: {response.StatusCode}"); 
}
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "html/index.html" }
});
app.UseStaticFiles();


app.Run();