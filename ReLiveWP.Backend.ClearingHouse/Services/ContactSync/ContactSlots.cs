using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public enum PhoneSlot { Mobile, Home, Home2, Business, Business2, HomeFax, BusinessFax, Pager, Car, CompanyMain, Radio, Assistant, Other }

public enum AddressSlot { Home, Business, Other }

public static class ContactSlots
{
    public static void SetEmails(ContactItem item, IEnumerable<string> emails)
    {
        var ordered = Clean(emails);
        if (ordered.Count > 0) item.Email1Address = ordered[0];
        if (ordered.Count > 1) item.Email2Address = ordered[1];
        if (ordered.Count > 2) item.Email3Address = ordered[2];
    }

    public static void SetImAddresses(ContactItem item, IEnumerable<string> addresses)
    {
        var ordered = Clean(addresses);
        if (ordered.Count > 0) item.ImAddress = ordered[0];
        if (ordered.Count > 1) item.ImAddress2 = ordered[1];
        if (ordered.Count > 2) item.ImAddress3 = ordered[2];
    }

    public static void SetPhones(ContactItem item, IEnumerable<(PhoneSlot Slot, string Number)> phones)
    {
        var overflow = new List<string>();
        var taken = new HashSet<PhoneSlot>();

        foreach (var (slot, number) in phones)
        {
            if (string.IsNullOrWhiteSpace(number)) continue;

            if (!taken.Add(slot))
            {
                overflow.Add(number);
                continue;
            }

            switch (slot)
            {
                case PhoneSlot.Mobile: item.MobilePhoneNumber = number; break;
                case PhoneSlot.Home: item.HomePhoneNumber = number; break;
                case PhoneSlot.Home2: item.Home2PhoneNumber = number; break;
                case PhoneSlot.Business: item.BusinessPhoneNumber = number; break;
                case PhoneSlot.Business2: item.Business2PhoneNumber = number; break;
                case PhoneSlot.HomeFax: item.HomeFaxNumber = number; break;
                case PhoneSlot.BusinessFax: item.BusinessFaxNumber = number; break;
                case PhoneSlot.Pager: item.PagerNumber = number; break;
                case PhoneSlot.Car: item.CarPhoneNumber = number; break;
                case PhoneSlot.CompanyMain: item.CompanyMainPhone = number; break;
                case PhoneSlot.Radio: item.RadioPhoneNumber = number; break;
                case PhoneSlot.Assistant: item.AssistantPhoneNumber = number; break;
                default: overflow.Add(number); break;
            }
        }

        foreach (var number in overflow)
        {
            if (!item.HasHome2PhoneNumber) { item.Home2PhoneNumber = number; continue; }
            if (!item.HasBusiness2PhoneNumber) { item.Business2PhoneNumber = number; continue; }
            break;
        }
    }

    public static void SetAddress(ContactItem item, AddressSlot slot,
                                  string? street, string? city, string? state, string? postalCode, string? country)
    {
        switch (slot)
        {
            case AddressSlot.Home:
                Set(street, v => item.HomeAddressStreet = v);
                Set(city, v => item.HomeAddressCity = v);
                Set(state, v => item.HomeAddressState = v);
                Set(postalCode, v => item.HomeAddressPostalCode = v);
                Set(country, v => item.HomeAddressCountry = v);
                break;
            case AddressSlot.Business:
                Set(street, v => item.BusinessAddressStreet = v);
                Set(city, v => item.BusinessAddressCity = v);
                Set(state, v => item.BusinessAddressState = v);
                Set(postalCode, v => item.BusinessAddressPostalCode = v);
                Set(country, v => item.BusinessAddressCountry = v);
                break;
            default:
                Set(street, v => item.OtherAddressStreet = v);
                Set(city, v => item.OtherAddressCity = v);
                Set(state, v => item.OtherAddressState = v);
                Set(postalCode, v => item.OtherAddressPostalCode = v);
                Set(country, v => item.OtherAddressCountry = v);
                break;
        }
    }

    public static string? BuildFileAs(ContactItem item)
    {
        if (item.HasLastName && item.HasFirstName)
            return $"{item.LastName}, {item.FirstName}";

        foreach (var candidate in new[]
        {
            item.HasLastName ? item.LastName : null,
            item.HasFirstName ? item.FirstName : null,
            item.HasNickName ? item.NickName : null,
            item.HasCompanyName ? item.CompanyName : null,
            item.HasEmail1Address ? item.Email1Address : null,
        })
            if (!string.IsNullOrWhiteSpace(candidate)) return candidate;

        return null;
    }

    public static void Set(string? value, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(value)) assign(value);
    }

    private static List<string> Clean(IEnumerable<string> values) =>
        [.. values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase)];
}
