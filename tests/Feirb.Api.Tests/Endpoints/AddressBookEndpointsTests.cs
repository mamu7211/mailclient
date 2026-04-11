using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Feirb.Shared.AddressBook;
using Feirb.Shared.Auth;
using Feirb.Shared.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Feirb.Api.Tests.Endpoints;

public class AddressBookEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AddressBookEndpointsTests()
    {
        _factory = TestWebApplicationFactory.Create($"TestDb-{Guid.NewGuid()}");
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Contacts CRUD ---

    [Fact]
    public async Task ListContacts_Empty_ReturnsEmptyPageAsync()
    {
        await LoginAsync();
        var response = await _client.GetAsync("/api/address-book/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ContactListItem>>();
        page.Should().NotBeNull();
        page!.Items.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task CreateContact_WithEmails_ReturnsCreatedAsync()
    {
        await LoginAsync();
        var request = new CreateContactRequest(
            "John Doe", "VIP client", true, ["john@example.com", "john.doe@work.com"]);

        var response = await _client.PostAsJsonAsync("/api/address-book/contacts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var contact = await response.Content.ReadFromJsonAsync<ContactResponse>();
        contact.Should().NotBeNull();
        contact!.DisplayName.Should().Be("John Doe");
        contact.IsImportant.Should().BeTrue();
        contact.Addresses.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateContact_ChangesDisplayNameAsync()
    {
        await LoginAsync();
        var created = await CreateContactAsync("Original Name");

        var update = new UpdateContactRequest("Updated Name", "Note", false);
        var response = await _client.PutAsJsonAsync($"/api/address-book/contacts/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContactResponse>();
        updated!.DisplayName.Should().Be("Updated Name");
        updated.Notes.Should().Be("Note");
    }

    [Fact]
    public async Task DeleteContact_OrphansLinkedAddressesAsync()
    {
        await LoginAsync();
        var created = await CreateContactAsync("To Delete", emails: ["orphan@example.com"]);

        var deleteResponse = await _client.DeleteAsync($"/api/address-book/contacts/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Address should still exist but unlinked
        var addressId = created.Addresses[0].Id;
        var addressResponse = await _client.GetAsync($"/api/address-book/addresses/{addressId}");
        addressResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var address = await addressResponse.Content.ReadFromJsonAsync<AddressResponse>();
        address!.ContactId.Should().BeNull();
    }

    [Fact]
    public async Task GetContact_NotFound_Returns404Async()
    {
        await LoginAsync();
        var response = await _client.GetAsync($"/api/address-book/contacts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Addresses CRUD ---

    [Fact]
    public async Task DeleteAddress_WhenLinked_Returns409Async()
    {
        await LoginAsync();
        var created = await CreateContactAsync("Linked", emails: ["linked@example.com"]);
        var addressId = created.Addresses[0].Id;

        var response = await _client.DeleteAsync($"/api/address-book/addresses/{addressId}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAddress_WhenOrphan_ReturnsOkAsync()
    {
        await LoginAsync();
        var created = await CreateContactAsync("To Orphan", emails: ["orphan2@example.com"]);
        var addressId = created.Addresses[0].Id;

        // Delete contact first — orphans the address
        await _client.DeleteAsync($"/api/address-book/contacts/{created.Id}");

        // Now delete the address
        var response = await _client.DeleteAsync($"/api/address-book/addresses/{addressId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PromoteAddress_CreatesContactAndLinksAsync()
    {
        await LoginAsync();
        // Create an orphan address by creating a contact and then deleting it
        var temp = await CreateContactAsync("Temp", emails: ["to-promote@example.com"]);
        var addressId = temp.Addresses[0].Id;
        await _client.DeleteAsync($"/api/address-book/contacts/{temp.Id}");

        var promote = new PromoteAddressRequest("Promoted Contact", null, false);
        var response = await _client.PostAsJsonAsync($"/api/address-book/addresses/{addressId}/promote", promote);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var newContact = await response.Content.ReadFromJsonAsync<ContactResponse>();
        newContact!.DisplayName.Should().Be("Promoted Contact");
        newContact.Addresses.Should().HaveCount(1);
        newContact.Addresses[0].Id.Should().Be(addressId);
    }

    [Fact]
    public async Task LinkAddress_AttachesToContactAsync()
    {
        await LoginAsync();
        var contactA = await CreateContactAsync("Contact A", emails: ["a@example.com"]);
        var contactB = await CreateContactAsync("Contact B");
        var addressId = contactA.Addresses[0].Id;

        var request = new LinkAddressRequest(contactB.Id);
        var response = await _client.PostAsJsonAsync($"/api/address-book/addresses/{addressId}/link", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var linked = await response.Content.ReadFromJsonAsync<AddressResponse>();
        linked!.ContactId.Should().Be(contactB.Id);
    }

    [Fact]
    public async Task UnlinkAddress_RemovesContactLinkAsync()
    {
        await LoginAsync();
        var contact = await CreateContactAsync("To Unlink", emails: ["unlink@example.com"]);
        var addressId = contact.Addresses[0].Id;

        var response = await _client.PostAsync($"/api/address-book/addresses/{addressId}/unlink", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var unlinked = await response.Content.ReadFromJsonAsync<AddressResponse>();
        unlinked!.ContactId.Should().BeNull();
    }

    // --- Autocomplete ---

    [Fact]
    public async Task SearchRecipients_Empty_ReturnsEmptyListAsync()
    {
        await LoginAsync();
        var response = await _client.GetAsync("/api/mail/recipients/search?q=nothing");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecipientSuggestion>>();
        suggestions.Should().NotBeNull();
        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchRecipients_ImportantRanksAboveOthersAsync()
    {
        await LoginAsync();
        // Create: one important contact, one normal contact
        await CreateContactAsync("Alice Normal", emails: ["alice@example.com"], isImportant: false);
        await CreateContactAsync("Alice Important", emails: ["alice2@example.com"], isImportant: true);

        var response = await _client.GetAsync("/api/mail/recipients/search?q=alice");
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecipientSuggestion>>();

        suggestions.Should().NotBeNull();
        suggestions.Should().HaveCountGreaterThanOrEqualTo(2);
        suggestions![0].Status.Should().Be(RecipientStatus.Important);
    }

    [Fact]
    public async Task SearchRecipients_BlockedRanksLastAsync()
    {
        await LoginAsync();
        var contact = await CreateContactAsync("Bob", emails: ["bob@example.com"]);
        await CreateContactAsync("Zed", emails: ["zed@example.com"]);

        // Block bob
        var blocked = new UpdateAddressRequest("Bob", true);
        await _client.PutAsJsonAsync($"/api/address-book/addresses/{contact.Addresses[0].Id}", blocked);

        var response = await _client.GetAsync("/api/mail/recipients/search?q=e");
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecipientSuggestion>>();

        suggestions.Should().NotBeNull();
        suggestions!.Last().Status.Should().Be(RecipientStatus.Blocked);
    }

    [Fact]
    public async Task Contacts_OnlyReturnsCurrentUsersContactsAsync()
    {
        var adminTokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        await CreateContactAsync("Admin contact");

        // Register and login as another user
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("otheruser", "other@example.com", "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("otheruser", "Password123!"));
        var otherTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherTokens!.AccessToken);

        var response = await _client.GetAsync("/api/address-book/contacts");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ContactListItem>>();
        page!.Items.Should().BeEmpty();
    }

    // --- Helpers ---

    private async Task LoginAsync()
    {
        var tokens = await SetupAndLoginAsAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private async Task<ContactResponse> CreateContactAsync(
        string displayName,
        IReadOnlyList<string>? emails = null,
        bool isImportant = false)
    {
        var request = new CreateContactRequest(displayName, null, isImportant, emails);
        var response = await _client.PostAsJsonAsync("/api/address-book/contacts", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContactResponse>())!;
    }

    private async Task<TokenResponse> SetupAndLoginAsAdminAsync()
    {
        var setupRequest = new CompleteSetupRequest(
            "admin", "admin@example.com", "AdminPassword123!",
            "smtp.example.com", 587, "smtp@example.com", "smtppass", true, true);
        await _client.PostAsJsonAsync("/api/setup/complete", setupRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "AdminPassword123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokens.Should().NotBeNull();
        return tokens!;
    }
}
