using System.Text.Json;
using Google.Protobuf;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class GoogleProjectionTests
{
    private const string Person = """
        {
          "resourceName": "people/c123",
          "etag": "%EgUBAj0P",
          "names": [{ "givenName": "Ada", "familyName": "Lovelace", "displayName": "Ada Lovelace" }],
          "emailAddresses": [
            { "value": "ada@example.com", "type": "home" },
            { "value": "ada@work.example", "type": "work" },
            { "value": "ada@third.example" },
            { "value": "ada@fourth.example" },
            { "value": "ada@fifth.example" }
          ],
          "phoneNumbers": [
            { "value": "+44 7700 900001", "type": "mobile" },
            { "value": "+44 20 7946 0001", "type": "home" },
            { "value": "+44 7700 900002", "type": "mobile" }
          ],
          "addresses": [
            { "type": "home", "streetAddress": "1 Test Street", "city": "London", "postalCode": "E1 6AN", "country": "UK" }
          ],
          "organizations": [{ "name": "Analytical Engines", "title": "Mathematician" }],
          "birthdays": [{ "date": { "year": 1815, "month": 12, "day": 10 } }],
          "photos": [{ "url": "https://lh3.googleusercontent.com/a/ACg8ocABC123=s100" }]
        }
        """;

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Projects_identity()
    {
        var result = GoogleContactSyncDriver.Project(Parse(Person))!;

        Assert.Equal("people/c123", result.ExternalId);
        Assert.Equal("%EgUBAj0P", result.Etag);
    }

    // Google sizes on a trailing =s{n}, so the existing one is replaced rather than appended to,
    // which is what stops us downloading a full-size photo per contact
    [Fact]
    public void Photo_url_asks_google_for_the_tile_size()
    {
        var result = GoogleContactSyncDriver.Project(Parse(Person))!;

        Assert.Equal($"https://lh3.googleusercontent.com/a/ACg8ocABC123=s{ContactPhotoService.TileSize}-c",
            result.PhotoUrl);
    }

    [Fact]
    public void Fills_the_three_email_slots_in_provider_order()
    {
        var c = GoogleContactSyncDriver.Project(Parse(Person))!.Contact;

        Assert.Equal("ada@example.com", c.Email1Address);
        Assert.Equal("ada@work.example", c.Email2Address);
        Assert.Equal("ada@third.example", c.Email3Address);
    }

    // EAS phone slots are named, so a second mobile has nowhere of its own to go. It must land
    // somewhere deterministic rather than overwrite the first.
    [Fact]
    public void Second_phone_of_a_kind_overflows_instead_of_overwriting()
    {
        var c = GoogleContactSyncDriver.Project(Parse(Person))!.Contact;

        Assert.Equal("+44 7700 900001", c.MobilePhoneNumber);
        Assert.Equal("+44 20 7946 0001", c.HomePhoneNumber);
        Assert.Equal("+44 7700 900002", c.Home2PhoneNumber);
    }

    [Fact]
    public void Projects_names_organisation_address_and_birthday()
    {
        var c = GoogleContactSyncDriver.Project(Parse(Person))!.Contact;

        Assert.Equal("Ada", c.FirstName);
        Assert.Equal("Lovelace", c.LastName);
        Assert.Equal("Ada Lovelace", c.FileAs);
        Assert.Equal("Analytical Engines", c.CompanyName);
        Assert.Equal("Mathematician", c.JobTitle);
        Assert.Equal("1 Test Street", c.HomeAddressStreet);
        Assert.Equal("London", c.HomeAddressCity);
        // 1815 is Lovelace's real birth year, and it still clamps: the rule cannot tell a historical
        // date from a placeholder, and nobody alive was born before 1909. The card keeps the truth.
        Assert.Equal(ContactDates.UnknownYear, c.Birthday.ToDateTime().Year);
        Assert.Equal(new DateTime(ContactDates.UnknownYear, 12, 10), c.Birthday.ToDateTime().Date);
    }

    // the mirror overwrites the whole record every poll, so an assignment that varies between runs
    // emits a phantom Change forever instead of degrading quietly
    [Fact]
    public void Projection_is_deterministic_across_repeated_runs()
    {
        var first = GoogleContactSyncDriver.Project(Parse(Person))!.Contact;

        for (var i = 0; i < 5; i++)
        {
            var again = GoogleContactSyncDriver.Project(Parse(Person))!.Contact;
            Assert.Equal(first.ToByteArray(), again.ToByteArray());
        }
    }

    [Fact]
    public void A_person_with_no_resource_name_is_skipped()
    {
        Assert.Null(GoogleContactSyncDriver.Project(Parse("""{ "names": [{ "givenName": "Nobody" }] }""")));
    }

    // Google leaves the year off when the user did not give one. EAS cannot say "year unknown", so
    // the day and month are kept against the placeholder year rather than thrown away.
    [Fact]
    public void Birthday_without_a_year_keeps_its_day_and_month()
    {
        var c = GoogleContactSyncDriver.Project(Parse("""
            { "resourceName": "people/c1", "birthdays": [{ "date": { "month": 4, "day": 1 } }] }
            """))!.Contact;

        Assert.Equal(new DateTime(ContactDates.UnknownYear, 4, 1), c.Birthday.ToDateTime().Date);
    }

    [Fact]
    public void A_birthday_with_no_day_at_all_is_still_dropped()
    {
        var c = GoogleContactSyncDriver.Project(Parse("""
            { "resourceName": "people/c1", "birthdays": [{ "date": { "year": 1990 } }] }
            """))!.Contact;

        Assert.Null(c.Birthday);
    }

    // the silhouette Google generates is not a photo, and fetching one per contact costs a round trip
    [Fact]
    public void Default_silhouette_photo_is_ignored()
    {
        var result = GoogleContactSyncDriver.Project(Parse("""
            {
              "resourceName": "people/c1",
              "photos": [{ "url": "https://example.com/silhouette.jpg", "default": true }]
            }
            """))!;

        Assert.Null(result.PhotoUrl);
    }

    [Fact]
    public void FileAs_falls_back_when_google_sends_no_display_name()
    {
        var c = GoogleContactSyncDriver.Project(Parse("""
            { "resourceName": "people/c1", "names": [{ "givenName": "Grace", "familyName": "Hopper" }] }
            """))!.Contact;

        Assert.Equal("Hopper, Grace", c.FileAs);
    }

    [Fact]
    public void A_contact_with_only_an_email_still_gets_a_file_as()
    {
        var c = GoogleContactSyncDriver.Project(Parse("""
            { "resourceName": "people/c1", "emailAddresses": [{ "value": "someone@example.com" }] }
            """))!.Contact;

        Assert.Equal("someone@example.com", c.FileAs);
    }
}
