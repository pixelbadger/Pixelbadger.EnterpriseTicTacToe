using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Endpoints.Common;

[TestClass]
public sealed class ResultPostProcessorTests
{
    [TestMethod]
    [DataRow(ResultErrorType.Invalid, 400)]
    [DataRow(ResultErrorType.Forbidden, 403)]
    [DataRow(ResultErrorType.NotFound, 404)]
    [DataRow(ResultErrorType.Conflict, 409)]
    [DataRow(ResultErrorType.None, 500)]
    public void ToStatusCode_MapsResultErrorTypeToHttpStatus(ResultErrorType errorType, int statusCode)
    {
        var result = ResultPostProcessor<object, object>.ToStatusCode(errorType);

        result.ShouldBe(statusCode);
    }

    [TestMethod]
    [DataRow(ResultErrorType.Invalid, "Request validation failed")]
    [DataRow(ResultErrorType.Forbidden, "Forbidden")]
    [DataRow(ResultErrorType.NotFound, "Not found")]
    [DataRow(ResultErrorType.Conflict, "Conflict")]
    [DataRow(ResultErrorType.None, "An unexpected error occurred")]
    public void ToTitle_MapsResultErrorTypeToProblemTitle(ResultErrorType errorType, string title)
    {
        var result = ResultPostProcessor<object, object>.ToTitle(errorType);

        result.ShouldBe(title);
    }
}
