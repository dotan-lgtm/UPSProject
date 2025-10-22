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
                                //  בדיקה אם הקובץ כבר קיים (גם בתוך childs)
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
                    if (child.TryGetProperty("user", out var userPropOuter) &&
                        userPropOuter.TryGetProperty("isManager", out var isManagerProp) &&
                        isManagerProp.GetBoolean() == false)
                    {
                        if (child.TryGetProperty("content", out var childContent))
                        {
                            var childStr = childContent.GetString();
                            if (!string.IsNullOrEmpty(childStr))
                            {
                                using var childDoc = JsonDocument.Parse(childStr);

                                //  attachments (מיילים)
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



            // שליפת trackno ו-custno מתוך user.content
            string trackno = "";
            string custno = "";

            if (firstObj.TryGetProperty("user", out var userMainProp))
            {
                if (userMainProp.TryGetProperty("content", out var userContentProp))
                {
                    var userContentStr = userContentProp.GetString();
                    if (!string.IsNullOrEmpty(userContentStr))
                    {
                        try
                        {
                            using var userContentJson = JsonDocument.Parse(userContentStr);
                            var rootUser = userContentJson.RootElement;

                            if (rootUser.TryGetProperty("track_no", out var trackNoProp))
                                trackno = trackNoProp.GetString() ?? "";

                            if (rootUser.TryGetProperty("cust_no", out var custNoProp))
                                custno = custNoProp.GetString() ?? "";
                        }
                        catch
                        {
                            Console.WriteLine(" לא הצלחנו לנתח את user.content");
                        }
                    }
                }
            }

            result.meta["trackno"] = trackno;
            result.meta["custno"] = custno;

        }




        return Results.Json(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Parsing error: {ex.Message}");
    }
});

app.MapPost("/api/uploadFiles", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
            return Results.BadRequest("Empty request body");

        var files = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(body);
        if (files == null || files.Count == 0)
            return Results.BadRequest("No files received");

        var httpClient = new HttpClient();
        var uploadResults = new List<object>();

        foreach (var file in files)
        {
            var name = file.GetValueOrDefault("name")?.ToString();
            var path = file.GetValueOrDefault("path")?.ToString();
            var doctypenum = file.GetValueOrDefault("tagCode")?.ToString() ?? "0";

            Console.WriteLine($"📥 מוריד קובץ: {name}");
            Console.WriteLine($"📄 קוד תיוג (doctypenum): {doctypenum}");
            bool success = false;
            string message = "";

            try
            {
                // הורדת הקובץ מה-Commbox
                var fileResponse = await httpClient.GetAsync(path);
                fileResponse.EnsureSuccessStatusCode();

                var fileBytes = await fileResponse.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(fileBytes);

                // קביעת סוג הקובץ לפי הסיומת
                var extension = System.IO.Path.GetExtension(name)?.Trim('.').ToLower() ?? "pdf";
                string contentType = extension switch
                {
                    "jpg" or "jpeg" => "jpg",
                    "png" => "png",
                    "pdf" => "pdf",
                    "doc" or "docx" => "docx",
                    "xls" or "xlsx" => "xlsx",
                    _ => "pdf"
                };

                // API body
                var uploadPayload = new
                {
                    fileName = name,
                    contentType = contentType,
                    fileContent = base64,
                    doctypenum = doctypenum,
                    trackno = file.GetValueOrDefault("trackno")?.ToString() ?? "",
                    custno = file.GetValueOrDefault("custno")?.ToString() ?? "",
                    fileno = ""
                };

                var json = JsonSerializer.Serialize(uploadPayload);
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // שליחת הקובץ ל-API החדש
                var uploadResponse = await httpClient.PostAsync(
                    "https://europe-west3-ups-testing-1.cloudfunctions.net/uploadFileBase64",
                    httpContent
                );

                var resultStr = await uploadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"🔄 תגובת API עבור {name}: {resultStr}");

                if (uploadResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonRes = JsonDocument.Parse(resultStr);
                        success = jsonRes.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
                        message = jsonRes.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                    }
                    catch
                    {
                        success = false;
                        message = "Parsing error in API response";
                    }
                }
                else
                {
                    success = false;
                    message = $"Upload request failed: {(int)uploadResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ שגיאה בהורדת או העלאת קובץ {name}: {ex.Message}");
                success = false;
                message = ex.Message;
            }

            uploadResults.Add(new { name, doctypenum, success, message });
        }

        return Results.Json(uploadResults);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error processing upload: {ex.Message}");
    }
});



app.Run();
