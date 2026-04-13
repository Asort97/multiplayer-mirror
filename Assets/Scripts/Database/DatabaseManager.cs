using UnityEngine;
using MySqlConnector;
using System;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Header("MySQL")]
    [SerializeField] private string host = "localhost";
    [SerializeField] private int port = 3306;
    [SerializeField] private string database = "laststanding";
    [SerializeField] private string user = "root";
    [SerializeField] private string password = "root";

    private string connectionString;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    public void Init()
    {
        if (initialized) return;

        connectionString = string.Format(
            "Server={0};Port={1};Database={2};Uid={3};Pwd={4};CharSet=utf8;",
            host, port, database, user, password
        );

        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS players (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nickname VARCHAR(64) UNIQUE NOT NULL,
                        wins INT DEFAULT 0,
                        losses INT DEFAULT 0,
                        kills INT DEFAULT 0,
                        deaths INT DEFAULT 0
                    )";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS client_profiles (
                        instance_key VARCHAR(255) PRIMARY KEY,
                        nickname VARCHAR(64) NOT NULL,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    )";
                    cmd.ExecuteNonQuery();
                }
            }
            initialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("MySQL Init failed: " + e.Message);
        }
    }

    public void GetOrCreatePlayer(string nickname)
    {
        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT IGNORE INTO players (nickname) VALUES (@nick)";
                    cmd.Parameters.AddWithValue("@nick", nickname);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("MySQL GetOrCreatePlayer: " + e.Message);
        }
    }

    public void RecordMatchResult(string nickname, bool won, int kills)
    {
        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT IGNORE INTO players (nickname) VALUES (@nick)";
                    cmd.Parameters.AddWithValue("@nick", nickname);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    if (won)
                        cmd.CommandText = "UPDATE players SET wins = wins + 1, kills = kills + @k WHERE nickname = @nick";
                    else
                        cmd.CommandText = "UPDATE players SET losses = losses + 1, deaths = deaths + 1, kills = kills + @k WHERE nickname = @nick";

                    cmd.Parameters.AddWithValue("@nick", nickname);
                    cmd.Parameters.AddWithValue("@k", kills);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("MySQL RecordMatchResult: " + e.Message);
        }
    }

    public (int wins, int losses, int kills, int deaths) GetPlayerStats(string nickname)
    {
        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT wins, losses, kills, deaths FROM players WHERE nickname = @nick";
                    cmd.Parameters.AddWithValue("@nick", nickname);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("MySQL GetPlayerStats: " + e.Message);
        }
        return (0, 0, 0, 0);
    }

    public string GetSavedNickname(string instanceKey)
    {
        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT nickname FROM client_profiles WHERE instance_key = @key LIMIT 1";
                    cmd.Parameters.AddWithValue("@key", instanceKey);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        return val.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("MySQL GetSavedNickname: " + e.Message);
        }

        return "";
    }

    public void SaveNickname(string instanceKey, string nickname)
    {
        try
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO client_profiles (instance_key, nickname) VALUES (@key, @nick) ON DUPLICATE KEY UPDATE nickname = VALUES(nickname)";
                    cmd.Parameters.AddWithValue("@key", instanceKey);
                    cmd.Parameters.AddWithValue("@nick", nickname);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("MySQL SaveNickname: " + e.Message);
        }
    }
}
