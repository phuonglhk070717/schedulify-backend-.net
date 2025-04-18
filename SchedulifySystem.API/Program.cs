using Microsoft.AspNetCore.Mvc;
using SchedulifySystem.API;
using SchedulifySystem.API.Middleware;
using SchedulifySystem.Service.Hubs;
using SchedulifySystem.Service.Validations;
using SchedulifySystem.Service.ViewModels.ResponseModels;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with custom model state response
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(m => m.Value.Errors.Count > 0)
            .SelectMany(m => m.Value.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        var response = new BaseResponseModel
        {
            Status = StatusCodes.Status400BadRequest,
            Message = string.Join("; ", errors)
        };

        return new BadRequestObjectResult(response);
    };
});

// Add IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS to allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("app-cors", policy =>
    {
        policy
            .AllowAnyOrigin()          // Allow requests from any origin
            .AllowAnyHeader()          // Allow any headers
            .AllowAnyMethod()          // Allow any HTTP method (GET, POST, etc.)
            .AllowCredentials();       // Allow credentials (cookies, HTTP authentication)
    });
});

// Custom service registrations
builder.Services.AddWebAPIService(builder);
builder.Services.AddInfractstructure(builder.Configuration);

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// Use Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Schedulify Web API");
    c.RoutePrefix = "swagger";
});

// HTTPS redirection (optional to limit to dev only)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Apply CORS policy
app.UseCors("app-cors");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseWebSockets();

// Use SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

// Map controllers
app.MapControllers();

// Global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Run the app
app.Run();
