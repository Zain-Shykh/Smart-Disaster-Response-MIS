using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Database_Backend.Models;

/// <summary>
/// Extension methods for executing stored procedures in DatabaseProjectContext.
/// Provides transaction-safe access to ACID-compliant database operations.
/// </summary>
public partial class DatabaseProjectContext
{
    /// <summary>
    /// Generic method to execute a stored procedure and return a single result set as a list of T.
    /// </summary>
    public async Task<List<T>> ExecuteStoredProcedureAsync<T>(
        string procedureName,
        params SqlParameter[] parameters) where T : new()
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;

            if (parameters != null && parameters.Length > 0)
                command.Parameters.AddRange(parameters);

            var results = new List<T>();

            using var reader = await command.ExecuteReaderAsync();
            var properties = typeof(T).GetProperties();

            while (await reader.ReadAsync())
            {
                var item = new T();
                foreach (var prop in properties)
                {
                    var ordinal = reader.GetOrdinal(prop.Name);
                    if (ordinal >= 0 && !reader.IsDBNull(ordinal))
                    {
                        var value = reader.GetValue(ordinal);
                        prop.SetValue(item, Convert.ChangeType(value, prop.PropertyType));
                    }
                }
                results.Add(item);
            }

            return results;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing stored procedure '{procedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Execute a stored procedure that returns scalar values (single row with multiple columns).
    /// </summary>
    public async Task<Dictionary<string, object>> ExecuteStoredProcedureScalarAsync(
        string procedureName,
        params SqlParameter[] parameters)
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;

            if (parameters != null && parameters.Length > 0)
                command.Parameters.AddRange(parameters);

            using var reader = await command.ExecuteReaderAsync();
            var result = new Dictionary<string, object>();

            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    result[fieldName] = value;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing stored procedure '{procedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Execute a stored procedure that returns output parameters and status.
    /// Used for procedures that modify data (INSERT, UPDATE, DELETE).
    /// </summary>
    public async Task<(int ReturnValue, Dictionary<string, object> OutputValues)> ExecuteStoredProcedureWithOutputAsync(
        string procedureName,
        List<SqlParameter> parameters)
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;

            if (parameters != null && parameters.Count > 0)
                command.Parameters.AddRange(parameters.ToArray());

            await command.ExecuteNonQueryAsync();

            var outputValues = new Dictionary<string, object>();
            foreach (SqlParameter param in command.Parameters)
            {
                if (param.Direction == ParameterDirection.Output ||
                    param.Direction == ParameterDirection.InputOutput)
                {
                    outputValues[param.ParameterName] = param.Value ?? DBNull.Value;
                }
            }

            return (command.ExecuteNonQuery(), outputValues);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing stored procedure '{procedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Execute a stored procedure that returns no results (void).
    /// </summary>
    public async Task ExecuteStoredProcedureNonQueryAsync(
        string procedureName,
        params SqlParameter[] parameters)
    {
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;

            if (parameters != null && parameters.Length > 0)
                command.Parameters.AddRange(parameters);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing stored procedure '{procedureName}': {ex.Message}", ex);
        }
    }
}

/// <summary>
/// DTO classes for stored procedure results
/// </summary>

public class StoredProcedureResult
{
    public string ResultStatus { get; set; }
    public string ErrorMessage { get; set; }
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
}

public class AllocationApprovalResult
{
    public string ResultStatus { get; set; }
    public int AllocationID { get; set; }
    public int RequestID { get; set; }
}

public class TeamAssignmentResult
{
    public string ResultStatus { get; set; }
    public int AssignmentID { get; set; }
}

public class DeploymentApprovalResult
{
    public string ResultStatus { get; set; }
    public int AssignmentID { get; set; }
    public int RequestID { get; set; }
}

public class CompletionResult
{
    public string ResultStatus { get; set; }
    public int AssignmentID { get; set; }
    public DateTime? CompletionTime { get; set; }
}

public class PatientAdmissionResult
{
    public string ResultStatus { get; set; }
    public int AdmissionID { get; set; }
    public int AvailableBeds { get; set; }
}

public class DischargeResult
{
    public string ResultStatus { get; set; }
    public int AdmissionID { get; set; }
    public int AvailableBeds { get; set; }
}

public class InventoryCheckResult
{
    public int InventoryID { get; set; }
    public int WarehouseID { get; set; }
    public int ResourceID { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public string AlertStatus { get; set; }
}

public class DashboardStatsResult
{
    public int IncidentCount { get; set; }
    public int ResourceAllocationCount { get; set; }
    public int TeamAssignmentCount { get; set; }
    public decimal? AvgHospitalOccupancyRate { get; set; }
    public decimal? TotalConfirmedDonations { get; set; }
    public decimal? TotalExpenses { get; set; }
    public decimal? InventoryUtilizationPercent { get; set; }
}

public class RequestApprovalResult
{
    public string ResultStatus { get; set; }
    public int RequestID { get; set; }
}

public class ExpenseApprovalResult
{
    public string ResultStatus { get; set; }
    public int ExpenseID { get; set; }
    public int RequestID { get; set; }
}

public class DispatchResult
{
    public string ResultStatus { get; set; }
    public int AllocationID { get; set; }
    public DateTime? DispatchedAt { get; set; }
}

public class InventoryUpdateResult
{
    public string ResultStatus { get; set; }
    public int InventoryID { get; set; }
    public decimal NewQuantity { get; set; }
}
