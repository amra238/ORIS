using MyORMLibrary;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

public class ORMContext : IEntityDao
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
            string sqlType = GetPostgreSqlType(property.PropertyType);
            string columnDefinition = $"{property.Name} {sqlType}";

            if (property.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {                
                columnDefinition = $"{property.Name} INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY";
            }
            else if (property.PropertyType == typeof(string))
            {
                columnDefinition += " NULL";
            }

            columnList.Add(columnDefinition);
        }

        string columnsSql = string.Join(",\n    ", columnList);

        string sql = $@"CREATE TABLE IF NOT EXISTS {tableName} (
        {columnsSql}
    );";

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(sql, connection))
            {
                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Таблица {tableName} создана или уже существует.");
                }
                catch (NpgsqlException ex)
                {
                    Console.WriteLine($"Ошибка при создании таблицы {tableName}: {ex.Message}");
                    throw;
                }
            }
        }
    }

    public T Create<T>(T entity, string tableName) where T : class, new()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanWrite && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                                 .ToArray();

        var columnNames = string.Join(", ", properties.Select(p => p.Name));
        var parameterNames = string.Join(", ", properties.Select(p => "@" + p.Name));
        
        string sql = $@"
    INSERT INTO {tableName} ({columnNames}) 
    VALUES ({parameterNames})
    RETURNING *";

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(sql, connection))
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
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"SELECT * FROM {tableName} WHERE id = @id";

            using (var command = new NpgsqlCommand(sql, connection))
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

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"SELECT * FROM {tableName}";

            using (var command = new NpgsqlCommand(sql, connection))
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
                         .Where(p => p.CanWrite && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                         .ToArray();

        if (properties.Length == 0)
            return;

        var setClauses = properties.Select(p => $"{p.Name} = @{p.Name}");
        string setClause = string.Join(", ", setClauses);
        string sql = $"UPDATE {tableName} SET {setClause} WHERE id = @id";

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();

            using (var command = new NpgsqlCommand(sql, connection))
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
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            string sql = $"DELETE FROM {tableName} WHERE id = @id";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }
    }

    private string GetPostgreSqlType(Type propertyType)
    {
        if (propertyType == typeof(int) || propertyType == typeof(int?))
            return "INTEGER";
        if (propertyType == typeof(string))
            return "VARCHAR(255)";
        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            return "BOOLEAN";
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            return "TIMESTAMP";
        if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
            return "NUMERIC(18,2)";
        if (propertyType == typeof(double) || propertyType == typeof(double?))
            return "DOUBLE PRECISION";
        if (propertyType == typeof(float) || propertyType == typeof(float?))
            return "REAL";
        if (propertyType == typeof(long) || propertyType == typeof(long?))
            return "BIGINT";
        if (propertyType == typeof(short) || propertyType == typeof(short?))
            return "SMALLINT";
        if (propertyType == typeof(byte[]))
            return "BYTEA";
        if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
            return "UUID";

        return "TEXT";
    }

    private string BuildSqlQuery<T>(Expression<Func<T, bool>> predicate, bool singleResult)
    {
        var tableName = typeof(T).Name + "s"; // Или просто "tours"
        var whereClause = ParseExpression(predicate.Body);

        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";

        if (singleResult)
            sql += " LIMIT 1";

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
        if (member.Member is PropertyInfo propertyInfo)
        {            
            if (propertyInfo.GetMethod?.IsStatic == true)
            {            
                var value = propertyInfo.GetValue(null); // null для статического свойства
                return FormatConstant(value);
            }
           
            return propertyInfo.Name;
        }
        else if (member.Member is FieldInfo fieldInfo)
        {
            if (member.Expression is ConstantExpression constExpr)
            {
                var obj = constExpr.Value;
                var fieldValue = fieldInfo.GetValue(obj);
                return FormatConstant(fieldValue);
            }

            throw new NotSupportedException($"Неподдерживаемое выражение для поля: {fieldInfo.Name}");
        }

        throw new NotSupportedException($"Неподдерживаемый тип члена: {member.Member.GetType().Name}");
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
       
        if (value is DateTime dt)
        {
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        }

        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            bool b => b ? "TRUE" : "FALSE",
            _ => value.ToString()
        };
    }

    private string ParseConstantExpression(ConstantExpression constant)
    {
        return FormatConstant(constant.Value);
    }

    private T ExecuteQuerySingle<T>(string query) where T : class, new()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(query, connection))
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

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(query, connection))
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