using Npgsql;
using NpgsqlTypes;
using System.Data;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Core.Services
{
    public class UserAuthorization : IUserAuthorization
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public UserAuthorization(IConfiguration config)
        {
            _configuration = config;
            _connectionString = _configuration.GetConnectionString("myConnection").Trim();
        }
        public (int Error, string ErrorMessage) Authorization(Authorization authorization)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            try
            {
               
                using (var cmd = new NpgsqlCommand("player_login", connection) {CommandType=CommandType.StoredProcedure})
                {                
                    cmd.Parameters.AddWithValue("_username", NpgsqlDbType.Varchar, authorization.UserName);
                    cmd.Parameters.AddWithValue("_password", NpgsqlDbType.Varchar, authorization.Password);
                    connection.Open();
                    cmd.ExecuteNonQuery();
                };

                return (0, "done");
            }

            catch (Exception e)
            {
                return (-1, e.Message);
            }
        }
    }
}
