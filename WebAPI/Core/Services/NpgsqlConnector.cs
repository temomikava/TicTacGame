﻿using Microsoft.Extensions.Configuration;
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
                    if (id == null)
                        return -1;
                    return (int)id;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
            return -1;
        }
        public (int Error, string ErrorMessage, Guid SessionId) Authorization(AuthorizationModel authorization)
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
                    //cmd.ExecuteNonQuery();
                    authorization.SessionID = (Guid)cmd.ExecuteScalar();
                    return (0,"done", authorization.SessionID);
                }

                catch (Exception e)
                {
                    return (-1, e.Message, authorization.SessionID);
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
                    cmd.Parameters.AddWithValue("started_at", match.StartedAt);
                    cmd.Parameters.AddWithValue("finished_at", match.FinishedAt);
                    cmd.Parameters.AddWithValue("state_id", match.StateId);
                    cmd.Parameters.AddWithValue("winner_player_id", match.Winner_Player_id);
                    cmd.Parameters.AddWithValue("points", match.Points);
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
        public (int Error, string ErrorMessage) StartMatch(Matchup match)
        {
            GameLibrary.Match startGame = new GameLibrary.Match();
            return startGame.MakeMove(2, 1);
            
        }
        
    }
}
