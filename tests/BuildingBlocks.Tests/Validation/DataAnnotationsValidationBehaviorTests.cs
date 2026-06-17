using BuildingBlocks.Validation;
using FluentAssertions;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Tests.Validation;

// Test request records must implement IRequest<T> because of the generic constraint
// on DataAnnotationsValidationBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
file sealed record ValidRequest(
    [property: Required][property: MinLength(3)] string Name) : IRequest<Unit>;

file sealed record RequestWithEmail(
    [property: Required][property: EmailAddress] string Email) : IRequest<Unit>;

public class DataAnnotationsValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new DataAnnotationsValidationBehavior<ValidRequest, Unit>();
        var request = new ValidRequest("Test Name");
        var nextCalled = false;

        await behavior.Handle(
            request,
            () => { nextCalled = true; return Task.FromResult(Unit.Value); },
            default);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequestWithTooShortName_ThrowsRequestValidationException()
    {
        var behavior = new DataAnnotationsValidationBehavior<ValidRequest, Unit>();
        var request = new ValidRequest("AB"); // MinLength is 3

        Func<Task> act = () => behavior.Handle(
            request,
            () => Task.FromResult(Unit.Value),
            default);

        var ex = await act.Should().ThrowAsync<RequestValidationException>();
        ex.Which.Errors.Should().Contain(e => e.Field.Contains("Name"));
    }

    [Fact]
    public async Task Handle_RequestWithInvalidEmail_ThrowsRequestValidationException()
    {
        var behavior = new DataAnnotationsValidationBehavior<RequestWithEmail, Unit>();
        var request = new RequestWithEmail("not-an-email");

        Func<Task> act = () => behavior.Handle(
            request,
            () => Task.FromResult(Unit.Value),
            default);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task Handle_ValidationException_ContainsErrors()
    {
        var behavior = new DataAnnotationsValidationBehavior<ValidRequest, Unit>();
        var request = new ValidRequest("AB");

        try
        {
            await behavior.Handle(request, () => Task.FromResult(Unit.Value), default);
        }
        catch (RequestValidationException ex)
        {
            ex.Errors.Should().NotBeEmpty();
            ex.Errors.Should().AllSatisfy(e =>
            {
                e.Field.Should().NotBeNullOrWhiteSpace();
                e.Message.Should().NotBeNullOrWhiteSpace();
            });
        }
    }
}
