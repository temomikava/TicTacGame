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
        public async Task<Mark[,]> FillGrid(Match match)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                List<Move> moves = new List<Move>();
                try
                {
                    var cmd = new NpgsqlCommand("getmovesbymatchid", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_matchid", match.Id);
                    var game = await GetGameByID(match.GameId);

                    connection.Open();
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Move move = new Move();

                        move.PlayerId = (int)reader["player_id"];
                        move.MatchId = (int)reader["match_id"];
                        move.RowCoordinate = (int)reader["row_coordinate"];
                        move.ColumnCoordinate = (int)reader["column_coordinate"];
                        moves.Add(move);
                    }
                    //int boardSize = (int)System.Math.Sqrt(game.BoardSize);
                    Mark[,] grid = new Mark[game.BoardSize, game.BoardSize];
                    var playerOneMoves = moves.Where(x => x.PlayerId == game.PlayerOne.Id).ToList();
                    var playerTwoMoves = moves.Where(x => x.PlayerId == game.PlayerTwo.Id).ToList();
                    playerOneMoves.ForEach(x => grid[x.RowCoordinate, x.ColumnCoordinate] = Mark.X);
                    playerTwoMoves.ForEach(x => grid[x.RowCoordinate, x.ColumnCoordinate] = Mark.O);
                    return grid;

                }
                catch (Exception)
                {

                    throw;
                }
            }

        }

        public void MakeMove(Match match, int r, int c)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var playerId = match.CurrentPlayerId == match.PlayerOne.Id ? match.PlayerTwo.Id : match.PlayerOne.Id;
                    var mark = playerId == match.PlayerOne.Id ? "X" : "O";
                    var cmd = new NpgsqlCommand("makemove", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_playerid", playerId);
                    cmd.Parameters.AddWithValue("_mark", mark);
                    cmd.Parameters.AddWithValue("_matchid", match.Id);
                    cmd.Parameters.AddWithValue("_matchover", match.MatchOver);
                    cmd.Parameters.AddWithValue("_rowcoordinate", r);
                    cmd.Parameters.AddWithValue("_columncoordinate", c);
                    cmd.Parameters.AddWithValue("_turnspassed", match.TurnsPassed);
                    cmd.Parameters.AddWithValue("_currentplayerid", match.CurrentPlayerId);
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {

                    throw;
                }
            }

        }
        public async Task<Match> GetActiveMatch(int gameId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var activeMatch = new Match();
                try
                {
                    var cmd = new NpgsqlCommand("get_active_matches", connection) { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("_game_id", gameId);
                    connection.Open();
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        return null;
                    }
                    while (reader.Read())
                    {
                        activeMatch.Id = (int)reader["id"];
                        activeMatch.GameId = (int)reader["gameid"];
                        activeMatch.TurnsPassed = (int)reader["turnspassed"];
                        activeMatch.CurrentPlayerId = (int)reader["currentplayerid"];
                        activeMatch.StartedAt = (DateTime)reader["started_at"];
                    }


                    return activeMatch;
                }
                catch (Exception)
                {

                    return null;
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

        public (int ErrorCode, string ErrorMessage) GameStart(int gameId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("gamestart", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", gameId);
                        cmd.Parameters.AddWithValue("_startedat", DateTime.Now);
                        cmd.Parameters.AddWithValue("_stateid", (int)StateType.Started);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        return (1, "success");
                    }

                }
                catch (Exception)
                {

                    throw;
                }
            }

        }

        public (int ErrorCode, string ErrorMessage, int matchId) MatchStart(Match match)
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
                        cmd.Parameters.AddWithValue("_currentplayerid", match.CurrentPlayerId);
                        connection.Open();
                        var matchid = cmd.ExecuteScalar();
                        return (1, "success", (int)matchid);
                    }

                }
                catch (Exception)
                {

                    throw;
                }
            }

        }
        public (int ErrorCode, string ErrorMessage) MatchEnd(Match match)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("match_end", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_matchid", match.Id);
                        cmd.Parameters.AddWithValue("_gameid", match.GameId);
                        cmd.Parameters.AddWithValue("_playeronescore", match.PlayerOneScore);
                        cmd.Parameters.AddWithValue("_playertwoscore", match.PlayerTwoScore);
                        cmd.Parameters.AddWithValue("_finishedat", DateTime.Now);
                        cmd.Parameters.AddWithValue("_winnerid", match.WinnerId);
                        cmd.Parameters.AddWithValue("_stateid", (int)StateType.Finished);
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
            return (1, "success");
        }
        public (int ErrorCode, string ErrorMessage) JoinToGame(int gameId, int playerId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("jointogame", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", gameId);
                        cmd.Parameters.AddWithValue("_playerid", playerId);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        return (1, "success");
                    }

                }
                catch (Exception ex)
                {

                    return (-1, ex.Message);
                }
            }

        }

        public (int ErrorCode, string ErrorMessage) GameEnd(Game game)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("game_end", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", game.GameId);
                        cmd.Parameters.AddWithValue("_finishedat", DateTime.Now);
                        cmd.Parameters.AddWithValue("_stateid", game.StateId);
                        cmd.Parameters.AddWithValue("_winnerid", game.Winner_Player_id);
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


        public async Task<(int ErrorCode, string ErrorMessage, string? Username)> GetUsername(int userId)
        {
            var connection = new NpgsqlConnection(_connectionString);

            try
            {
                var cmd = new NpgsqlCommand("get_username", connection) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("_id", userId);
                connection.Open();
                var username = cmd.ExecuteScalar();
                return (1, "success", username?.ToString());
            }
            catch (Exception)
            {
                return (-1, "fail", "fail");
            }
            finally
            {
                connection.Close();
            }
        }


        public async Task<(int erorrCode, string message, int gameId)> GameCreate(Game game)
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

                    // return await Task<(1, "done", (int)id)>;
                    return new(1, "done", (int)id);
                }
                catch (Exception e)
                {
                    return new(-1, "fail", -1);
                }
            }
        }



        public async Task<Game> GetGameByID(int gameId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    var game = new Game();
                    using (var cmd = new NpgsqlCommand("get_game_by_id", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_game_id", gameId);
                        connection.Open();
                        NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            game.GameId = (int)reader["_id"];
                            game.CreatedAt = (DateTime)reader["_created_at"];
                            game.StartedAt = reader["_started_at"] is DBNull ? null : (DateTime)reader["_started_at"];
                            game.PlayerTwo = reader["_player_two_id"] is DBNull ? new Player() : new Player { Id = (int)reader["_player_two_id"] };
                            game.PlayerOneScore = reader["_player_one_score"] is DBNull ? 0 : (int)reader["_player_one_score"];
                            game.PlayerTwoScore = reader["_player_two_score"] is DBNull ? 0 : (int)reader["_player_two_score"];
                            game.PlayerTwo.UserName = GetUsername(game.PlayerTwo.Id).Result.Username;

                            game.StateId = (int)reader["_state_id"];
                            game.BoardSize = (int)reader["_board_size"];
                            game.TargetScore = (int)reader["_target_score"];
                            game.PlayerOne = new Player { Id = (int)reader["_player_one_id"] };
                            game.PlayerOne.UserName = GetUsername(game.PlayerOne.Id).Result.Username;
                        }

                        return game;

                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
        public async Task<IEnumerable<Game>> GetGames()
        {
            List<Game> output = new List<Game>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("getallgames", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        connection.Open();
                        NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            Game game = new Game();


                            game.GameId = (int)reader["_id"];
                            game.CreatedAt = (DateTime)reader["_created_at"];
                            game.StartedAt = reader["_started_at"] is DBNull ? null : (DateTime)reader["_started_at"];
                            game.PlayerTwo = reader["_player_two_id"] is DBNull ? new Player() : new Player { Id = (int)reader["_player_two_id"] };
                            if (game.PlayerTwo.Id != 0)
                            {
                                game.PlayerTwo = new Player { Id = game.PlayerTwo.Id, UserName = GetUsername(game.PlayerTwo.Id).Result.Username };
                            }
                            game.PlayerOneScore = reader["_player_one_score"] is DBNull ? 0 : (int)reader["_player_one_score"];
                            game.PlayerTwoScore = reader["_player_two_score"] is DBNull ? 0 : (int)reader["_player_two_score"];

                            game.StateId = (int)reader["_state_id"];
                            game.BoardSize = (int)reader["_board_size"];
                            game.TargetScore = (int)reader["_target_score"];
                            game.PlayerOne = reader["_player_one_id"] is DBNull ? new Player() : new Player { Id = (int)reader["_player_one_id"] };
                            game.PlayerOne = new Player { Id = game.PlayerOne.Id, UserName = GetUsername(game.PlayerOne.Id).Result.Username };
                            output.Add(game);




                        }

                    }
                    return output;
                }
                catch (Exception ex)
                {

                    throw new Exception(ex.Message + " problem with load games");
                }
            }
        }

        public async Task Ondisconnected(int gameId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("ondisconncted", connection) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("_gameid", gameId);
                        cmd.Parameters.AddWithValue("_stateid", (int)StateType.Cancelled);
                        connection.Open();
                        await cmd.ExecuteNonQueryAsync();

                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }

        }

        public async Task WaitingForReconnect(int gameId, int stateId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                try
                {
                    using (var cmd = new NpgsqlCommand("waitingforreconnect", connection) { CommandType = CommandType.StoredProcedure })
                    {

                        cmd.Parameters.AddWithValue("_gameid", gameId);
                        cmd.Parameters.AddWithValue("_stateid", stateId);
                        connection.Open();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        public async Task<int[]> GetMovesHistory(int gameId)
        {
            var match = GetActiveMatch(gameId).Result;
            var grid = FillGrid(match).Result;
            var output = new List<int>();

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i,j]==Mark.X)
                    {
                        output.Add(1);
                    }
                    if (grid[i, j] == Mark.O)
                    {
                        output.Add(0);
                    }
                    if (grid[i, j] == Mark.None)
                    {
                        output.Add(-1);
                    }

                }
            }
            return  output.ToArray();
        }




        //public (int Error, string ErrorMessage) StartGame(Game mainGame)
        //{
        //    GameLibrary.Game startGame = new GameLibrary.Game();
        //    return startGame.MakeMove(2, 1);

        //}

    }
}
