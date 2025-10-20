using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/api/conversation/{id}", async (string id) =>
{
    var url = $"https://api.commbox.io/streams/BJ_bn1RYqzUJyXEV0adodrw%3d%3d/objects/{id}";
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImNtYi0zMi1kaWdpdC1rZXktaWQtMSJ9.eyJjbGllbnRfaWQiOiIzM2E1MjYyOWQwODg0ZTAxOWI4N2Q2M2UxNGJlY2FmMyIsImNsaWVudF9zZWNyZXQiOiI1Mzk5NGFiMmJlMzlhZjMwMjFjYjUwZDdlMDVmNTdjNGVlYzliMzdhMDMzMDAwYWMyNTZmOGZhNzRiMDcyZjcyIiwianRpIjoiOWIyOGE3ZTktMWVhYi00NTY2LTlkMzgtZGEzYjc5MjhkZGIwIiwibmJmIjoxNzYwOTQ2MzkyLCJleHAiOjE3ODgzNjgxOTQsImlhdCI6MTc2MDk0NjM5Mn0.1fn5o3xc7sf6IgbhCPjfz3Eato-ie51P7V-324QTips");

    var response = await client.GetAsync(url);
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest("Failed to fetch data from Commbox API");

    var content = await response.Content.ReadAsStringAsync();

    try
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var result = new
        {
            conversationId = id,
            files = new List<object>(),
            meta = new Dictionary<string, object?>()
        };

        if (root.TryGetProperty("data", out var dataArr) && dataArr.GetArrayLength() > 0)
        {
            var firstObj = dataArr[0];

            // ======================
            // בדיקה בתוך content הראשי
            // ======================
            if (firstObj.TryGetProperty("content", out var contentProp))
            {
                var contentStr = contentProp.GetString();
                if (!string.IsNullOrEmpty(contentStr))
                {
                    using var innerDoc = JsonDocument.Parse(contentStr);

                    // 1. אם יש attachments (מבנה ישן)
                    if (innerDoc.RootElement.TryGetProperty("attachments", out var attachments))
                    {
                        foreach (var file in attachments.EnumerateArray())
                        {
                            var path = file.GetProperty("path").GetString();
                            var name = file.GetProperty("name").GetString();
                            if (!string.IsNullOrEmpty(path))
                            {
                                result.files.Add(new { name, path });
                            }
                        }
                    }

                    // 2. אם יש link (מבנה חדש)
                    if (innerDoc.RootElement.TryGetProperty("link", out var link))
                    {
                        var href = link.TryGetProperty("href", out var hrefProp) ? hrefProp.GetString() : null;
                        var size = link.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt32() : 0;
                        if (!string.IsNullOrEmpty(href))
                        {
                            // הימנעות מהכפלה אם אותו קובץ כבר נוסף
                            if (!result.files.Any(f => f?.GetType().GetProperty("path")?.GetValue(f)?.ToString() == href))
                            {
                                // ✅ בדיקה אם הקובץ כבר קיים (גם בתוך childs)
                                bool alreadyExists = result.files.Any(f =>
                                    f?.GetType().GetProperty("path")?.GetValue(f)?.ToString() == href);

                                if (!alreadyExists)
                                {
                                    result.files.Add(new
                                    {
                                        name = System.IO.Path.GetFileName(href),
                                        path = href,
                                        size
                                    });
                                }
                            }

                        }
                    }
                }
            }

            // ======================
            // בדיקה גם בתוך childs (מבנה של הודעות המשך)
            // ======================
            if (firstObj.TryGetProperty("childs", out var childs) && childs.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in childs.EnumerateArray())
                {
                    // נבדוק קודם אם ההודעה נשלחה ע"י הלקוח בלבד
                    if (child.TryGetProperty("user", out var userProp) &&
                        userProp.TryGetProperty("isManager", out var isManagerProp) &&
                        isManagerProp.GetBoolean() == false)
                    {
                        if (child.TryGetProperty("content", out var childContent))
                        {
                            var childStr = childContent.GetString();
                            if (!string.IsNullOrEmpty(childStr))
                            {
                                using var childDoc = JsonDocument.Parse(childStr);

                                // 🔹 attachments (מיילים)
                                if (childDoc.RootElement.TryGetProperty("attachments", out var attachments))
                                {
                                    foreach (var file in attachments.EnumerateArray())
                                    {
                                        var path = file.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
                                        var name = file.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                                        var size = file.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt32() : 0;

                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            bool alreadyExists = result.files.Any(f =>
                                                f?.GetType().GetProperty("path")?.GetValue(f)?.ToString() == path);

                                            if (!alreadyExists)
                                            {
                                                result.files.Add(new
                                                {
                                                    name = name ?? System.IO.Path.GetFileName(path),
                                                    path,
                                                    size
                                                });
                                            }
                                        }
                                    }
                                }

                                // 🔹 link (וואטסאפ / וובצ'אט)
                                else if (childDoc.RootElement.TryGetProperty("link", out var link))
                                {
                                    var href = link.TryGetProperty("href", out var hrefProp) ? hrefProp.GetString() : null;
                                    var size = link.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt32() : 0;

                                    if (!string.IsNullOrEmpty(href))
                                    {
                                        bool alreadyExists = result.files.Any(f =>
                                            f?.GetType().GetProperty("path")?.GetValue(f)?.ToString() == href);

                                        if (!alreadyExists)
                                        {
                                            result.files.Add(new
                                            {
                                                name = System.IO.Path.GetFileName(href),
                                                path = href,
                                                size
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        return Results.Json(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Parsing error: {ex.Message}");
    }
});

app.Run();
