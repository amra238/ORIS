using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyORMLibrary
{
    internal interface IEntityDao
    {
        T ReadById<T>(int id, string tableName) where T : class, new();
        List<T> ReadByAll<T>(string tableName) where T : class, new();
        void Create<T>(string tableName) where T : class;
        void Update<T>(int id, T entity, string tableName) where T : class;
        void Delete(int id, string tableName);
    }
}
