namespace SecurePay.Api.Repositories;

using Dapper;
using Oracle.ManagedDataAccess.Client;
using SecurePay.Api.Interfaces;
using SecurePay.Api.Models;
using SecurePay.Api.Models.DTOs;
using System.Data;

public class AccountRepository : IAccountRepository
{
    private readonly string _connectionString;

    public AccountRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleDbConnection")
            ?? throw new ArgumentNullException("OracleDbConnection not found in configuration");
    }

    private OracleConnection CreateConnection() => new OracleConnection(_connectionString);

    public async Task<Account?> GetByIdAsync(int accountId)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM ACCOUNTS WHERE AccountId = :accountId";
        return await connection.QueryFirstOrDefaultAsync<Account>(sql, new {accountId});
        // var parameters = new DynamicParameters();
        // parameters.Add("p_account_id", accountId, DbType.Int32, ParameterDirection.Input);
        // parameters.Add("p_cursor", dbType: DbType.Object, direction: ParameterDirection.Output);

        // var result = await connection.QueryFirstOrDefaultAsync<Account>(
        //     "SP_GET_ACCOUNT_BY_ID",
        //     parameters,
        //     commandType: CommandType.StoredProcedure
        // );
        //return result;
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Accounts WHERE AccountNumber = :accountNumber";
        return await connection.QueryFirstOrDefaultAsync<Account>(sql, new { accountNumber });
        // var parameters = new DynamicParameters();
        // parameters.Add("p_account_number", accountNumber, DbType.String, ParameterDirection.Input);
        // parameters.Add("p_cursor", dbType: DbType.Object, direction: ParameterDirection.Output);

        // var result = await connection.QueryFirstOrDefaultAsync<Account>(
        //     "SP_GET_ACCOUNT_BY_NUMBER",
        //     parameters,
        //     commandType: CommandType.StoredProcedure
        // );
    }

    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        using var connection = CreateConnection();
        const string sql = "SELECT * FROM Accounts";
        return await connection.QueryAsync<Account>(sql);
        // var parameters = new DynamicParameters();
        // parameters.Add("p_cursor", dbType: DbType.Object, direction: ParameterDirection.Output);

        // var result = await connection.QueryAsync<Account>(
        //     "SP_GET_ALL_ACCOUNTS",
        //     parameters,
        //     commandType: CommandType.StoredProcedure
        // );

        // return result;
    }

    public async Task<int> CreateAsync(Account account)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("p_customer_name", account.CustomerName, DbType.String, ParameterDirection.Input);
        parameters.Add("p_account_number", account.AccountNumber, DbType.String, ParameterDirection.Input);
        parameters.Add("p_balance", account.Balance, DbType.Decimal, ParameterDirection.Input);
        parameters.Add("p_currency", account.Currency, DbType.String, ParameterDirection.Input);
        parameters.Add("p_account_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "SP_CREATE_ACCOUNT",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("p_account_id");
    }

    public async Task<bool> UpdateAsync(Account account)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("p_account_id", account.AccountId, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_customer_name", account.CustomerName, DbType.String, ParameterDirection.Input);
        parameters.Add("p_account_number", account.AccountNumber, DbType.String, ParameterDirection.Input);
        parameters.Add("p_balance", account.Balance, DbType.Decimal, ParameterDirection.Input);
        parameters.Add("p_currency", account.Currency, DbType.String, ParameterDirection.Input);
        parameters.Add("p_rows_affected", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "SP_UPDATE_ACCOUNT",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("p_rows_affected") > 0;
    }

    public async Task<bool> DeleteAsync(int accountId)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("p_account_id", accountId, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_rows_affected", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "SP_DELETE_ACCOUNT",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("p_rows_affected") > 0;
    }

    public async Task<bool> UpdateBalanceAsync(int accountId, decimal newBalance)
    {
        using var connection = CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("p_account_id", accountId, DbType.Int32, ParameterDirection.Input);
        parameters.Add("p_new_balance", newBalance, DbType.Decimal, ParameterDirection.Input);
        parameters.Add("p_rows_affected", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "SP_UPDATE_BALANCE",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("p_rows_affected") > 0;
    }

    public async Task<string> TransferMoneyAsync(TransferRequestDto transferRequest)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SP_TRANSFER_MONEY";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new OracleParameter("p_sender_id", OracleDbType.Int32, ParameterDirection.Input) { Value = transferRequest.SenderId });
        command.Parameters.Add(new OracleParameter("p_receiver_id", OracleDbType.Int32, ParameterDirection.Input) { Value = transferRequest.ReceiverId });
        command.Parameters.Add(new OracleParameter("p_amount", OracleDbType.Decimal, ParameterDirection.Input) { Value = transferRequest.Amount });
        command.Parameters.Add(new OracleParameter("p_status", OracleDbType.Varchar2, 100, null, ParameterDirection.Output));

        await command.ExecuteNonQueryAsync();

        return command.Parameters["p_status"].Value?.ToString() ?? "HATA";
    }
}