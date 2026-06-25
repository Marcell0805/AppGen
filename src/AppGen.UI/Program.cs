using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using AppGen.Engine;
using AppGen.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<TemplateRenderer>();
builder.Services.AddSingleton<SolutionGenerator>();
builder.Services.AddSingleton<EntityGenerator>();
builder.Services.AddSingleton<UiGenerator>();
builder.Services.AddSingleton<PortalGenerator>();
builder.Services.AddSingleton<PortalGenerationService>();
builder.Services.AddSingleton<FlutterGenerator>();
builder.Services.AddSingleton<MobileApplicationGenerator>();
builder.Services.AddSingleton<MobileGenerationService>();
builder.Services.AddSingleton<DocumentationApplicationGenerator>();
builder.Services.AddSingleton<ProjectPromoter>();
builder.Services.AddSingleton<AppGenerationService>();
builder.Services.AddSingleton<OutputFolderService>();
builder.Services.AddScoped<WizardStateService>();
builder.Services.AddSingleton<ManifestSaveService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
