using System.Xml.Linq;
using Google.Protobuf;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class CardDavProjectionTests
{
    private const string Card = """
        BEGIN:VCARD
        VERSION:3.0
        N:Hopper;Grace;Brewster;Rear Admiral;PhD
        FN:Grace Hopper
        NICKNAME:Amazing Grace
        ORG:US Navy;Programming Languages
        TITLE:Rear Admiral
        EMAIL;TYPE=INTERNET,WORK:grace@navy.example
        EMAIL;TYPE=INTERNET,HOME:grace@home.example
        TEL;TYPE=CELL,VOICE:+1 555 0100
        TEL;TYPE=WORK,VOICE:+1 555 0101
        TEL;TYPE=HOME,FAX:+1 555 0102
        ADR;TYPE=WORK:;;1 Navy Yard;Washington;DC;20374;USA
        BDAY:1906-12-09
        NOTE:Compiler pioneer
        URL:https://example.com/grace
        CATEGORIES:Colleagues,Legends
        X-RELIVE-CANARY:keepme
        END:VCARD
        """;

    [Fact]
    public void Projects_names_organisation_and_identity()
    {
        var result = CardDavContactSyncDriver.Project("addressbooks/personal/grace.vcf", "\"etag-1\"", Card)!;
        var c = result.Contact;

        Assert.Equal("addressbooks/personal/grace.vcf", result.ExternalId);
        Assert.Equal("\"etag-1\"", result.Etag);
        Assert.Equal("Grace", c.FirstName);
        Assert.Equal("Hopper", c.LastName);
        Assert.Equal("Brewster", c.MiddleName);
        Assert.Equal("Grace Hopper", c.FileAs);
        Assert.Equal("Amazing Grace", c.NickName);
        Assert.Equal("US Navy", c.CompanyName);
        Assert.Equal("Rear Admiral", c.JobTitle);
    }

    [Fact]
    public void Maps_phone_types_onto_the_named_slots()
    {
        var c = CardDavContactSyncDriver.Project("x.vcf", null, Card)!.Contact;

        Assert.Equal("+1 555 0100", c.MobilePhoneNumber);
        Assert.Equal("+1 555 0101", c.BusinessPhoneNumber);
        Assert.Equal("+1 555 0102", c.HomeFaxNumber);
    }

    [Fact]
    public void Projects_addresses_emails_and_categories()
    {
        var c = CardDavContactSyncDriver.Project("x.vcf", null, Card)!.Contact;

        Assert.Equal("grace@navy.example", c.Email1Address);
        Assert.Equal("grace@home.example", c.Email2Address);
        Assert.Equal("1 Navy Yard", c.BusinessAddressStreet);
        Assert.Equal("Washington", c.BusinessAddressCity);
        Assert.Equal(["Colleagues", "Legends"], c.Categories.Select(x => x.Name));
    }

    [Fact]
    public void Projection_is_deterministic_across_repeated_runs()
    {
        var first = CardDavContactSyncDriver.Project("x.vcf", null, Card)!.Contact;

        for (var i = 0; i < 5; i++)
            Assert.Equal(first.ToByteArray(), CardDavContactSyncDriver.Project("x.vcf", null, Card)!.Contact.ToByteArray());
    }

    [Fact]
    public void Falls_back_to_a_built_file_as_when_the_card_has_no_fn()
    {
        var c = CardDavContactSyncDriver.Project("x.vcf", null, """
            BEGIN:VCARD
            VERSION:3.0
            N:Lovelace;Ada;;;
            END:VCARD
            """)!.Contact;

        Assert.Equal("Lovelace, Ada", c.FileAs);
    }

    // hrefs come back as absolute server paths while the proxy resolves under the stored ServiceUrl
    [Theory]
    [InlineData("/addressbooks/wam/personal/", "https://dav.example/", "addressbooks/wam/personal/")]
    [InlineData("/remote.php/dav/addressbooks/wam/contacts/", "https://dav.example/remote.php/dav/", "addressbooks/wam/contacts/")]
    [InlineData("https://dav.example/remote.php/dav/addressbooks/wam/c/", "https://dav.example/remote.php/dav", "addressbooks/wam/c/")]
    [InlineData("/addressbooks/x/", null, "addressbooks/x/")]
    public void Relative_strips_the_service_url_prefix(string href, string? serviceUrl, string expected)
    {
        Assert.Equal(expected, CardDavContactSyncDriver.Relative(href, serviceUrl));
    }

    // a real, redacted iCloud card. vCardLib returns nothing at all when an X- property's value contains '=',
    // and Apple emits base64 in X-IMAGEHASH and X-ADDRESSING-GRAMMAR as a matter of course
    private const string AppleCard = """
        BEGIN:VCARD
        VERSION:3.0
        PRODID:-//Apple Inc.//iOS 15.4//EN
        N:;Redacted;;;
        FN:Redacted
        item1.X-ADDRESSING-GRAMMAR:JWq02OcFLToOfyunjkXngaWq3XGM//b4Pjv/PaVdfrnfG1Z4=
        EMAIL;type=INTERNET;type=HOME;type=pref:redacted@icloud.com
        TEL;type=HOME;type=VOICE;type=pref:441234567890
        BDAY;value=date:1950-05-10
        PHOTO;TYPE=JPEG;X-ABCROP-RECTANGLE=ABClipRect_1&0&0&320&320&PNu7TWLOuQpaKVIlc9JNxg==;VALUE=uri:https://gateway.icloud.com/contacts/1/ck/card/abc
        REV:2000-01-01T00:00:00Z
        UID:REDACTED
        X-IMAGETYPE:PHOTO
        X-IMAGEHASH:PNu7TWLOuQpaKVIlc9JNxg==
        END:VCARD
        """;

    [Fact]
    public void Parses_an_apple_card_carrying_base64_in_custom_properties()
    {
        var result = CardDavContactSyncDriver.Project("test.vcf", "\"e1\"", AppleCard);

        Assert.NotNull(result);
        Assert.Equal("Redacted", result.Contact.FileAs);
        Assert.Equal("redacted@icloud.com", result.Contact.Email1Address);
        Assert.Equal("441234567890", result.Contact.HomePhoneNumber);
        Assert.Equal(1950, result.Contact.Birthday.ToDateTime().Year);
    }

    // a PHOTO is either inline base64 or a uri, and iCloud uses the uri form against its own host
    [Fact]
    public void Reads_a_photo_uri_rather_than_treating_it_as_base64()
    {
        var result = CardDavContactSyncDriver.Project("grannie.vcf", null, AppleCard)!;

        Assert.Equal("https://gateway.icloud.com/contacts/1/ck/card/abc", result.PhotoUrl);
        Assert.Null(result.PhotoData);
    }

    [Fact]
    public void Reads_an_inline_base64_photo_as_bytes()
    {
        var result = CardDavContactSyncDriver.Project("inline.vcf", null, """
            BEGIN:VCARD
            VERSION:3.0
            FN:Inline
            PHOTO;ENCODING=b;TYPE=JPEG:/9j/4AAQSkZJRgABAQEAYABgAAD=
            END:VCARD
            """)!;

        Assert.NotNull(result.PhotoData);
        Assert.Null(result.PhotoUrl);
    }

    // the crop hash is base64, so its padding can land across a fold; the parameter has to be read
    // after unfolding or the trailing field is truncated
    [Fact]
    public void Reads_the_apple_crop_rectangle_across_a_folded_line()
    {
        var result = CardDavContactSyncDriver.Project("crop.vcf", null,
            "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Cropped\r\n"
            + "PHOTO;X-ABCROP-RECTANGLE=ABClipRect_1&28&21&679&679&cbA6M6uMSgIO+PyoJ0Byew=\r\n"
            + " =;VALUE=uri:https://gateway.icloud.com/contacts/123/ck/card/456\r\n"
            + "END:VCARD\r\n")!;

        Assert.Equal(new PhotoCrop(28, 21, 679, 679, OriginIsBottomLeft: true), result.PhotoCrop);
    }

    // iCloud sends a yearless birthday as 1604 with no marker at all: the X-APPLE-OMIT-YEAR
    // parameter Contacts.app writes into local exports does not survive CardDAV
    [Fact]
    public void Apples_placeholder_birth_year_is_clamped_at_projection()
    {
        var result = CardDavContactSyncDriver.Project("redacted.vcf", null, """
            BEGIN:VCARD
            VERSION:3.0
            PRODID:-//Apple Inc.//iOS 17.0//EN
            N:;redacted;;;
            FN:redacted
            BDAY;value=date:1604-09-23
            END:VCARD
            """)!;

        var birthday = result.Contact.Birthday.ToDateTime();

        Assert.Equal(ContactDates.UnknownYear, birthday.Year);
        Assert.Equal(9, birthday.Month);
        Assert.Equal(23, birthday.Day);
    }

    [Fact]
    public void A_photo_without_a_crop_rectangle_has_none()
    {
        var result = CardDavContactSyncDriver.Project("plain.vcf", null, """
            BEGIN:VCARD
            VERSION:3.0
            FN:Uncropped
            PHOTO;VALUE=uri:https://gateway.icloud.com/contacts/1/ck/card/abc
            END:VCARD
            """)!;

        Assert.Null(result.PhotoCrop);
    }

    [Theory]
    [InlineData("ABClipRect_1&28&21&0&679&hash")]
    [InlineData("ABClipRect_1&28&21&679")]
    [InlineData("ABClipRect_1&x&y&w&h&hash")]
    [InlineData("garbage")]
    public void An_unusable_crop_rectangle_is_ignored_rather_than_throwing(string rect)
    {
        var result = CardDavContactSyncDriver.Project("crop.vcf", null,
            $"BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Cropped\r\n"
            + $"PHOTO;X-ABCROP-RECTANGLE={rect};VALUE=uri:https://gateway.icloud.com/x\r\n"
            + "END:VCARD\r\n")!;

        Assert.Null(result.PhotoCrop);
    }

    private static XElement Response(string props) =>
        XElement.Parse($"""<response xmlns="DAV:"><propstat><prop>{props}</prop></propstat></response>""");

    [Fact]
    public void A_non_multistatus_response_is_reported_rather_than_parsed_as_empty()
    {
        var e = Assert.Throws<ContactSyncException>(() =>
            CardDavContactSyncDriver.ParseMultistatus("""<?xml version="1.0"?><D:error xmlns:D="DAV:"/>"""));

        Assert.Contains("expected multistatus", e.Message);
    }

    [Fact]
    public void Unparseable_xml_is_reported_rather_than_throwing_raw()
    {
        Assert.Throws<ContactSyncException>(() => CardDavContactSyncDriver.ParseMultistatus("not xml at all"));
    }
}
