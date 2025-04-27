var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()   //  Access-Control-Allow-Origin: *
            .AllowAnyHeader()   //  Access-Control-Allow-Headers: *
            .AllowAnyMethod()); //  Access-Control-Allow-Methods: *
});
var app = builder.Build();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ai}/{action=Index}/{id?}");

app.Run();