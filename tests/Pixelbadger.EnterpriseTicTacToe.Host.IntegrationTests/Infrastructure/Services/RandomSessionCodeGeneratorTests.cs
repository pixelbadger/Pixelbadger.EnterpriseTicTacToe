using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Services;

[TestClass]
public sealed class RandomSessionCodeGeneratorTests
{
    [TestMethod]
    public void GenerateCode_AlwaysReturnsSixCharacterAlphanumericValue()
    {
        var generator = new RandomSessionCodeGenerator();

        for (var index = 0; index < 50; index++)
        {
            var code = generator.GenerateCode();
            code.Length.ShouldBe(6);
            code.All(char.IsAsciiLetterOrDigit).ShouldBeTrue();
            code.ShouldBe(code.ToUpperInvariant());
        }
    }
}
