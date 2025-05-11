using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private string connectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;";
public async Task<int> addProduct(Warehouse warehouse)
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var transaction = connection.BeginTransaction();

    try
    {
        var checkWarehouse = new SqlCommand("SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection, transaction);
        checkWarehouse.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
        if ((int)(await checkWarehouse.ExecuteScalarAsync()) == 0)
            throw new Exception("Warehouse not found");
        
        var checkProduct = new SqlCommand("SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct", connection, transaction);
        checkProduct.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
        if ((int)(await checkProduct.ExecuteScalarAsync()) == 0)
            throw new Exception("Product not found");
        
        var checkOrder = new SqlCommand(
            @"SELECT TOP 1 IdOrder FROM [Order]
              WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL",
            connection, transaction);
        checkOrder.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
        checkOrder.Parameters.AddWithValue("@Amount", warehouse.Amount);
        checkOrder.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedAt);

        var idOrderObj = await checkOrder.ExecuteScalarAsync();
        if (idOrderObj == null)
            throw new Exception("No matching order found");
        int idOrder = (int)idOrderObj;
        
        var checkIfFulfilled = new SqlCommand("SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @IdOrder", connection, transaction);
        checkIfFulfilled.Parameters.AddWithValue("@IdOrder", idOrder);
        if ((int)(await checkIfFulfilled.ExecuteScalarAsync()) > 0)
            throw new Exception("Order already fulfilled");
        
        var updateOrder = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", connection, transaction);
        updateOrder.Parameters.AddWithValue("@IdOrder", idOrder);
        await updateOrder.ExecuteNonQueryAsync();
        
        var getPrice = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", connection, transaction);
        getPrice.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
        decimal price = (decimal)(await getPrice.ExecuteScalarAsync());
        decimal totalPrice = price * warehouse.Amount;
        
        var insert = new SqlCommand(@"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            OUTPUT INSERTED.IdProductWarehouse
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE())
        ", connection, transaction);

        insert.Parameters.AddWithValue("@IdWarehouse", warehouse.IdWarehouse);
        insert.Parameters.AddWithValue("@IdProduct", warehouse.IdProduct);
        insert.Parameters.AddWithValue("@IdOrder", idOrder);
        insert.Parameters.AddWithValue("@Amount", warehouse.Amount);
        insert.Parameters.AddWithValue("@Price", totalPrice);

        var insertedId = await insert.ExecuteScalarAsync();

        await transaction.CommitAsync();

        return (int)insertedId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
}