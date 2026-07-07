using FluentAssertions;
using VoucherSystem.Application;

namespace VoucherSystem.UnitTests;

public class BrandServiceTests
{
    // --- Name validation ---

    [Fact]
    public void ValidateName_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BrandService.ValidateName(""));
    }

    [Fact]
    public void ValidateName_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BrandService.ValidateName("   "));
    }

    [Fact]
    public void ValidateName_WithValidName_DoesNotThrow()
    {
        BrandService.ValidateName("My Brand");
    }

    [Fact]
    public void ValidateName_WithNameTooLong_ThrowsArgumentException()
    {
        var longName = new string('A', 201);
        Assert.Throws<ArgumentException>(() => BrandService.ValidateName(longName));
    }

    // --- URL validation ---

    [Fact]
    public void ValidateUrl_WithNullUrl_DoesNotThrow()
    {
        BrandService.ValidateUrl(null!, "WebsiteUrl");
    }

    [Fact]
    public void ValidateUrl_WithEmptyUrl_DoesNotThrow()
    {
        BrandService.ValidateUrl("", "WebsiteUrl");
    }

    [Fact]
    public void ValidateUrl_WithHttpUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("http://example.com", "WebsiteUrl"));
    }

    [Fact]
    public void ValidateUrl_WithHttpsUrl_DoesNotThrow()
    {
        BrandService.ValidateUrl("https://example.com", "WebsiteUrl");
    }

    [Fact]
    public void ValidateUrl_WithLocalhostUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("https://localhost:3000", "WebsiteUrl"));
    }

    [Fact]
    public void ValidateUrl_WithLocalhostSubdomainUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("https://localhost", "WebsiteUrl"));
    }

    [Fact]
    public void ValidateUrl_WithIPv4Url_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("https://192.168.1.1", "WebsiteUrl"));
    }

    [Fact]
    public void ValidateUrl_WithIPv6Url_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("https://[::1]", "WebsiteUrl"));
    }

    [Fact]
    public void ValidateUrl_WithInvalidUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateUrl("not-a-url", "WebsiteUrl"));
    }

    // --- Email validation ---

    [Fact]
    public void ValidateEmail_WithNullEmail_DoesNotThrow()
    {
        BrandService.ValidateEmail(null!);
    }

    [Fact]
    public void ValidateEmail_WithEmptyEmail_DoesNotThrow()
    {
        BrandService.ValidateEmail("");
    }

    [Fact]
    public void ValidateEmail_WithValidEmail_DoesNotThrow()
    {
        BrandService.ValidateEmail("support@example.com");
    }

    [Fact]
    public void ValidateEmail_WithInvalidEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateEmail("not-an-email"));
    }

    [Fact]
    public void ValidateEmail_WithTooLongEmail_ThrowsArgumentException()
    {
        var local = new string('a', 250);
        var longEmail = $"{local}@b.com";
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateEmail(longEmail));
    }

    // --- Color validation ---

    [Fact]
    public void ValidateColor_WithNullColor_DoesNotThrow()
    {
        BrandService.ValidateColor(null!, "PrimaryColor");
    }

    [Fact]
    public void ValidateColor_WithEmptyColor_DoesNotThrow()
    {
        BrandService.ValidateColor("", "PrimaryColor");
    }

    [Fact]
    public void ValidateColor_WithValidHexColor_DoesNotThrow()
    {
        BrandService.ValidateColor("#FF5733", "PrimaryColor");
        BrandService.ValidateColor("#ffffff", "PrimaryColor");
        BrandService.ValidateColor("#000000", "PrimaryColor");
    }

    [Fact]
    public void ValidateColor_WithInvalidHexColor_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateColor("#GGGGGG", "PrimaryColor"));
    }

    [Fact]
    public void ValidateColor_WithShortHexColor_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BrandService.ValidateColor("#FFF", "PrimaryColor"));
    }

    // --- URL sanitization ---

    [Fact]
    public void SanitizeUrl_WithNull_ReturnsNull()
    {
        BrandService.SanitizeUrl(null).Should().BeNull();
    }

    [Fact]
    public void SanitizeUrl_WithEmpty_ReturnsNull()
    {
        BrandService.SanitizeUrl("").Should().BeNull();
    }

    [Fact]
    public void SanitizeUrl_WithTrailingSlash_RemovesIt()
    {
        BrandService.SanitizeUrl("https://example.com/").Should().Be("https://example.com");
    }

    [Fact]
    public void SanitizeUrl_WithWhitespace_Trims()
    {
        BrandService.SanitizeUrl("  https://example.com  ").Should().Be("https://example.com");
    }
}
