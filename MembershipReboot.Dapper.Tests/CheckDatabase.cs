using Dapper;
using System;
using System.Data.SqlClient;

namespace MembershipReboot.Dapper.Tests {
    public class CheckDatabase : IDisposable {
        public CheckDatabase() {
            using (var connection = new SqlConnection(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True")) {
                connection.Open();
                connection.Execute(SqlQueries.CheckDb);
            }
        }

        public void Dispose() {
            using (var connection = new SqlConnection(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=MembershipReboot;Integrated Security=True")) {
                connection.Open();
                Helpers.ResetDatabase(connection);
            }
        }
    }
}
