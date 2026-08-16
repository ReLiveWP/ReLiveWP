using ReLiveWP.ServiceDefaults.Contacts;

using ServiceCaps = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceCapabilities;
using ServiceFlags = ReLiveWP.Backend.ConnectedServices.Data.LiveConnectedServiceFlags;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

// ConnectedServices owns these enums and everyone else reads the bits off a Connection over gRPC.
// Nothing in the type system ties the two together, so renumbering here would silently change what
// another service believes a connection is: mirrors would stop noticing a busted token, or transient
// import connections would never get cleaned up. This is what makes that a build failure.
public class ConnectionWireParityTests
{
    [Fact]
    public void The_flags_on_the_wire_match_the_enum()
    {
        Assert.Equal((ulong)ServiceFlags.Transient, ConnectionConsts.TransientFlag);
        Assert.Equal((ulong)ServiceFlags.Busted, ConnectionConsts.BustedFlag);
    }

    [Fact]
    public void The_capabilities_on_the_wire_match_the_enum()
    {
        Assert.Equal((uint)ServiceCaps.Contacts, ConnectionConsts.ContactsCapability);
        Assert.Equal((uint)ServiceCaps.Calendar, ConnectionConsts.CalendarCapability);
    }
}
