using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace ReLiveWP.Backend.DeviceRegistration
{
    public class DeviceRegistrationService : DeviceRegistration.DeviceRegistrationBase
    {
        public override Task<DeviceRegistrationResponse> RegisterDevice(DeviceRegistrationRequest request, ServerCallContext context)
        {
            // TODO: store registration information
            var deviceRegistrationResponse = new DeviceRegistrationResponse() { Succeeded = true };
            return Task.FromResult(deviceRegistrationResponse);
        }
    }
}
