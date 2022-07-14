using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using WebAPI.Core.Interface;
using WebAPI.Models;
using GameLibrary;
using GameLibrary.Enums;

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

                    return (1, "done", sessionId);
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
                    if (registration.FirstName.Length < 3)
                    {
                        return (0, "firstname must include minimum 3 symbols");
                    }
                    if (registration.LastName.Length < 3)
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

                    return (1, "done");
                }

                catch (Exception e)
                {
                    return (-1, e.Message);
                }
            }
        }
        public (int ErrorCode, string ErrorMessage) MakeMove(Move move)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("makemove", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_playerid",move.PlayerId);
                        cmd.Parameters.AddWithValue("_rowcoordinate",move.RowCoordinate);
                        cmd.Parameters.AddWithValue("_columncoordinate", move.ColumnCoordinate);
                        cmd.Parameters.AddWithValue("_matchid", move.MatchId);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        return (1, "success");
                    }

                }
                catch (Exception)
                {
                    return (-1, "fail");
                }
            }

        }
        public (int ErrorCode, string ErrorMessage,int matchId) MatchStart(Match match)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("match_start", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", match.GameId);
                        cmd.Parameters.AddWithValue("_stateid", match.StateId);
                        cmd.Parameters.AddWithValue("_startedat", DateTime.Now);
                        var matchid=cmd.ExecuteScalar();
                        return (1, "success", (int)matchid);
                    }

                }
                catch (Exception)
                {

                    return (-1, "fail", -1);
                }
            }

        }
        public (int ErrorCode, string ErrorMessage) MatchEnd(Match match)
        {
            //    using (var connection = new NpgsqlConnection(_connectionString))
            //    {
            //        try
            //        {
            //            using (var cmd = new NpgsqlCommand("match_end", connection) { CommandType = CommandType.StoredProcedure })
            //            {
            //                cmd.Parameters.AddWithValue("_matchid", match.Id);
            //                cmd.Parameters.AddWithValue("_finishedat",match.FinishedAt);
            //                cmd.Parameters.AddWithValue("_winnerid",match.Winner_Player_id);
            //                connection.Open();
            //                cmd.ExecuteNonQuery();
            //                return (1, "success");
            //            }

            //        }
            //        catch (Exception)
            //        {

            //            return (-1, "fail");
            //        }
            //    }
                 return (1, "success");
            }
            public (int ErrorCode, string ErrorMessage) GameEnd(Game game)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("game_end", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", game.Id);
                        cmd.Parameters.AddWithValue("_finishedat", DateTime.Now);
                        cmd.Parameters.AddWithValue("_stateid", game.StateId);
                        cmd.Parameters.AddWithValue("_playeronescore", game.PlayerOneScore);
                        cmd.Parameters.AddWithValue("_playertwoscore", game.PlayerTwoScore);
                        cmd.Parameters.AddWithValue("_winnerid", game.Winner_Player_id);
                        return (1, "success");
                    }

                }
                catch (Exception)
                {

                    return (-1, "fail");
                }
            }

        }


        public (int ErrorCode, string ErrorMessage, string Username) GetUsername(int userId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var cmd = new NpgsqlCommand("get_username", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_id",userId);
                    connection.Open();
                    var username=cmd.ExecuteScalar();
                    return (1, "success", username.ToString());
                }
                catch (Exception)
                {
                    return (-1, "fail", "fail");
                }
            }

        }      
        public (int ErrorCode, string ErrorMessage) GameStart(Game game)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("gamestart", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", game.Id);
                        cmd.Parameters.AddWithValue("_startedat", game.StartedAt);
                        cmd.Parameters.AddWithValue("_stateid", game.StateId);
                        cmd.Parameters.AddWithValue("_playertwoid", game.PlayerTwo.Id);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }

                    return (1, "success");

                }
                catch (Exception)
                {

                    return (-1, "join to game failed");
                }
            }
        }

        public (int ErrorCode, string ErrorMessage, int GameId) GameCreate(Game game)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var cmd = new NpgsqlCommand("create_game", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_createdat", game.CreatedAt);
                    cmd.Parameters.AddWithValue("_stateid", game.StateId);
                    cmd.Parameters.AddWithValue("_scoretarget", game.TargetScore);
                    cmd.Parameters.AddWithValue("_boardsize", game.BoardSize);
                    cmd.Parameters.AddWithValue("_playeroneid", game.PlayerOne.Id);
                    connection.Open();
                    var id = cmd.ExecuteScalar();

                    return (1, "done", (int)id);
                }
                catch (Exception e)
                {
                    return (-1, e.Message, -1);
                }
            }
        }
        public List<Game> GetGames()
        {
            List<Game> output = new List<Game>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("getallgames", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        connection.Open();
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            Game game = new Game();
                            //match.Id = (int)reader["_id"];
                            //match.StateId= (int)reader["_state_id"];
                            //match.TargetScore= (int)reader["_points"];
                            if ((int)reader["_state_id"]==1)
                            {
                                game.Id = (int)reader["_id"];
                                game.CreatedAt = (DateTime)reader["_created_at"];
                                game.StateId = (int)reader["_state_id"];
                                game.BoardSize = (int)reader["_board_size"];
                                game.TargetScore = (int)reader["_target_score"];
                                game.PlayerOne.Id = (int)reader["_player_one_id"];
                                game.PlayerOne.UserName = GetUsername(game.PlayerOne.Id).Username;
                                output.Add(game);

                            }
                            else if((int)reader["_state_id"] == 2)
                            {
                                game.Id = (int)reader["_id"];
                                game.StartedAt = (DateTime)reader["_started_at"];
                                game.StateId = (int)reader["_state_id"];
                                game.PlayerOne.Id = (int)reader["_player_one_id"];
                                game.PlayerTwo.Id = (int)reader["_player_two_id"];
                                game.BoardSize = (int)reader["_board_size"];
                                game.TargetScore = (int)reader["_target_score"];
                                game.PlayerOne.UserName = GetUsername(game.PlayerOne.Id).Username;
                                game.PlayerTwo.UserName = GetUsername(game.PlayerTwo.Id).Username;
                                output.Add(game);

                            }


                        }

                    }
                    return output;  
                }
                catch (Exception ex)
                {

                    throw new Exception(ex.Message+" problem with load games");
                }
            }
        }
        //public (int Error, string ErrorMessage) StartGame(Game game)
        //{
        //    GameLibrary.Game startGame = new GameLibrary.Game();
        //    return startGame.MakeMove(2, 1);

        //}

    }
}
