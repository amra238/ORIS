using Microsoft.Data.SqlClient;
using Microsoft.SqlServer;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClassLibrary1
{
    public class Class1
    {
        public static string Create<T>() where T : class
        {
            var type = typeof(T);

            var table = type.GetCustomAttribute<TableAttribute>();
            var keyTable = type.GetCustomAttribute<PrimaryKeyAttribute>();
            var properties = type.GetCustomAttributes<ColumnAttribute>();

            string sqlCreateCommand = $"CREATE TABLE {table.Name} ( {keyTable.PrimaryId} SERIAL PRIMARY KEY";

            foreach (var property in properties)
            {
                var column = property.GetType().GetCustomAttribute<ColumnAttribute>().Name;

                if (column is int)
                    sqlCreateCommand += $", {column} INTEGER NOT NULL";

                if (column is string)
                    sqlCreateCommand += $", {column} VARCHAR(100) NOT NULL";
            }
            sqlCreateCommand += ");";

            CreateJSONResponse(table.Name, "created", sqlCreateCommand, "", "create");

            return sqlCreateCommand;
        }

        public static void Apply(string sql)
        {
            string connectionString = @"Data Source=localhost;Initial Catalog=usersdb;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string tableName = ExtractTableNameAdvanced(sql);

                    if (!string.IsNullOrEmpty(tableName) && TableExists(connection, tableName))
                        DropTable(connection, tableName);    
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        int result = command.ExecuteNonQuery();
                    }

                    CreateJSONResponse(tableName, "applied", sql, "", "apply");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            try
            {
                string checkSql = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @TableName";

                using (SqlCommand command = new SqlCommand(checkSql, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void DropTable(SqlConnection connection, string tableName)
        {
            try
            {
                string dropSql = $"DROP TABLE {tableName}";
                using (SqlCommand command = new SqlCommand(dropSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string ExtractTableNameAdvanced(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return null;

            var match = Regex.Match(sql,
                @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:\[?(\w+)\]?|`?(\w+)`?)",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            }

            return null;
        }

        private static void CreateJSONResponse(string tableName, string status, string sql, string previousSQl, string typeOfResponse)
        {
            var model = new JSONResponseCreate();
            if (typeOfResponse == "create")
            {
                model = new JSONResponseCreate()
                {
                    mirgation = $"Create{tableName}Table",
                    status = "created",
                    up_sql = sql,
                    down_sql = previousSQl,
                };
            }
            else if (typeOfResponse == "apply")
            {

            }
            else
                throw new ArgumentException("че-то не так");

            
            string JSONResponse = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            File.WriteAllText("C:\\Users\\amirg\\source\\repos\\cm\\ClassLibrary1\\response.json", JSONResponse);
            Console.WriteLine("JSON was created");
        }

    }
}
