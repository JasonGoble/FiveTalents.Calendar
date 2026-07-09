using System.Text.Json.Serialization;

using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Lectionary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.Services.AddSingleton<IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar>>(
    new Dictionary<LiturgicalTradition, ILiturgicalCalendar>
    {
        [LiturgicalTradition.AcnaBcp2019] = new AcnaBcp2019Calendar(),
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// GET /calendar/traditions
app.MapGet("/calendar/traditions",
    (IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar> calendars) =>
        calendars.Keys.Select(t => new { tradition = t, name = TraditionDisplayName(t) }))
    .WithName("GetTraditions");

// GET /translations
app.MapGet("/translations", () => Results.Ok(TranslationCatalog.GetAll()))
    .WithName("GetTranslations");

// GET /calendar/{tradition}/day/{date}
app.MapGet("/calendar/{tradition}/day/{date}",
    (string tradition, DateOnly date,
     IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar> calendars) =>
    {
        if (!TryResolve(tradition, calendars, out ILiturgicalCalendar? calendar))
        {
            return Results.NotFound($"Tradition '{tradition}' is not supported.");
        }

        return Results.Ok(calendar.GetDay(date));
    })
    .WithName("GetDay");

// GET /calendar/{tradition}/range?from=&to=
app.MapGet("/calendar/{tradition}/range",
    (string tradition, DateOnly from, DateOnly to,
     IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar> calendars) =>
    {
        if (!TryResolve(tradition, calendars, out ILiturgicalCalendar? calendar))
        {
            return Results.NotFound($"Tradition '{tradition}' is not supported.");
        }

        if (to < from)
        {
            return Results.BadRequest("'to' must be on or after 'from'.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            return Results.BadRequest("Range cannot exceed 366 days.");
        }

        return Results.Ok(calendar.GetRange(from, to));
    })
    .WithName("GetRange");

// GET /calendar/{tradition}/easter/{year}
app.MapGet("/calendar/{tradition}/easter/{year}",
    (string tradition, int year,
     IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar> calendars) =>
    {
        if (!TryResolve(tradition, calendars, out ILiturgicalCalendar? calendar))
        {
            return Results.NotFound($"Tradition '{tradition}' is not supported.");
        }

        if (year is < 1 or > 9999)
        {
            return Results.BadRequest("Year must be between 1 and 9999.");
        }

        DateOnly easter = calendar.GetEaster(year);
        return Results.Ok(new { year, date = easter });
    })
    .WithName("GetEaster");

app.Run();

static bool TryResolve(
    string tradition,
    IReadOnlyDictionary<LiturgicalTradition, ILiturgicalCalendar> calendars,
    out ILiturgicalCalendar? calendar)
{
    if (Enum.TryParse<LiturgicalTradition>(tradition, ignoreCase: true, out LiturgicalTradition t) &&
        calendars.TryGetValue(t, out calendar))
    {
        return true;
    }

    calendar = null;
    return false;
}

static string TraditionDisplayName(LiturgicalTradition tradition)
{
    return tradition switch
    {
        LiturgicalTradition.AcnaBcp2019 => "ACNA Book of Common Prayer 2019",
        LiturgicalTradition.RevisedCommonLectionary => "Revised Common Lectionary",
        LiturgicalTradition.CommonLectionary => "Common Lectionary (1983)",
        LiturgicalTradition.Episcopal => "Episcopal Church (TEC)",
        _ => tradition.ToString(),
    };
}

public partial class Program { }
