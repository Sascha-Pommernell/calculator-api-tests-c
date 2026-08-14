using System.Text;
using System.Text.Json;
using Allure.Net.Commons;
using Allure.NUnit;
using Microsoft.Playwright;

namespace CalculatorApiTests;

/// <summary>
/// Basisklasse für die Calculator-API-Tests.
/// Die API wird nicht aus diesem Repository gestartet, sondern extern betrieben.
/// Die Basis-URL ist über die Umgebungsvariable API_BASE_URL konfigurierbar
/// (Standard: http://localhost:5116).
/// Jede Fixture hält bewusst ihre eigene Playwright-Instanz: Playwright ist nicht
/// threadsicher, die Fixtures laufen aber parallel (siehe AssemblyInfo).
/// </summary>
[AllureNUnit]
public abstract class ApiTestBase
{
    private IPlaywright? _playwright;
    protected IAPIRequestContext Request = null!;

    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5116";

    private static bool IsCi =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    protected static string Endpoint(string op) => $"/api/calculator/{op}";

    /// <summary>Namen der API-Operationen (Pfadsegmente der Endpunkte).</summary>
    protected static class Ops
    {
        public const string Add = "add";
        public const string Subtract = "subtract";
        public const string Multiply = "multiply";
        public const string Divide = "divide";
    }

    /// <summary>Von der API zurückgegebene Bezeichnung der Operation (Response-Feld <c>operation</c>).</summary>
    protected static readonly IReadOnlyDictionary<string, string> OperationName = new Dictionary<string, string>
    {
        [Ops.Add] = "Addition",
        [Ops.Subtract] = "Subtraktion",
        [Ops.Multiply] = "Multiplikation",
        [Ops.Divide] = "Division",
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUpPlaywright()
    {
        _playwright = await Playwright.CreateAsync();
        await EnsureApiReachableAsync(_playwright);
    }

    [OneTimeTearDown]
    public void OneTimeTearDownPlaywright()
    {
        _playwright?.Dispose();
        _playwright = null;
    }

    [SetUp]
    public async Task SetUpApiContext()
    {
        Request = await _playwright!.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            Timeout = 10_000,
        });
    }

    [TearDown]
    public async Task TearDownApiContext()
    {
        if (Request is not null)
        {
            await Request.DisposeAsync();
            Request = null!;
        }
    }

    protected Task<IAPIResponse> CalcAsync(string op, double[] numbers) =>
        PostAsync(op, new() { DataObject = new { numbers } });

    /// <summary>POST auf einen Calculator-Endpunkt, aufgezeichnet als Allure-Step mit Request/Response-Attachments.</summary>
    protected Task<IAPIResponse> PostAsync(string op, APIRequestContextOptions options) =>
        AllureApi.Step($"POST {Endpoint(op)}", async () =>
        {
            AttachRequest(options);
            var response = await Request.PostAsync(Endpoint(op), options);
            await AttachResponseAsync(response);
            return response;
        });

    /// <summary>GET auf einen Pfad, aufgezeichnet als Allure-Step mit Response-Attachment.</summary>
    protected Task<IAPIResponse> GetAsync(string path) =>
        AllureApi.Step($"GET {path}", async () =>
        {
            var response = await Request.GetAsync(path);
            await AttachResponseAsync(response);
            return response;
        });

    private static void AttachRequest(APIRequestContextOptions options)
    {
        var body = options.Data as string
            ?? (options.DataObject is not null ? JsonSerializer.Serialize(options.DataObject) : null);
        if (!string.IsNullOrEmpty(body))
        {
            AllureApi.AddAttachment("Request-Body", "application/json", Encoding.UTF8.GetBytes(body), ".json");
        }
    }

    private static async Task AttachResponseAsync(IAPIResponse response)
    {
        var body = await response.TextAsync();
        var text = $"HTTP {response.Status} {response.StatusText}\n" +
                   $"Content-Type: {response.Headers.GetValueOrDefault("content-type", "-")}\n\n{body}";
        AllureApi.AddAttachment($"Response (HTTP {response.Status})", "text/plain", Encoding.UTF8.GetBytes(text), ".txt");
    }

    /// <summary>Prüft ProblemDetails-Struktur gemäß RFC 9457 (title, status, Content-Type; optional errors-Objekt).</summary>
    protected static Task ExpectProblemDetailsAsync(IAPIResponse response, bool expectErrors = false) =>
        AllureApi.Step("Prüfe ProblemDetails-Struktur (RFC 9457)", async () =>
        {
            var body = await response.JsonAsync();
            Assert.Multiple(() =>
            {
                Assert.That(response.Headers.GetValueOrDefault("content-type"), Does.Contain("application/problem+json"));
                Assert.That(body?.GetProperty("title").GetString(), Is.Not.Null.And.Not.Empty);
                Assert.That(body?.GetProperty("status").GetInt32(), Is.EqualTo(response.Status));
                if (expectErrors)
                {
                    var hasErrors = body is { } b
                        && b.TryGetProperty("errors", out var errors)
                        && errors.ValueKind == JsonValueKind.Object
                        && errors.EnumerateObject().Any();
                    Assert.That(hasErrors, Is.True, "Erwartet ein nicht-leeres 'errors'-Objekt (Modelvalidierung).");
                }
            });
        });

    /// <summary>Bricht die Fixture ab, wenn die API nicht erreichbar ist (Probe: GET /health).</summary>
    private static async Task EnsureApiReachableAsync(IPlaywright playwright)
    {
        var context = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            Timeout = 5000,
        });
        try
        {
            var response = await context.GetAsync("/health");
            if (!response.Ok)
            {
                ReportUnreachable($"Health-Endpunkt lieferte HTTP {response.Status}.");
            }
        }
        catch (PlaywrightException ex)
        {
            ReportUnreachable(ex.Message);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    /// <summary>Lokal: Inconclusive (Umgebungsproblem). In CI: harter Fehlschlag, damit die Pipeline nicht „grün ohne Tests“ wird.</summary>
    private static void ReportUnreachable(string reason)
    {
        var message =
            $"Die Calculator-API ist unter '{BaseUrl}' nicht erreichbar. " +
            "Bitte API starten oder API_BASE_URL korrekt setzen. " +
            $"Fehler: {reason}";

        if (IsCi)
        {
            Assert.Fail(message);
        }
        else
        {
            Assert.Inconclusive(message);
        }
    }
}
