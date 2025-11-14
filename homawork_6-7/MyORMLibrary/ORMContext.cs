using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;

public class ORMContext
{
    private readonly string _connectionString;

    public ORMContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        var sqlQuery = BuildSqlQuery(predicate, singleResult: true);
        return ExecuteQuerySingle<T>(sqlQuery);
    }

    public IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        var sqlQuery = BuildSqlQuery(predicate, singleResult: false);
        return ExecuteQueryMultiple<T>(sqlQuery);
    }

    public void Create<T>(string tableName) where T : class
    {
        var properties = typeof(T).GetProperties();
        var columnList = new List<string>();

        foreach (var property in properties)
        {
            string sqlType = GetSqlType(property.PropertyType);
            string columnDefinition = $"{property.Name} {sqlType}";

            if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                columnDefinition += " PRIMARY KEY IDENTITY(1,1)";
            }
            else if (property.PropertyType == typeof(string))
            {
                columnDefinition += " NULL";
            }

            columnList.Add(columnDefinition);
        }

        string columnsSql = string.Join(",\n    ", columnList);
        string sql = $@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
        BEGIN
            CREATE TABLE {tableName} (
                {columnsSql}
            )
        END";

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public T Create<T>(T entity, string tableName) where T : class, new()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanWrite && !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

        var columnNames = string.Join(", ", properties.Select(p => p.Name));
        var parameterNames = string.Join(", ", properties.Select(p => "@" + p.Name));

        string sql = $@"
        INSERT INTO {tableName} ({columnNames}) 
        OUTPUT INSERTED.* 
        VALUES ({parameterNames})";

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(sql, connection))
            {
                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    command.Parameters.AddWithValue("@" + property.Name, value);
                }
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {                        
                        return MapToObject<T>(reader);
                    }
                }
            }
        }

        return entity;
    }

    public T ReadById<T>(int id, string tableName) where T : class, new()
    {     
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"SELECT * FROM {tableName} WHERE Id = @id";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {                        
                        return MapToObject<T>(reader);
                    }
                }
            }
        }
        return null; 
    }

    public List<T> ReadByAll<T>(string tableName) where T : class, new()
    {
        var results = new List<T>();

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"SELECT * FROM {tableName}";

            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read()) 
                {
                    var entity = MapToObject<T>(reader);
                    results.Add(entity);
                }
            }
        }
        return results;
    }

    public void Update<T>(int id, T entity, string tableName) where T : class
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanWrite && !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                         .ToArray();
       
        if (properties.Length == 0)
            return;

        var setClauses = properties.Select(p => $"{p.Name} = @{p.Name}");
        string setClause = string.Join(", ", setClauses);
        string sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @id";

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    command.Parameters.AddWithValue("@" + property.Name, value);
                }

                int rowsAffected = command.ExecuteNonQuery();
            }
        }
    }

    public void Delete(int id, string tableName)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"DELETE FROM {tableName} WHERE Id = @id";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }                      
        }
    }

    private string GetSqlType(Type propertyType)
    {
        if (propertyType == typeof(int) || propertyType == typeof(int?))
            return "INT";
        if (propertyType == typeof(string))
            return "NVARCHAR(255)";
        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            return "BIT";
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            return "DATETIME";
        if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
            return "DECIMAL(18,2)";
        if (propertyType == typeof(double) || propertyType == typeof(double?))
            return "FLOAT";
        if (propertyType == typeof(float) || propertyType == typeof(float?))
            return "REAL";
        if (propertyType == typeof(long) || propertyType == typeof(long?))
            return "BIGINT";
        if (propertyType == typeof(short) || propertyType == typeof(short?))
            return "SMALLINT";
        if (propertyType == typeof(byte[]))
            return "VARBINARY(MAX)";
        if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
            return "UNIQUEIDENTIFIER";

        return "NVARCHAR(MAX)";
    }

    private string BuildSqlQuery<T>(Expression<Func<T, bool>> predicate, bool singleResult)
    {
        var tableName = typeof(T).Name + "s"; 
        var whereClause = ParseExpression(predicate.Body);       

        var sql =  $"SELECT * FROM {tableName} WHERE {whereClause}";

        if (singleResult == true)
            sql += " Limit 1";

        return sql;
    }

    private string ParseExpression(Expression expression)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
                return ParseBinaryExpression((BinaryExpression)expression);

            case ExpressionType.AndAlso:
            case ExpressionType.OrElse:
                return ParseLogicalExpression((BinaryExpression)expression);

            case ExpressionType.MemberAccess:
                return ParseMemberExpression((MemberExpression)expression);

            case ExpressionType.Constant:
                return ParseConstantExpression((ConstantExpression)expression);
            default:
                throw new NotSupportedException($"Неподдерживаемый тип выражения: {expression.NodeType}");
        }
    }

    private string ParseBinaryExpression(BinaryExpression binary)
    {
        var left = ParseExpression(binary.Left);
        var right = ParseExpression(binary.Right);
        var op = GetSqlOperator(binary.NodeType);

        return $"{left} {op} {right}";
    }

    private string ParseLogicalExpression(BinaryExpression binary)
    {
        var left = ParseExpression(binary.Left);
        var right = ParseExpression(binary.Right);
        var op = GetSqlOperator(binary.NodeType);

        return $"({left} {op} {right})";
    }

    private string ParseMemberExpression(MemberExpression member)
    {
        return member.Member.Name; 
    }

    private string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Неподдерживаемый оператор: {nodeType}")
        };
    }

    private string FormatConstant(object value)
    {
        if (value == null) return "NULL";

        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'", bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'", _ => value.ToString()
        };
    }

    private string ParseConstantExpression(ConstantExpression constant)
    {
        return FormatConstant(constant.Value);
    }

    private T ExecuteQuerySingle<T>(string query) where T : class, new()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return MapToObject<T>(reader);
                }
            }
        }
        return null;
    }

    private IEnumerable<T> ExecuteQueryMultiple<T>(string query) where T : class, new()
    {
        var results = new List<T>();

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(MapToObject<T>(reader));
                }
            }
        }

        return results;
    }

    private T MapToObject<T>(IDataReader reader) where T : class, new()
    {
        var obj = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var property = properties.FirstOrDefault(p =>
                p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (property != null && property.CanWrite && !reader.IsDBNull(i))
            {
                var value = reader.GetValue(i);

                if (value.GetType() != property.PropertyType)
                {
                    value = Convert.ChangeType(value, property.PropertyType);
                }

                property.SetValue(obj, value);
            }
        }

        return obj;
    }
}