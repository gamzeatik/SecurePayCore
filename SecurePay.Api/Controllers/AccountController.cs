using Microsoft.AspNetCore.Mvc;
using SecurePay.Api.Interfaces;
using SecurePay.Api.Models;
using SecurePay.Api.Models.DTOs;

namespace SecurePay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;

    public AccountController(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Account>>>> GetAll()
    {
        var accounts = await _accountRepository.GetAllAsync();
        return Ok(new ApiResponse<IEnumerable<Account>>
        {
            Success = true,
            Message = "Hesaplar başarıyla getirildi",
            Data = accounts
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Account>>> GetById(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound(new ApiResponse<Account>
            {
                Success = false,
                Message = "Hesap bulunamadı"
            });
        }

        return Ok(new ApiResponse<Account>
        {
            Success = true,
            Message = "Hesap başarıyla getirildi",
            Data = account
        });
    }

    [HttpGet("by-number/{accountNumber}")]
    public async Task<ActionResult<ApiResponse<Account>>> GetByAccountNumber(string accountNumber)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
        if (account == null)
        {
            return NotFound(new ApiResponse<Account>
            {
                Success = false,
                Message = "Hesap bulunamadı"
            });
        }

        return Ok(new ApiResponse<Account>
        {
            Success = true,
            Message = "Hesap başarıyla getirildi",
            Data = account
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] Account account)
    {
        var accountId = await _accountRepository.CreateAsync(account);
        return CreatedAtAction(nameof(GetById), new { id = accountId }, new ApiResponse<int>
        {
            Success = true,
            Message = "Hesap başarıyla oluşturuldu",
            Data = accountId
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] Account account)
    {
        account.AccountId = id;
        var result = await _accountRepository.UpdateAsync(account);
        if (!result)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Hesap güncellenemedi"
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Hesap başarıyla güncellendi",
            Data = true
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _accountRepository.DeleteAsync(id);
        if (!result)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Hesap silinemedi"
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Hesap başarıyla silindi",
            Data = true
        });
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<string>>> Transfer([FromBody] TransferRequestDto transferRequest)
    {
        var result = await _accountRepository.TransferMoneyAsync(transferRequest);
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Transfer işlemi tamamlandı",
            Data = result
        });
    }
}
