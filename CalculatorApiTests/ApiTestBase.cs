using Allure.NUnit;
using Microsoft.Playwright;

namespace CalculatorApiTests;

/// <summary>
/// Basisklasse für die Calculator-API-Tests.
/// Die API wird nicht aus diesem Repository gestartet, sondern extern betrieben.
/// Die Basis-URL ist über die Umgebungsvariable API_BASE_URL konfigurierbar
/// (Standard: http://localhost:5116).
/// </summary>
[AllureNUnit]
public abstract class ApiTestBase
{
    private IPlaywright? _playwright;
    protected IAPIRequestContext Request = null!;

    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5116";

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
        Request.PostAsync(Endpoint(op), new() { DataObject = new { numbers } });

    /// <summary>Prüft ProblemDetails-Struktur gemäß RFC 9457 (title, status, Content-Type).</summary>
    protected static async Task ExpectProblemDetailsAsync(IAPIResponse response)
    {
        var body = await response.JsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.GetValueOrDefault("content-type"), Does.Contain("application/problem+json"));
            Assert.That(body?.GetProperty("title").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(body?.GetProperty("status").GetInt32(), Is.EqualTo(response.Status));
        });
    }

    /// <summary>Bricht die Fixture mit klarer Meldung ab, wenn die API nicht erreichbar ist.</summary>
    private static async Task EnsureApiReachableAsync(IPlaywright playwright)
    {
        var context = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            Timeout = 5000,
        });
        try
        {
            await context.GetAsync("/");
        }
        catch (PlaywrightException ex)
        {
            Assert.Inconclusive(
                $"Die Calculator-API ist unter '{BaseUrl}' nicht erreichbar. " +
                "Bitte API starten oder API_BASE_URL korrekt setzen. " +
                $"Fehler: {ex.Message}");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}
