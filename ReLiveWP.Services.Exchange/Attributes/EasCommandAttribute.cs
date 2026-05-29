using Microsoft.AspNetCore.Mvc.ActionConstraints;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Attributes;

/// <summary>
/// Routes a controller or action to a specific EAS command by checking the
/// <see cref="ActiveSyncContext"/> populated by <see cref="ActiveSyncMiddleware"/>.
/// Apply to the controller class so that all actions in that controller are
/// implicitly constrained to one command.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EasCommandAttribute : Attribute, IActionConstraint
{
    private readonly EasCommand _command;

    public EasCommandAttribute(EasCommand command) => _command = command;

    /// <summary>
    /// Evaluated before unconstrained actions; all EAS command controllers run
    /// at the same order so only one will match per request.
    /// </summary>
    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        var items = context.RouteContext.HttpContext.Items;
        if (items[ActiveSyncMiddleware.ContextKey] is not ActiveSyncContext easContext)
            return false;

        return easContext.Command == _command;
    }
}
