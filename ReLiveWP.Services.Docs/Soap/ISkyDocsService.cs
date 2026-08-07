using System.ServiceModel;

namespace ReLiveWP.Services.Docs.Soap;

[ServiceContract(Namespace = CloudDocuments.Ns)]
public interface ISkyDocsService
{
    [OperationContract(Action = "GetWebAccountInfo")]
    Task<GetWebAccountInfoResponse> GetWebAccountInfo(GetWebAccountInfoRequest request);

    [OperationContract(Action = "GetChangesSinceToken")]
    Task<GetChangesSinceTokenResponse> GetChangesSinceToken(GetChangesSinceTokenRequest request);

    [OperationContract(Action = "GetProductInfo")]
    Task<GetProductInfoResponse> GetProductInfo(GetProductInfoRequest request);

    [OperationContract(Action = "ResolveWebUrl")]
    Task<ResolveWebUrlResponse> ResolveWebUrl(ResolveWebUrlRequest request);
}
