using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Azure.Cosmos;
using FutureTech_StudentManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Authentication with BOTH Google and GitHub
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google"; // Default to Google
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("profile");
    options.Scope.Add("email");
})
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
    options.CallbackPath = "/signin-github";
    options.SaveTokens = true;
    options.Scope.Add("user:email");
});

// Configure Authorization with Admin Email List
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
        {
            // Get the user's email from their Google/GitHub account
            var userEmail = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // List of allowed admin emails (ADD YOUR GROUP MEMBERS' EMAILS HERE)
            var adminEmails = new List<string>
            {
                "nandishandu51@gmail.com",           
                "Gugukhanyiswa@gmail.com",       
                "angelndaba83@gmail.com",
                "nkanyisomabanga0@gmail.com",
                "Sfundozuma114@gmail.com",
                "snepromise0607@gmail.com",
                "thembelihlecele502@gmail.com",
                 "mqwelzanoxolo09@gmail.com",
                "njabulomazibuko86@gmail.com",
                "Luyandafortune17@gmail.com"



            };

            // Allow access ONLY if the user's email is in the admin list
            return !string.IsNullOrEmpty(userEmail) && adminEmails.Contains(userEmail);
        }));
});

// Configure Azure Cosmos DB
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var connectionString = builder.Configuration["Azure:CosmosDB:ConnectionString"];
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Cosmos DB connection string is not configured.");
    }

    var cosmosClientOptions = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    return new CosmosClient(connectionString, cosmosClientOptions);
});

builder.Services.AddSingleton<ICosmosDbService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var databaseName = builder.Configuration["Azure:CosmosDB:DatabaseName"] ?? "StudentDB";
    var containerName = builder.Configuration["Azure:CosmosDB:ContainerName"] ?? "Students";
    return new CosmosDbService(cosmosClient, databaseName, containerName);
});

// Configure Azure Blob Storage
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure Cosmos DB database and container exist
using (var scope = app.Services.CreateScope())
{
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var databaseName = configuration["Azure:CosmosDB:DatabaseName"] ?? "StudentDB";
    var containerName = configuration["Azure:CosmosDB:ContainerName"] ?? "Students";

    try
    {
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id", 400);
        Console.WriteLine("✅ Cosmos DB initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Cosmos DB initialization warning: {ex.Message}");
    }
}

app.Run();