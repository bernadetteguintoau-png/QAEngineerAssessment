using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace QAEngineerAssessment;

[TestFixture]
public class QAEngineerAssessment : PageTest
{
    private const string BaseUrl = "https://demo.snipeitapp.com";
    private const string Username = "admin";
    private const string Password = "password";
    private const string ModelSearch = "Macbook Pro 13";
    private const string StatusName = "Ready to Deploy";

    private static readonly Random Rng = new();

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
    };

    [Test]
    public async Task CreateCheckoutAndValidateMacBookAsset()
    {
        Page.SetDefaultTimeout(15000);

        try
        {
            await LoginAsync();
            var (assetTag, userName) = await CreateAndCheckoutAssetAsync();
            await FindAssetInListAsync(assetTag);
            await ValidateAssetPageAsync(assetTag, userName);
            await ValidateHistoryAsync(assetTag, userName);
            TestContext.WriteLine("ALL CHECKS PASSED");
        }
        catch
        {
            await Page.ScreenshotAsync(new() { Path = "failure.png", FullPage = true });
            TestContext.WriteLine("Test failed - screenshot saved to failure.png");
            throw;
        }
    }

    private async Task LoginAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/login");

        var usernameInput = Page.Locator("input[name='username']:not([aria-hidden='true']), input#username");
        await usernameInput.First.WaitForAsync();
        await usernameInput.First.FillAsync(Username);

        var passwordInput = Page.Locator("input[name='password']:not([aria-hidden='true']), input#password, input[type='password']:not([aria-hidden='true'])");
        await passwordInput.First.WaitForAsync();
        await passwordInput.First.FillAsync(Password);

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("login|sign in", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.WaitForURLAsync(new Regex(@"^(?!.*/login).*$"));
        await Expect(Page.Locator("body")).Not.ToContainTextAsync("These credentials do not match");
    }

    private async Task<(string AssetTag, string UserName)> CreateAndCheckoutAssetAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/hardware/create");

        await PickSelect2Async("model_select_id", ModelSearch);
        await PickSelect2Async("status_select_id", StatusName);

        var assetTag = await Page.InputValueAsync("#asset_tag");
        Assert.That(assetTag, Is.Not.Empty, "Asset tag was not auto-generated");

        var userName = await PickSelect2Async("assigned_user_select", randomOption: true);

        await Page.Locator("#submit_button").ClickAsync();
        await Page.WaitForURLAsync(new Regex(@".*/hardware(/\d+)?$"));
        await Expect(Page.Locator("body")).ToContainTextAsync(new Regex("success", RegexOptions.IgnoreCase));

        TestContext.WriteLine($"Created asset {assetTag}, checked out to: {userName}");
        return (assetTag, userName);
    }

    private async Task FindAssetInListAsync(string assetTag)
    {
        await Page.GotoAsync($"{BaseUrl}/hardware");
        await Page.Locator("input.search-input").First.FillAsync(assetTag);
        await Page.Keyboard.PressAsync("Enter");

        var row = Page.Locator("table tbody tr", new() { HasTextString = assetTag });
        await Expect(row).ToHaveCountAsync(1);
        await Expect(row).ToContainTextAsync("Macbook Pro 13", new() { IgnoreCase = true });

        await row.GetByRole(AriaRole.Link, new() { Name = assetTag }).First.ClickAsync();
    }

    private async Task ValidateAssetPageAsync(string assetTag, string userName)
    {
        var body = Page.Locator("body");
        await Expect(body).ToContainTextAsync(assetTag);
        await Expect(body).ToContainTextAsync("Macbook Pro 13", new() { IgnoreCase = true });
        await Expect(body).ToContainTextAsync("Deployed"); 
        await Expect(body).ToContainTextAsync(NameOnly(userName));
    }

    private async Task ValidateHistoryAsync(string assetTag, string userName)
    {
        await Page.Locator("a[href='#history']").First.ClickAsync();

        var history = Page.Locator("#history");
        await Expect(history).ToBeVisibleAsync();

        var checkoutRow = history.Locator("table tbody tr")
            .Filter(new() { HasTextRegex = new Regex("checkout", RegexOptions.IgnoreCase) });
        await Expect(checkoutRow.First).ToBeVisibleAsync(new() { Timeout = 30000 });

        var text = await history.InnerTextAsync();
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Match("(?i)checkout"), "No checkout entry in history");
            Assert.That(text, Does.Match("(?i)create"), "No create entry in history");
            Assert.That(text, Does.Contain(assetTag), "Asset tag not found in history");
            Assert.That(text, Does.Contain(NameOnly(userName)), "Checkout target not found in history");
            Assert.That(text.ToLowerInvariant(), Does.Contain("admin"), "Admin not shown in history");
        });
    }

    private async Task<string> PickSelect2Async(string containerId, string? search = null, bool randomOption = false)
    {
        await Page.ClickAsync($"#select2-{containerId}-container");
        if (search != null)
            await Page.FillAsync(".select2-search__field", search);

        var options = Page.Locator(".select2-results__option:not(.loading-results):not(.select2-results__message):not([aria-disabled='true'])");

        if (search != null)
            options = options.Filter(new() { HasTextRegex = new Regex(Regex.Escape(search), RegexOptions.IgnoreCase) });

        await options.First.WaitForAsync();

        var count = await options.CountAsync();
        Assert.That(count, Is.GreaterThan(0), $"No options found for {containerId}");

        var option = randomOption ? options.Nth(Rng.Next(count)) : options.First;
        var text = (await option.InnerTextAsync()).Trim();
        await option.ClickAsync();
        return text;
    }

    private static string NameOnly(string userText) =>
        Regex.Split(userText, @"\s*\(")[0].Trim();
}