using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    internal class Users
    {
        [Table("users")]
        class User
        {
            [PrimaryKey]
            public int Id { get; set; }
            [Column]
            public string Name { get; set; }
            [Column]
            public int Age { get; set; }
        }
    }
}
