using TS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddInfrastructure(builder.Configuration);



var app = builder.Build();

app.MapControllers();


app.Run();
