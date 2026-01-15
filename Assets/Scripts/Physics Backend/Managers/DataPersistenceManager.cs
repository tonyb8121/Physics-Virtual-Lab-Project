using UnityEngine;
using System;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;

/// <summary>
/// SQLite wrapper for offline data storage.
/// Creates tables for UserProgress and SyncQueue.
/// </summary>
public class DataPersistenceManager : MonoBehaviour
{
    private static DataPersistenceManager _instance;
    public static DataPersistenceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DataPersistenceManager");
                _instance = go.AddComponent<DataPersistenceManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private string _dbPath;
    private IDbConnection _dbConnection;

    public void Initialize()
    {
        _dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "ARPhysicsLab.db");
        Logger.Log($"Initializing Database at: {_dbPath}");
        
        CreateTables();
    }

    private void CreateTables()
    {
        try
        {
            using (_dbConnection = new SqliteConnection(_dbPath))
            {
                _dbConnection.Open();

                using (IDbCommand dbCmd = _dbConnection.CreateCommand())
                {
                    // Table: User Progress
                    string progressTable = "CREATE TABLE IF NOT EXISTS UserProgress (UserId TEXT, ModuleId TEXT, Score INTEGER, Completed INTEGER, Timestamp TEXT, PRIMARY KEY(UserId, ModuleId))";
                    dbCmd.CommandText = progressTable;
                    dbCmd.ExecuteNonQuery();

                    // Table: Sync Queue (for offline actions)
                    string queueTable = "CREATE TABLE IF NOT EXISTS SyncQueue (Id INTEGER PRIMARY KEY AUTOINCREMENT, ActionType TEXT, Payload TEXT, Timestamp TEXT)";
                    dbCmd.CommandText = queueTable;
                    dbCmd.ExecuteNonQuery();
                }
                _dbConnection.Close();
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Database Init Error: {e.Message}");
        }
    }

    public void SaveProgressLocal(string userId, string moduleId, int score, bool completed)
    {
        try
        {
            using (var conn = new SqliteConnection(_dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR REPLACE INTO UserProgress (UserId, ModuleId, Score, Completed, Timestamp) VALUES (@u, @m, @s, @c, @t)";
                    
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@u"; p1.Value = userId; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@m"; p2.Value = moduleId; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@s"; p3.Value = score; cmd.Parameters.Add(p3);
                    var p4 = cmd.CreateParameter(); p4.ParameterName = "@c"; p4.Value = completed ? 1 : 0; cmd.Parameters.Add(p4);
                    var p5 = cmd.CreateParameter(); p5.ParameterName = "@t"; p5.Value = DateTime.UtcNow.ToString("o"); cmd.Parameters.Add(p5);

                    cmd.ExecuteNonQuery();
                }
            }
            Logger.Log($"Saved progress locally for {moduleId}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Save Local Error: {e.Message}");
        }
    }

    public void QueueForSync(string actionType, string jsonPayload)
    {
        try
        {
            using (var conn = new SqliteConnection(_dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO SyncQueue (ActionType, Payload, Timestamp) VALUES (@a, @p, @t)";
                    
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@a"; p1.Value = actionType; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@p"; p2.Value = jsonPayload; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@t"; p3.Value = DateTime.UtcNow.ToString("o"); cmd.Parameters.Add(p3);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Queue Error: {e.Message}");
        }
    }

    public void SyncQueuedData()
    {
        // This will be connected to FirestoreHelper in Phase 2
        Logger.Log("Checking sync queue...");
        // Logic: Read from SyncQueue -> Send to Firebase -> Delete from SyncQueue
    }

    public void SaveAllPendingData()
    {
        // Close connections if any are open (handled by 'using' blocks usually)
        if (_dbConnection != null && _dbConnection.State == ConnectionState.Open)
        {
            _dbConnection.Close();
        }
    }
}