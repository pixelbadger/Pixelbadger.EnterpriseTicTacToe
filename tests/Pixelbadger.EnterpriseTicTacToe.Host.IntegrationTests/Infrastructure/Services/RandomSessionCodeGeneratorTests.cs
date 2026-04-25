using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Services;

[TestClass]
public sealed class RandomSessionCodeGeneratorTests
{
    [TestMethod]
    public void GenerateCode_AlwaysReturnsConfiguredLengthAlphanumericValue()
    {
        var generator = new RandomSessionCodeGenerator();

        for (var index = 0; index < 50; index++)
        {
            var code = generator.GenerateCode();
            code.Length.ShouldBe(GameSession.SessionCodeLength);
            code.All(char.IsAsciiLetterOrDigit).ShouldBeTrue();
            code.ShouldBe(code.ToUpperInvariant());
        }
    }
}
