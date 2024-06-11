using CactusFrontEnd.Components;
using CactusFrontEnd.Cosmos;
using CactusFrontEnd.Cosmos.utils;
using CactusFrontEnd.FrontEndFunctions;
using CactusFrontEnd.Security;
using JsonNet.ContractResolvers;
using Messenger;
using MessengerInterfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;


TokenVerification.Initialize();
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<IRepository<Account>, CosmosAccountRepository>();
builder.Services.AddSingleton<IRepository<Channel>, CosmosChannelRepository>();
builder.Services.AddSingleton<IRepository<Message>, CosmosMessageRepository>();
builder.Services.AddSingleton<IMessengerService, MessengerService>();
builder.Services.AddSingleton<CosmosClient>(_ => new CosmosClient(
    "AccountEndpoint=https://cactus-messenger.documents.azure.com:443/;AccountKey=A60Hjx2IJlO8FibcDgntPPJB1xkZkoS8TDmfAZOZrC9vUm4phxsTkl8VKZEHAj4ayM4ANW2h94sqACDbYa7SWw==;",
    new CosmosClientOptions
    {
        Serializer = new CosmosNewtonsoftJsonSerializer(new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new PrivateSetterContractResolver()
		})
    }));
builder.Services.AddBlazorContextMenu(options =>
{
	options.ConfigureTemplate("cactusTemplate", template =>
	{
		template.MenuCssClass = "cactusMenu";
		template.MenuItemCssClass = "cactusMenuItem";
	});
});

WebApplication app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();


app.MapRazorComponents<CactusFrontEnd.Components.App>()
    .AddInteractiveServerRenderMode();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.Services.GetRequiredService<IMessengerService>().InitializeAsync();

app.Run();
