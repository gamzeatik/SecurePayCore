using System.Net;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using SecurePay.Api.Models;

namespace SecurePay.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Bir hata oluştu: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = new ApiResponse<object>
        {
            Success = false,
            Data = null
        };

        switch (exception)
        {
            case OracleException oracleEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = ParseOracleException(oracleEx);
                break;

            case ArgumentNullException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = "Gerekli parametreler eksik";
                break;

            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = exception.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse.Message = "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(apiResponse, jsonOptions));
    }

    private string ParseOracleException(OracleException ex)
    {
        // Oracle hata kodlarına göre kullanıcı dostu mesajlar
        return ex.Number switch
        {
            1 => "Bu kayıt zaten mevcut",
            1400 => "Zorunlu alan boş bırakılamaz",
            1401 => "Girilen değer çok uzun",
            2291 => "İlişkili kayıt bulunamadı",
            2292 => "Bu kayıt başka kayıtlar tarafından kullanılıyor",
            12541 => "Veritabanı bağlantısı kurulamadı",
            12543 => "Veritabanı sunucusuna erişilemiyor",
            20001 => "Yetersiz bakiye", // Custom error code for insufficient balance
            20002 => "Hesap bulunamadı", // Custom error code for account not found
            _ => ParseOracleMessage(ex.Message)
        };
    }

    private string ParseOracleMessage(string message)
    {
        // ORA-20xxx custom error mesajlarını parse et
        if (message.Contains("ORA-20"))
        {
            var startIndex = message.IndexOf("ORA-20");
            var endIndex = message.IndexOf("\n", startIndex);
            if (endIndex == -1) endIndex = message.Length;

            var errorPart = message.Substring(startIndex, endIndex - startIndex);

            // Custom hata mesajını çıkar
            if (errorPart.Contains(":"))
            {
                return errorPart.Split(':').Last().Trim();
            }
        }

        // Genel Oracle hatası
        if (message.Contains("ORA-"))
        {
            return "Veritabanı işlemi sırasında bir hata oluştu";
        }

        return "Beklenmeyen bir hata oluştu";
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
