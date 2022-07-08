using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Core.Services
{
    public class NpgsqlConnector : IDatabaseConnection
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public NpgsqlConnector(IConfiguration config)
        {
            _configuration = config;
            _connectionString = _configuration.GetConnectionString("myConnection").Trim();
        }
        public int GetUserId(Guid sessionId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var cmd = new NpgsqlCommand("authorize", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.Add("_sessionid", NpgsqlDbType.Uuid); 
                    cmd.Parameters["_sessionid"].Value = sessionId;

                    connection.Open();

                    var id = cmd.ExecuteScalar();
                   
                    if (id is DBNull)
                    return -1;
                        return (int)id;
                    
                    
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }
        public (int ErrorCode, string ErrorMessage, Guid? SessionId) Authorization(AuthorizationModel authorization)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            { 
                try
                {
                    var cmd = new NpgsqlCommand("player_login", connection) { CommandType = CommandType.StoredProcedure };
                    var a = cmd.Parameters.AddWithValue("_username", authorization.UserName);
                    cmd.Parameters.AddWithValue("_password", authorization.Password);
                    cmd.Parameters.Add("_session_id", NpgsqlDbType.Uuid).Direction = ParameterDirection.Output;
                    connection.Open();
                    var sessionId = (Guid?)cmd.ExecuteScalar();
                    
                    return (1,"done", sessionId);
                }

                catch (Exception e)
                {
                    return (-1, e.Message, null);
                }
            }
        }

        public (int Error, string ErrorMessage) Registration(RegistrationModel registration)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var cmd = new NpgsqlCommand("player_registration", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_firstname", registration.FirstName);
                    cmd.Parameters.AddWithValue("_lastname", registration.LastName);
                    cmd.Parameters.AddWithValue("_username", registration.UserName);
                    cmd.Parameters.AddWithValue("_password", registration.Password);
                    if (registration.FirstName.Length<3)
                    {
                        return (0, "firstname must include minimum 3 symbols");
                    }
                    if (registration.LastName.Length<3)
                    {
                        return (0, "lastname must include minimum 3 symbols");
                    }
                    if (registration.UserName.Length < 3)
                    {
                        return (0, "username must include minimum 3 symbols");
                    }
                    if (registration.Password.Length < 6)
                    {
                        return (0, "password must include minimum 6 symbols");
                    }

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
        public (int Error, string ErrorMessage) CreateMatch(Matchup match)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var cmd = new NpgsqlCommand("create_match", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_createdat", match.CreatedAt);
                    cmd.Parameters.AddWithValue("_stateid", match.StateId);
                    cmd.Parameters.AddWithValue("_points", match.Points);
                    cmd.Parameters.AddWithValue("_boardsize", match.BoardSize);
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
        public List<Matchup> GetAllMatches()
        {
            List<Matchup> output=new List<Matchup>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("getallmatches", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        connection.Open();
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            Matchup match = new Matchup()
                            {
                                Id = (int)reader["_id"],
                                //reader.GetInt32(0),
                                StateId = (int)reader["_state_id"],
                                BoardSize = (int)reader["_board_size"],
                                Points = (int)reader["_points"]
                            };
                            output.Add(match);

                        }
                        
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            return output;
        }
        public (int Error, string ErrorMessage) StartMatch(Matchup match)
        {
            GameLibrary.Match startGame = new GameLibrary.Match();
            return startGame.MakeMove(2, 1);
            
        }
        
    }
}
