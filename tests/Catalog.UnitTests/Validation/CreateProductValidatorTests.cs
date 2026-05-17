using Catalog.Application.Products.Commands;
using Catalog.Application.Products.Validators;
using FluentAssertions;

namespace Catalog.UnitTests.Validation;

public class CreateProductValidatorTests
{
    [Fact]
    public void Should_Fail_When_Price_Is_Zero()
    {
        var validator = new CreateProductCommandValidator();
        var result = validator.Validate(new CreateProductCommand("Name", "Desc", 0, 1));
        result.IsValid.Should().BeFalse();
    }
}
