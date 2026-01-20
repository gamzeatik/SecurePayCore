namespace SecurePay.Api.Models;

public class Account
{
    public int AccountId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; }
}