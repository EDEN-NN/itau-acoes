using Acoes.Api.Middleware;
using Acoes.Infrastructure.IoC;

var builder = WebApplication.CreateBuilder(args);

NativeInjectorBootStrapper.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1"));
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
