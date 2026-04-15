using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var notes = new List<Note>();
var nextId = 1;

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        time = DateTime.UtcNow
    });
});

app.MapGet("/version", (IConfiguration config) =>
{
    var appName = config["App:Name"] ?? "IsLabApp";
    var appVersion = config["App:Version"] ?? "0.1.0";

    return Results.Ok(new
    {
        name = appName,
        version = appVersion
    });
});

app.MapGet("/api/notes", () =>
{
    return Results.Ok(notes);
});

app.MapGet("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    return note is null
        ? Results.NotFound(new { message = "Заметка не найдена" })
        : Results.Ok(note);
});

app.MapPost("/api/notes", (CreateNoteRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "Поле title обязательно" });
    }

    var note = new Note
    {
        Id = nextId++,
        Title = request.Title.Trim(),
        Text = request.Text?.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    notes.Add(note);

    return Results.Created($"/api/notes/{note.Id}", note);
});

app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    if (note is null)
    {
        return Results.NotFound(new { message = "Заметка не найдена" });
    }

    notes.Remove(note);
    return Results.Ok(new { message = "Заметка удалена" });
});

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Строка подключения ConnectionStrings:Mssql не задана");
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        return Results.Ok(new
        {
            status = "ok",
            message = "Подключение к БД успешно"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка подключения к БД: {ex.Message}");
    }
});

app.Run();

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNoteRequest
{
    public string Title { get; set; } = "";
    public string? Text { get; set; }
}