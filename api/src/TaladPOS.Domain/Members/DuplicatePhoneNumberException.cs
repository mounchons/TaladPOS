namespace TaladPOS.Domain.Members;

/// <summary>Thrown when a phone number is already registered to another member (FR-011).</summary>
public class DuplicatePhoneNumberException : Exception
{
    public string PhoneNumber { get; }

    public DuplicatePhoneNumberException(string phoneNumber)
        : base($"Phone number '{phoneNumber}' is already registered to another member.")
    {
        PhoneNumber = phoneNumber;
    }
}
