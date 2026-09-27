namespace ReLiveWP.Services.Devices.Services
{
    public interface ICarrierLookupService
    {
        CarrierInfo? GetCarrierInfo(string carrierId);
    }
}