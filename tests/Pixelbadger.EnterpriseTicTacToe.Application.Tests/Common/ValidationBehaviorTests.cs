using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Common;

[TestClass]
public sealed class ValidationBehaviorTests
{
    [TestMethod]
    public async Task Handle_WhenNoValidatorRegistered_CallsNext()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var sut = new ValidationBehavior<TestCommand, string>(provider);
        var nextCallCount = 0;

        var result = await sut.Handle(
            new TestCommand("value"),
            (_, _) =>
            {
                nextCallCount++;
                return new ValueTask<Result<string>>(Result.Success("ok"));
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
        nextCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenValidatorFails_ReturnsInvalidResultWithoutCallingNext()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IValidator<TestCommand>, TestCommandValidator>()
            .BuildServiceProvider();
        var sut = new ValidationBehavior<TestCommand, string>(provider);
        var nextCallCount = 0;

        var result = await sut.Handle(
            new TestCommand(string.Empty),
            (_, _) =>
            {
                nextCallCount++;
                return new ValueTask<Result<string>>(Result.Success("ok"));
            },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Invalid);
        result.ErrorMessage.ShouldContain("Value");
        nextCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WhenValidatorPasses_CallsNext()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IValidator<TestCommand>, TestCommandValidator>()
            .BuildServiceProvider();
        var sut = new ValidationBehavior<TestCommand, string>(provider);
        var nextCallCount = 0;

        var result = await sut.Handle(
            new TestCommand("valid"),
            (_, _) =>
            {
                nextCallCount++;
                return new ValueTask<Result<string>>(Result.Success("done"));
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("done");
        nextCallCount.ShouldBe(1);
    }

    private sealed record TestCommand(string Value) : ICommand<Result<string>>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(command => command.Value).NotEmpty();
        }
    }
}
