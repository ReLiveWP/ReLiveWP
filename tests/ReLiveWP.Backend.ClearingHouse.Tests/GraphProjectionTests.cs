using System.Text.Json;
using Google.Protobuf;
using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class GraphProjectionTests
{
    private const string Contact = """
        {
          "id": "AAMkAGI2THVSAAA=",
          "@odata.etag": "W/\"EQAAABYAAAB\"",
          "displayName": "Grace Hopper",
          "givenName": "Grace",
          "middleName": "Brewster",
          "surname": "Hopper",
          "title": "Rear Admiral",
          "generation": "PhD",
          "nickName": "Amazing Grace",
          "fileAs": "Hopper, Grace",
          "companyName": "US Navy",
          "department": "Programming Languages",
          "jobTitle": "Systems Engineer",
          "officeLocation": "Navy Yard",
          "businessHomePage": "https://example.com/grace",
          "personalNotes": "Compiler pioneer",
          "spouseName": "Vincent",
          "birthday": "1906-12-09T00:00:00Z",
          "categories": ["Colleagues", "Legends"],
          "emailAddresses": [
            { "name": "Work", "address": "grace@navy.example" },
            { "name": "Home", "address": "grace@home.example" }
          ],
          "homePhones": ["+1 555 0100", "+1 555 0101"],
          "businessPhones": ["+1 555 0102"],
          "mobilePhone": "+1 555 0103",
          "imAddresses": ["grace@im.example"],
          "homeAddress": {
            "street": "1 Navy Yard", "city": "Washington", "state": "DC",
            "countryOrRegion": "USA", "postalCode": "20374"
          },
          "businessAddress": {},
          "otherAddress": {}
        }
        """;

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static RemoteContact Project(string json) => GraphContactSyncDriver.Project(Parse(json))!;

    [Fact]
    public void Projects_identity_and_etag()
    {
        var result = Project(Contact);

        Assert.Equal("AAMkAGI2THVSAAA=", result.ExternalId);
        Assert.Equal("W/\"EQAAABYAAAB\"", result.Etag);
        Assert.Equal("Grace", result.Contact.FirstName);
        Assert.Equal("Brewster", result.Contact.MiddleName);
        Assert.Equal("Hopper", result.Contact.LastName);
        Assert.Equal("Hopper, Grace", result.Contact.FileAs);
    }

    // Graph splits the honorific prefix and the job title across `title` and `jobTitle`, and putting
    // the job title in the honorific slot is the easy mistake
    [Fact]
    public void Separates_the_honorific_from_the_job_title()
    {
        var c = Project(Contact).Contact;

        Assert.Equal("Rear Admiral", c.Title);
        Assert.Equal("Systems Engineer", c.JobTitle);
        Assert.Equal("PhD", c.Suffix);
    }

    [Fact]
    public void Projects_organisation_notes_and_spouse()
    {
        var c = Project(Contact).Contact;

        Assert.Equal("US Navy", c.CompanyName);
        Assert.Equal("Programming Languages", c.Department);
        Assert.Equal("Navy Yard", c.OfficeLocation);
        Assert.Equal("Compiler pioneer", c.Notes);
        Assert.Equal("https://example.com/grace", c.WebPage);
        Assert.Equal("Vincent", c.Spouse);
        Assert.Equal("Amazing Grace", c.NickName);
    }

    [Fact]
    public void Maps_phone_lists_onto_the_named_slots()
    {
        var c = Project(Contact).Contact;

        Assert.Equal("+1 555 0103", c.MobilePhoneNumber);
        Assert.Equal("+1 555 0100", c.HomePhoneNumber);
        Assert.Equal("+1 555 0101", c.Home2PhoneNumber);
        Assert.Equal("+1 555 0102", c.BusinessPhoneNumber);
    }

    [Fact]
    public void Projects_emails_im_and_categories()
    {
        var c = Project(Contact).Contact;

        Assert.Equal("grace@navy.example", c.Email1Address);
        Assert.Equal("grace@home.example", c.Email2Address);
        Assert.Equal("grace@im.example", c.ImAddress);
        Assert.Equal(["Colleagues", "Legends"], c.Categories.Select(x => x.Name));
    }

    [Fact]
    public void Projects_the_home_address()
    {
        var c = Project(Contact).Contact;

        Assert.Equal("1 Navy Yard", c.HomeAddressStreet);
        Assert.Equal("Washington", c.HomeAddressCity);
        Assert.Equal("DC", c.HomeAddressState);
        Assert.Equal("20374", c.HomeAddressPostalCode);
        Assert.Equal("USA", c.HomeAddressCountry);
    }

    // Graph sends an empty object for an address that was never filled in
    [Fact]
    public void An_empty_address_object_does_not_occupy_a_slot()
    {
        var c = Project(Contact).Contact;

        Assert.False(c.HasBusinessAddressStreet);
        Assert.False(c.HasOtherAddressStreet);
    }

    [Fact]
    public void Projects_the_birthday()
    {
        var birthday = Project("""{ "id": "1", "birthday": "1972-04-17T00:00:00Z" }""").Contact.Birthday.ToDateTime();

        Assert.Equal(new DateTime(1972, 4, 17, 0, 0, 0, DateTimeKind.Utc), birthday);
    }

    // EAS cannot carry a year before 1908, so the day survives and the year becomes the placeholder
    [Fact]
    public void A_birth_year_below_the_floor_is_clamped()
    {
        var birthday = Project(Contact).Contact.Birthday.ToDateTime();

        Assert.Equal(ContactDates.UnknownYear, birthday.Year);
        Assert.Equal(12, birthday.Month);
        Assert.Equal(9, birthday.Day);
    }

    // the photo is fetched later through the proxy, so the driver only records where to find it
    [Fact]
    public void Records_a_proxied_photo_path_rather_than_a_url()
    {
        var result = Project(Contact);

        Assert.Equal("microsoft", result.PhotoServiceId);
        Assert.Equal("graph.microsoft.com/v1.0/me/contacts/AAMkAGI2THVSAAA%3D/photo/$value", result.PhotoUrl);
        Assert.Null(result.PhotoData);
    }

    // a contact read out of a folder has to be addressed through that folder, not the default one
    [Fact]
    public void A_photo_in_a_named_folder_is_addressed_through_it()
    {
        var result = GraphContactSyncDriver.Project(Parse(Contact), "AAMkAGZvbGRlcg==")!;

        Assert.Equal(
            "graph.microsoft.com/v1.0/me/contactFolders/AAMkAGZvbGRlcg%3D%3D/contacts/AAMkAGI2THVSAAA%3D/photo/$value",
            result.PhotoUrl);
    }

    [Fact]
    public void Falls_back_to_the_display_name_when_there_is_no_file_as()
    {
        var c = Project("""
            { "id": "1", "displayName": "Ada Lovelace", "givenName": "Ada", "surname": "Lovelace" }
            """).Contact;

        Assert.Equal("Ada Lovelace", c.FileAs);
    }

    [Fact]
    public void Falls_back_to_a_built_file_as_when_there_is_neither()
    {
        var c = Project("""{ "id": "1", "givenName": "Ada", "surname": "Lovelace" }""").Contact;

        Assert.Equal("Lovelace, Ada", c.FileAs);
    }

    [Fact]
    public void A_contact_without_an_id_is_not_projected()
        => Assert.Null(GraphContactSyncDriver.Project(Parse("""{ "displayName": "Nobody" }""")));

    [Fact]
    public void Projection_is_deterministic_across_repeated_runs()
    {
        var first = Project(Contact).Contact.ToByteArray();

        for (var i = 0; i < 5; i++)
            Assert.Equal(first, Project(Contact).Contact.ToByteArray());
    }
}
