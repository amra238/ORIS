//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Data.SqlClient;
//using System.Data;

//namespace MiniHttpServer.Endpoints
//{
//    internal class UserEndpoint 
//    {
//        public void GetUsers(string connectionString)
//        {
//            string sqlExpression = "SELECT * FROM Users";
//            using (SqlConnection connection = new SqlConnection(connectionString))
//            {
//                connection.Open();
//                var command = new SqlCommand(sqlExpression, connection);
//                SqlDataReader reader = command.ExecuteReader();

//                if (reader.HasRows) 
//                {                    
//                    Console.WriteLine("{0}\t{1}\t{2}", reader.GetName(0), reader.GetName(1), reader.GetName(2));

//                    while (reader.Read()) // построчно считываем данные
//                    {
//                        object id = reader.GetValue(0);
//                        object name = reader.GetValue(1);
//                        object age = reader.GetValue(2);

//                        Console.WriteLine("{0} \t{1} \t{2}", id, name, age);
//                    }
//                }

//                reader.Close();
//            }

//            Console.Read();
//        }
//    }
//}
