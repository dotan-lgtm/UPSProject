using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
);

app.MapGet("/api/conversation/{id}", async (string id) =>
{
    var url = $"https://api.commbox.io/streams/pE6q0uPbXiwBNLiTvKsDMw%3d%3d/objects/{id}";
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImNtYi0zMi1kaWdpdC1rZXktaWQtMSJ9.eyJjbGllbnRfaWQiOiIzM2E1MjYyOWQwODg0ZTAxOWI4N2Q2M2UxNGJlY2FmMyIsImNsaWVudF9zZWNyZXQiOiI1Mzk5NGFiMmJlMzlhZjMwMjFjYjUwZDdlMDVmNTdjNGVlYzliMzdhMDMzMDAwYWMyNTZmOGZhNzRiMDcyZjcyIiwianRpIjoiMjFlMjRkYTUtY2IyOS00Njk5LTk1YWMtMzJhYmVjN2YwYzkwIiwibmJmIjoxNzYwNTEzMzQ4LCJleHAiOjE3ODgzNjgxOTQsImlhdCI6MTc2MDUxMzM0OH0.UAe8W28bOpq_4s4wA6DY7D4oReJ_dEFWZ76Qg312z20");

    var response = await client.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();

    return Results.Content(content, "application/json");
});

app.Run();
