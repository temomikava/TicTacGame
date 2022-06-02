using Npgsql;
using NpgsqlTypes;
using System.Data;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Core.Services
{

    public class UserRegistration : IUserRegistration
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public UserRegistration(IConfiguration config)
        {
            _configuration = config;
            _connectionString = _configuration.GetConnectionString("myConnection").Trim();
        }

        public async Task Registration(Registration registration)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            try
            {
                await using var cmd = new NpgsqlCommand("public.insert_users", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("_firstname", NpgsqlDbType.Varchar, registration.FirstName);
                cmd.Parameters.AddWithValue("_lastname", NpgsqlDbType.Varchar, registration.LastName);
                cmd.Parameters.AddWithValue("_username", NpgsqlDbType.Varchar, registration.UserName);
                cmd.Parameters.AddWithValue("_password", NpgsqlDbType.Varchar, registration.Password);
              
                await connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                connection.Close();
            }
            catch (Exception e)
            {
                await Task.Delay(100);
                throw new Exception("Error-" + e);
            }          
        }
    }
}
