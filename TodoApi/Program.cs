using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);// יצירת מבנה של אפליקציה אינטרנטית חדשה
builder.Services.AddEndpointsApiExplorer();// הוספת קונפיגורציה לאפשרות ראות וניהול של נקודות הסיום באפליקציה

builder.Services.AddSwaggerGen(b =>
{
    b.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
});

//Register DbContext as a service
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");// משיכת מחרוזת החיבור לבסיס הנתונים מתוך קובץ התצורה

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"), ServerVersion.Parse("8.0.36-mysql")),// הוספת ה-DbContext לקונטיינר השירותים
    ServiceLifetime.Singleton);// רישום ה-DbContext כשירות תפקיד יחיד

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
    builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build(); // בניית האפליקציה מתוך המבנה שנבנה בשלב הקודם


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
    
}

app.UseCors("AllowAll");

// Additional route to serve requests to the base address ("/")
app.MapGet("/", () => "Hello World!");

//Retrieving all tasks
app.MapGet("/todos", async (ToDoDbContext context) =>
{
    var items = await context.Items.ToListAsync();
    return JsonSerializer.Serialize(items);
});

//Adding a new task
app.MapPost("/todos", async (ToDoDbContext context, Item newItem) =>
{
    context.Items.Add(newItem);
    await context.SaveChangesAsync();
    return newItem;
});

//task update
app.MapPut("/todos/{id}", async (ToDoDbContext context, int id, Item updatedItem) =>
{
    var existingItem = await context.Items.FindAsync(id);
    if (existingItem == null)
    {
        return Results.NotFound();
    }

    if(updatedItem.Name != null)
    {
        existingItem.Name = updatedItem.Name;
    }

    existingItem.IsComplete = updatedItem.IsComplete;

    await context.SaveChangesAsync();
    return Results.NoContent();
});


//Deleting a task
app.MapDelete("/todos/{id}", async (ToDoDbContext context, int id) =>
{
    var existingItem = await context.Items.FindAsync(id);
    if (existingItem == null)
    {
        return Results.NotFound();
    }

    context.Items.Remove(existingItem);
    await context.SaveChangesAsync();
    return Results.NoContent();
});



app.Run();
