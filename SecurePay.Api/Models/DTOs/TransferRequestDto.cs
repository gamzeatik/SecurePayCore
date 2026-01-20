namespace SecurePay.Api.Models.DTOs;

public class TransferRequestDto
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public decimal Amount { get; set; }
    // API güvenliği için isteğe bağlı olarak bir açıklama veya PIN eklenebilir.
}