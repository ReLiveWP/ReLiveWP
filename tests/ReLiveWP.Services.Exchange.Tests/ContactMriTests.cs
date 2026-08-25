using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Tests;

public class ContactMriTests
{
    [Fact]
    public void A_live_contact_gets_only_the_passport_mri()
    {
        var mris = ContactMri.Derive("amy@relivewp.net", "WL", "some-object-id", "Regular");

        Assert.Equal(["1:amy@relivewp.net"], mris);
    }

    [Fact]
    public void A_facebook_contact_gets_the_via_form()
    {
        var mris = ContactMri.Derive(null, "FB", "12345", "Regular");

        Assert.Equal(["13:12345;via=14:fb"], mris);
    }

    [Fact]
    public void The_passport_mri_comes_first_so_it_lands_in_MainMri()
    {
        var mris = ContactMri.Derive("amy@relivewp.net", "FB", "12345", "Regular");

        Assert.Equal(["1:amy@relivewp.net", "13:12345;via=14:fb"], mris);
    }

    [Fact]
    public void A_fan_page_is_not_messageable()
    {
        var mris = ContactMri.Derive(null, "FB", "12345", "Fan");

        Assert.Empty(mris);
    }

    [Fact]
    public void A_network_that_is_not_im_enabled_gets_nothing()
    {
        // TWITR is a real source id but carries no IM offer
        Assert.Empty(ContactMri.Derive(null, "TWITR", "12345", "Regular"));
    }

    [Fact]
    public void An_unknown_source_id_gets_nothing()
    {
        Assert.Empty(ContactMri.Derive(null, "NOPE", "12345", "Regular"));
    }

    [Fact]
    public void Email_based_networks_are_skipped_until_we_serve_them()
    {
        // GOOG is EmailBasedXmpp, so an object id is not enough to build its MRI
        Assert.Empty(ContactMri.Derive(null, "GOOG", "12345", "Regular"));
    }

    [Fact]
    public void A_contact_with_nothing_to_go_on_gets_nothing()
    {
        Assert.Empty(ContactMri.Derive(null, null, null, null));
        Assert.Empty(ContactMri.Derive(null, "FB", null, "Regular"));
    }
}
