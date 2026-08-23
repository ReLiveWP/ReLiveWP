using ReLiveWP.Services.Login.Models.Sso;

namespace ReLiveWP.Services.Login.Tests;

public class SignInBindingTests
{
    [Fact]
    public void Remember_me_defaults_to_false_so_an_absent_value_cannot_mean_remembered()
    {
        Assert.False(new SignInViewModel().RememberMe);
    }
}
