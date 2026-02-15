using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Services;

[TestClass]
public sealed class HmacClientIdentityHasherTests
{
    [TestMethod]
    public void Hash_WithSameInputAndKey_IsDeterministic()
    {
        var sut = CreateHasher("key-one");

        var first = sut.Hash("client-1");
        var second = sut.Hash("client-1");

        first.ShouldBe(second);
    }

    [TestMethod]
    public void Hash_WithDifferentInputs_ProducesDifferentHashes()
    {
        var sut = CreateHasher("key-one");

        var first = sut.Hash("client-1");
        var second = sut.Hash("client-2");

        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Hash_WithDifferentKeys_ProducesDifferentHashes()
    {
        var firstHasher = CreateHasher("key-one");
        var secondHasher = CreateHasher("key-two");

        var first = firstHasher.Hash("client-1");
        var second = secondHasher.Hash("client-1");

        first.ShouldNotBe(second);
    }

    [TestMethod]
    public void Hash_WithBlankIdentity_ThrowsArgumentException()
    {
        var sut = CreateHasher("key-one");

        Should.Throw<ArgumentException>(() => sut.Hash(" "));
    }

    private static HmacClientIdentityHasher CreateHasher(string key)
    {
        return new HmacClientIdentityHasher(Options.Create(new ClientIdentityOptions
        {
            HashKey = key
        }));
    }
}
