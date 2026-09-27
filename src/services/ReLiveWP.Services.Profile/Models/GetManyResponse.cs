using System.ServiceModel;

namespace ReLiveWP.Services.Profile.Models;

[MessageContract(IsWrapped = true, WrapperName = "GetManyResponse", WrapperNamespace = ProfileConstants.Ns)]
public class GetManyResponse
{
    [MessageBodyMember(Name = "GetManyResult", Namespace = ProfileConstants.Ns)]
    public GetProfilesResponse GetManyResult { get; set; } = new();
}
