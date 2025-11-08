//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;

//public class ORMContext
//{
//    private readonly string _connectionString;

//    public ORMContext(string connectionString)
//    {
//        _connectionString = connectionString;
//    }

//    public T Create<T>(T entity, string tableName) where T : class
//    {
//        // Пример реализации метода Create
//        // Параметризованный SQL-запрос для вставки данных
//        throw new NotImplementedException();
//    }

//    public T ReadById<T>(int id) where T : class
//    {
//        using (SqlConnection connection = new SqlConnection(_connectionString))
//        {
//            connection.Open();
//            string sql = $"SELECT * FROM {tableName} WHERE Id = @id";
//            SqlCommand command = new SqlCommand(sql, connection);
//            command.Parameters.AddWithValue("@id", id);

//            using (SqlDataReader reader = command.ExecuteReader())
//            {
//                if (reader.Read())
//                {
//                    // Маппинг данных из таблицы в объект
//                    throw new NotImplementedException();
//                }
//            }
//        }
//        return null;
//    }

//    public T ReadByAll<T>(int id) where T : class
//    {
//        using (SqlConnection connection = new SqlConnection(_connectionString))
//        {
//            connection.Open();
//            string sql = $"SELECT * FROM {tableName} WHERE Id = @id";
//            SqlCommand command = new SqlCommand(sql, connection);
//            command.Parameters.AddWithValue("@id", id);

//            using (SqlDataReader reader = command.ExecuteReader())
//            {
//                if (reader.Read())
//                {
//                    // Маппинг данных из таблицы в объект
//                    throw new NotImplementedException();
//                }
//            }
//        }
//        return null;
//    }

//    public void Update<T>(int id, T entity, string tableName)
//    {
//        using (SqlConnection connection = new SqlConnection(_connectionString))
//        {
//            connection.Open();
//            string sql = $"UPDATE {tableName} SET Column1 = @value1 WHERE Id = @id";
//            SqlCommand command = new SqlCommand(sql, connection);
//            command.Parameters.AddWithValue("@id", id);
//            command.Parameters.AddWithValue("@value1", "значение");

//            command.ExecuteNonQuery();
//        }
//    }

//    public void Delete(int id, string tableName)
//    {
//        using (SqlConnection connection = new SqlConnection(_connectionString))
//        {
//            connection.Open();
//            string sql = $"DELETE FROM {tableName} WHERE Id = @id";
//            SqlCommand command = new SqlCommand(sql, connection);
//            command.Parameters.AddWithValue("@id", id);

//            command.ExecuteNonQuery();
//        }
//    }
//}