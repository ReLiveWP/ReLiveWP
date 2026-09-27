using System.ComponentModel.DataAnnotations;
using ReLiveWP.Services.Login.Models.Sso;

namespace ReLiveWP.Services.Login.Tests;

public class SignUpBindingTests
{
    private static SignUpViewModel CompleteModel() => new()
    {
        PendingId = "pending",
        InviteCode = "BCDFG-HJKMP-QRTVW-XY234-6789B",
        EmailAddress = "new@example.com",
        Username = "newbie",
        Password = "hunter22",
        ConfirmPassword = "hunter22",
    };

    private static List<ValidationResult> ValidateModel(SignUpViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Remember_me_defaults_to_false_so_an_absent_value_cannot_mean_remembered()
    {
        Assert.False(new SignUpViewModel().RememberMe);
    }

    [Fact]
    public void Complete_form_is_valid()
    {
        Assert.Empty(ValidateModel(CompleteModel()));
    }

    [Fact]
    public void Mismatched_passwords_fail_on_the_confirm_field()
    {
        var model = CompleteModel();
        model.ConfirmPassword = "hunter23";

        var results = ValidateModel(model);

        var failure = Assert.Single(results);
        Assert.Contains(nameof(SignUpViewModel.ConfirmPassword), failure.MemberNames);
    }

    [Fact]
    public void Missing_invite_fails()
    {
        var model = CompleteModel();
        model.InviteCode = "";

        var results = ValidateModel(model);

        var failure = Assert.Single(results);
        Assert.Contains(nameof(SignUpViewModel.InviteCode), failure.MemberNames);
    }
}
