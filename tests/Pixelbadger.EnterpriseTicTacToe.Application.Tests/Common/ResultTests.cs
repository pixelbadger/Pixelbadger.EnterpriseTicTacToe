using Pixelbadger.EnterpriseTicTacToe.Application;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Common;

[TestClass]
public sealed class ResultTests
{
    [TestMethod]
    public void Success_WithoutValue_ReturnsSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.ErrorType.ShouldBe(ResultErrorType.None);
        result.ErrorMessage.ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Failure_WithoutValue_ReturnsFailureResult()
    {
        var result = Result.Failure(ResultErrorType.NotFound, "Game not found");

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
        result.ErrorMessage.ShouldBe("Game not found");
    }

    [TestMethod]
    public void Success_WithValue_ReturnsSuccessfulTypedResult()
    {
        var result = Result.Success("ok");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
        result.ErrorType.ShouldBe(ResultErrorType.None);
        result.ErrorMessage.ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Failure_WithValue_ReturnsFailureTypedResult()
    {
        var result = Result.Failure<string>(ResultErrorType.Conflict, "conflict");

        result.IsFailure.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.ErrorType.ShouldBe(ResultErrorType.Conflict);
        result.ErrorMessage.ShouldBe("conflict");
    }

    [TestMethod]
    public void Failure_WhenErrorTypeIsNone_Throws()
    {
        Should.Throw<ArgumentException>(() => Result.Failure(ResultErrorType.None, "message"));
    }

    [TestMethod]
    public void Failure_WhenMessageIsEmpty_Throws()
    {
        Should.Throw<ArgumentException>(() => Result.Failure(ResultErrorType.Invalid, string.Empty));
    }
}
