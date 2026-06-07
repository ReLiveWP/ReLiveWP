using System.ServiceModel;

namespace ReLiveWP.Services.Profile.Models;

[MessageContract(IsWrapped = true, WrapperName = "GetMany", WrapperNamespace = ProfileConstants.Ns)]
public class GetManyRequest
{
    [MessageHeader(Name = "SOAPApplicationHeader", Namespace = ProfileConstants.Ns)]
    public SOAPApplicationHeader? ApplicationHeader { get; set; }

    [MessageHeader(Name = "SOAPUserHeader", Namespace = ProfileConstants.Ns)]
    public SOAPUserHeader? UserHeader { get; set; }

    [MessageBodyMember(Name = "request", Namespace = ProfileConstants.Ns)]
    public GetProfilesRequest Request { get; set; } = new();
}
