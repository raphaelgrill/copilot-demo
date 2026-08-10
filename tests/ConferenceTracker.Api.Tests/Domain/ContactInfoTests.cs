using ConferenceTracker.Api.Domain;

namespace ConferenceTracker.Api.Tests.Domain;

public class ContactInfoTests
{
    [Fact]
    public void Two_contacts_with_the_same_values_are_equal()
    {
        var first = new ContactInfo("mara@example.com", "@maralq");
        var second = new ContactInfo("mara@example.com", "@maralq");

        Assert.Equal(first, second);
    }
}
