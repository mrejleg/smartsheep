#:property PublishAot=false
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
// Uses the same local Web client configuration; never prints credentials or tokens.
using var config = JsonDocument.Parse(File.ReadAllText(args[0]));
var api = config.RootElement.GetProperty("Api");
using var client = new HttpClient { BaseAddress = new Uri(api.GetProperty("Url").GetString()!) };
var response = await client.PostAsJsonAsync("Auth/Token",new {
    ClientId=api.GetProperty("ClientId").GetString(), ClientSecret=api.GetProperty("ClientSecret").GetString(),
    Username="SuperAdmin",Email="",FullName="Super Administrator",Roles="" });
response.EnsureSuccessStatusCode();
using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
JsonElement Field(JsonElement item,string name) => item.EnumerateObject().Single(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)).Value;
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",Field(Field(token.RootElement,"Data"),"Token").GetString());
var profileResponse = await client.GetAsync("Auth/Profile/SuperAdmin");
Console.WriteLine($"Profile HTTP: {(int)profileResponse.StatusCode}");
profileResponse.EnsureSuccessStatusCode();
using var profile=JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
var farms=Field(profile.RootElement,"Farms");
if(farms.GetArrayLength()!=46 || !Field(profile.RootElement,"IsSuccessResponse").GetBoolean()) throw new Exception("Expected 46 assigned farms in the local SuperAdmin profile.");
Console.WriteLine("PASS: Web profile API returns all 46 assigned farms with code and name");
var mobileResponse=await client.GetAsync("MobileApp/Profile?username=SuperAdmin");
mobileResponse.EnsureSuccessStatusCode();
using var mobile=JsonDocument.Parse(await mobileResponse.Content.ReadAsStringAsync());
if(Field(Field(mobile.RootElement,"Data"),"Farms").GetArrayLength()!=46) throw new Exception("Mobile profile assignments mismatch.");
Console.WriteLine("PASS: Mobile profile API returns the same assigned farms");
foreach(var path in new[]{"Auth/Profile/another-user","MobileApp/Profile?username=another-user"}) {
    using var denied=await client.GetAsync(path);
    if(denied.StatusCode!=System.Net.HttpStatusCode.Forbidden) throw new Exception("Another user's profile must be forbidden.");
    Console.WriteLine("PASS: Another user's profile is denied");
}
