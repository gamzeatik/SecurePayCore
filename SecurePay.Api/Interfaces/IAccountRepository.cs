using SecurePay.Api.Models;
using SecurePay.Api.Models.DTOs;

namespace SecurePay.Api.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(int accountId);
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<IEnumerable<Account>> GetAllAsync();
    Task<int> CreateAsync(Account accountId);
    Task<bool> UpdateAsync(Account account);
    Task<bool> DeleteAsync(int accountId);
    Task<bool> UpdateBalanceAsync(int accountId, decimal newBalance);
    Task<string> TransferMoneyAsync(TransferRequestDto transferRequest);
}