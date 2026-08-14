using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using Microsoft.Playwright;

namespace CalculatorApiTests;

/// <summary>4.1 Happy Path – Grundrechenarten (erwartet: 200 OK).</summary>
[TestFixture]
[Category("Prio-Hoch")]
[AllureSuite("4.1 Happy Path")]
[AllureSeverity(SeverityLevel.critical)]
public class HappyPathTests : ApiTestBase
{
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return new TestCaseData(Ops.Add, new double[] { 1, 2 }, 3d).SetArgDisplayNames("TC-ADD-01 add 1+2=3");
        yield return new TestCaseData(Ops.Add, new double[] { 1, 2, 3, 4 }, 10d).SetArgDisplayNames("TC-ADD-02 add 1+2+3+4=10");
        yield return new TestCaseData(Ops.Add, new double[] { -5, 2.5 }, -2.5).SetArgDisplayNames("TC-ADD-03 add -5+2.5=-2.5");
        yield return new TestCaseData(Ops.Subtract, new double[] { 10, 4 }, 6d).SetArgDisplayNames("TC-SUB-01 subtract 10-4=6");
        yield return new TestCaseData(Ops.Subtract, new double[] { 10, 4, 3 }, 3d).SetArgDisplayNames("TC-SUB-02 subtract 10-4-3=3");
        yield return new TestCaseData(Ops.Subtract, new double[] { -1, -1 }, 0d).SetArgDisplayNames("TC-SUB-03 subtract -1--1=0");
        yield return new TestCaseData(Ops.Multiply, new double[] { 3, 4 }, 12d).SetArgDisplayNames("TC-MUL-01 multiply 3*4=12");
        yield return new TestCaseData(Ops.Multiply, new double[] { 2, 3, 4 }, 24d).SetArgDisplayNames("TC-MUL-02 multiply 2*3*4=24");
        yield return new TestCaseData(Ops.Multiply, new double[] { 5, 0 }, 0d).SetArgDisplayNames("TC-MUL-03 multiply 5*0=0");
        yield return new TestCaseData(Ops.Multiply, new double[] { -2, 2.5 }, -5d).SetArgDisplayNames("TC-MUL-04 multiply -2*2.5=-5");
        yield return new TestCaseData(Ops.Divide, new double[] { 10, 4 }, 2.5).SetArgDisplayNames("TC-DIV-01 divide 10/4=2.5");
        yield return new TestCaseData(Ops.Divide, new double[] { 100, 5, 2 }, 10d).SetArgDisplayNames("TC-DIV-02 divide 100/5/2=10");
        yield return new TestCaseData(Ops.Divide, new double[] { -9, 3 }, -3d).SetArgDisplayNames("TC-DIV-03 divide -9/3=-3");
        yield return new TestCaseData(Ops.Divide, new double[] { 0, 5 }, 0d).SetArgDisplayNames("TC-DIV-04 divide 0/5=0");
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Operation_liefert_korrektes_Ergebnis(string op, double[] numbers, double result)
    {
        var response = await CalcAsync(op, numbers);
        Assert.That(response.Status, Is.EqualTo(200));
        var body = (await response.JsonAsync())!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("operation").GetString(), Is.EqualTo(OperationName[op]));
            Assert.That(body.GetProperty("numbers").EnumerateArray().Select(e => e.GetDouble()), Is.EqualTo(numbers));
            Assert.That(body.GetProperty("result").GetDouble(), Is.EqualTo(result));
        });
    }
}

/// <summary>4.2 Gleitkomma-Randfälle.</summary>
[TestFixture]
[Category("Prio-Mittel")]
[AllureSuite("4.2 Gleitkomma-Randfälle")]
[AllureSeverity(SeverityLevel.normal)]
public class FloatingPointTests : ApiTestBase
{
    [Test(Description = "TC-FLT-01: add(0.1, 0.2) ≈ 0.3 (Toleranzvergleich)")]
    public async Task TC_FLT_01_Add_0_1_und_0_2()
    {
        var response = await CalcAsync(Ops.Add, [0.1, 0.2]);
        Assert.That(response.Status, Is.EqualTo(200));
        var body = (await response.JsonAsync())!.Value;
        Assert.That(body.GetProperty("result").GetDouble(), Is.EqualTo(0.3).Within(1e-10));
    }

    [Test(Description = "TC-FLT-02: add(1e308, 1e308) → 400 (Überlauf)")]
    public async Task TC_FLT_02_Add_Ueberlauf()
    {
        var response = await CalcAsync(Ops.Add, [1e308, 1e308]);
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test(Description = "TC-FLT-03: divide(1e308, 1e-308) → 400 (Überlauf)")]
    public async Task TC_FLT_03_Divide_Ueberlauf()
    {
        var response = await CalcAsync(Ops.Divide, [1e308, 1e-308]);
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test(Description = "TC-FLT-04: divide(1, 3) ≈ 0.3333… (Toleranzvergleich)")]
    public async Task TC_FLT_04_Divide_1_durch_3()
    {
        var response = await CalcAsync(Ops.Divide, [1, 3]);
        Assert.That(response.Status, Is.EqualTo(200));
        var body = (await response.JsonAsync())!.Value;
        Assert.That(body.GetProperty("result").GetDouble(), Is.EqualTo(1d / 3d).Within(1e-10));
    }

    [Test(Description = "TC-FLT-05: multiply(1e308, 1e308) → 400 (Überlauf)")]
    public async Task TC_FLT_05_Multiply_Ueberlauf()
    {
        var response = await CalcAsync(Ops.Multiply, [1e308, 1e308]);
        Assert.That(response.Status, Is.EqualTo(400));
    }

    [Test(Description = "TC-FLT-06: subtract(-1e308, 1e308) → 400 (Überlauf ins negative Infinity)")]
    public async Task TC_FLT_06_Subtract_Ueberlauf()
    {
        var response = await CalcAsync(Ops.Subtract, [-1e308, 1e308]);
        Assert.That(response.Status, Is.EqualTo(400));
    }
}

/// <summary>4.3 Division durch null (erwartet: 400 Bad Request).</summary>
[TestFixture]
[Category("Prio-Hoch")]
[AllureSuite("4.3 Division durch null")]
[AllureSeverity(SeverityLevel.critical)]
public class DivisionByZeroTests : ApiTestBase
{
    [Test(Description = "TC-DIV0-01: divide(10, 0) → 400 mit ProblemDetails")]
    public async Task TC_DIV0_01_Divide_durch_null()
    {
        var response = await CalcAsync(Ops.Divide, [10, 0]);
        Assert.That(response.Status, Is.EqualTo(400));
        var body = (await response.JsonAsync())!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("title").GetString(), Is.EqualTo("Ungültige Berechnung"));
            Assert.That(body.GetProperty("detail").GetString(), Does.Contain("Division durch null"));
        });
    }

    [Test(Description = "TC-DIV0-02: divide(10, 2, 0) → 400 (null an späterer Position)")]
    public async Task TC_DIV0_02_Divide_null_an_spaeterer_Position()
    {
        var response = await CalcAsync(Ops.Divide, [10, 2, 0]);
        Assert.That(response.Status, Is.EqualTo(400));
    }
}

/// <summary>4.4 Eingabevalidierung (erwartet: 400, alle 4 Endpunkte; TC-VAL-08: 415).</summary>
[TestFixture("add")]
[TestFixture("subtract")]
[TestFixture("multiply")]
[TestFixture("divide")]
[Category("Prio-Hoch")]
[AllureSuite("4.4 Eingabevalidierung")]
[AllureSeverity(SeverityLevel.critical)]
public class ValidationTests(string op) : ApiTestBase
{
    [Test(Description = "TC-VAL-01: mit nur einer Zahl → 400")]
    public async Task TC_VAL_01_Nur_eine_Zahl()
    {
        var response = await CalcAsync(op, [42]);
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-02: mit leerem Array → 400")]
    public async Task TC_VAL_02_Leeres_Array()
    {
        var response = await CalcAsync(op, []);
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-03: ohne Feld numbers → 400")]
    public async Task TC_VAL_03_Ohne_Feld_numbers()
    {
        var response = await PostAsync(op, new() { DataObject = new { } });
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-04: mit leerem Body → 400")]
    public async Task TC_VAL_04_Leerer_Body()
    {
        var response = await PostAsync(op, new()
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Data = "",
        });
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-05: mit ungültigem Typ → 400")]
    public async Task TC_VAL_05_Ungueltiger_Typ()
    {
        var response = await PostAsync(op, new()
        {
            DataObject = new { numbers = new object[] { 1, "abc" } },
        });
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-06: mit syntaktisch ungültigem JSON → 400")]
    public async Task TC_VAL_06_Ungueltiges_JSON()
    {
        var response = await PostAsync(op, new()
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Data = "{ \"numbers\": [1, 2",
        });
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-07: mit numbers = null → 400")]
    public async Task TC_VAL_07_Numbers_null()
    {
        var response = await PostAsync(op, new()
        {
            DataObject = new { numbers = (double[]?)null },
        });
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }

    [Test(Description = "TC-VAL-08: mit Content-Type text/plain → 415")]
    public async Task TC_VAL_08_ContentType_text_plain()
    {
        var response = await PostAsync(op, new()
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
            Data = "{\"numbers\":[1,2]}",
        });
        Assert.That(response.Status, Is.EqualTo(415));
    }

    [Test(Description = "TC-VAL-09: mit mehr als 1000 Zahlen → 400 (Obergrenze)")]
    public async Task TC_VAL_09_Zu_viele_Zahlen()
    {
        var response = await CalcAsync(op, [.. Enumerable.Repeat(1d, 1001)]);
        Assert.That(response.Status, Is.EqualTo(400));
        await ExpectProblemDetailsAsync(response, expectErrors: true);
    }
}

/// <summary>4.5 API-Vertrag / Robustheit.</summary>
[TestFixture]
[Category("Prio-Mittel")]
[AllureSuite("4.5 API-Vertrag")]
[AllureSeverity(SeverityLevel.normal)]
public class ContractTests : ApiTestBase
{
    [Test(Description = "TC-CON-01: Response enthält genau operation, numbers, result")]
    public async Task TC_CON_01_Response_Felder()
    {
        var response = await CalcAsync(Ops.Add, [1, 2]);
        Assert.That(response.Status, Is.EqualTo(200));
        var body = (await response.JsonAsync())!.Value;
        var keys = body.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);
        Assert.That(keys, Is.EqualTo(new[] { "numbers", "operation", "result" }));
    }

    [Test(Description = "TC-CON-02: Content-Type ist application/json")]
    public async Task TC_CON_02_ContentType()
    {
        var response = await CalcAsync(Ops.Add, [1, 2]);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Headers["content-type"], Does.Contain("application/json"));
    }

    [Test(Description = "TC-CON-03: GET auf POST-Endpunkt → 405 Method Not Allowed")]
    public async Task TC_CON_03_Get_auf_Post_Endpunkt()
    {
        var response = await GetAsync(Endpoint(Ops.Add));
        Assert.That(response.Status, Is.EqualTo(405));
    }

    [Test(Description = "TC-CON-04: Unbekannte Operation /modulo → 404 Not Found")]
    public async Task TC_CON_04_Unbekannte_Operation()
    {
        var response = await PostAsync("modulo", new()
        {
            DataObject = new { numbers = new double[] { 1, 2 } },
        });
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [Test(Description = "TC-CON-05: Unbekannte Zusatzfelder im Body werden toleriert → 200")]
    public async Task TC_CON_05_Zusatzfelder_toleriert()
    {
        var response = await PostAsync(Ops.Add, new()
        {
            DataObject = new { numbers = new double[] { 1, 2 }, extra = true },
        });
        Assert.That(response.Status, Is.EqualTo(200));
        var body = (await response.JsonAsync())!.Value;
        Assert.That(body.GetProperty("result").GetDouble(), Is.EqualTo(3));
    }
}
