using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CycleScoresWeb.Data;
using CycleScoresWeb.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
//builder.Services.AddDbContext<CycleScoresWebContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("CycleScoresWebContext") ?? throw new InvalidOperationException("Connection string 'CycleScoresWebContext' not found.")));


var connection = String.Empty;
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}
else
{
    connection = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
}

builder.Services.AddDbContext<CycleScoresWebContext>(options =>
    options.UseSqlServer(connection));

builder.Services.AddSingleton<ICommuniqueService, CommuniqueService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
