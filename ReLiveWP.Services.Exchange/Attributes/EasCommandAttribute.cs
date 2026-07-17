using Microsoft.AspNetCore.Mvc.ActionConstraints;
using ReLiveWP.Services.Exchange.Middleware;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EasCommandAttribute(EasCommand command) : Attribute, IActionConstraint
{
    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        var items = context.RouteContext.HttpContext.Items;
        if (items[ActiveSyncMiddleware.ContextKey] is not ActiveSyncContext easContext)
            return false;

        return easContext.Command == command;
    }
}
