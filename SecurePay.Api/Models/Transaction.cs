namespace SecurePay.Api.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public int SenderAccountId { get; set; }
    public int ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = "Transfer";
    public string Status { get; set; } = "Pending";
    public string? ReferenceNo { get; set; }
    public DateTime TransactionDate { get; set; }
}