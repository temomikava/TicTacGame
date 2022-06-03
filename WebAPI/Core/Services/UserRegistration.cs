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

        public int statusID;
        public UserRegistration(IConfiguration config)
        {
            _configuration = config;
            _connectionString = _configuration.GetConnectionString("myConnection").Trim();
        }

        public (int Error, string ErrorMessage) Registration(Registration registration)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            try
            {
                using var cmd = new NpgsqlCommand("public.insert_users", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("_firstname", NpgsqlDbType.Varchar, registration.FirstName);
                cmd.Parameters.AddWithValue("_lastname", NpgsqlDbType.Varchar, registration.LastName);
                cmd.Parameters.AddWithValue("_username", NpgsqlDbType.Varchar, registration.UserName);
                cmd.Parameters.AddWithValue("_password", NpgsqlDbType.Varchar, registration.Password);

                connection.Open();
                cmd.ExecuteNonQuery();


                return (0, "done");
            }

            catch (Exception e)
            {
                return (-1, e.Message);
            }
        }
    }
   
}
