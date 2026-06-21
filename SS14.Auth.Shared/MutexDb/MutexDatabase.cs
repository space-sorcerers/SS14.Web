using System;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace SS14.Auth.Shared.MutexDb;

/// <summary>
/// Simple SQLite DB to act as a mutex around hourly email send counters. 
/// </summary>
public sealed class MutexDatabase : IDisposable
{
    private readonly IOptions<MutexOptions> _options;
    private SqliteConnection _connection;

    public MutexDatabase(IOptions<MutexOptions> options)
    {
        _options = options;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    public int IncCount(string key)
    {
        _connection ??= OpenConnection();

        // Auto-create table if it doesn't exist
        _connection.Execute(
            "CREATE TABLE IF NOT EXISTS MutexCounts (Key TEXT PRIMARY KEY NOT NULL, Count INTEGER NOT NULL) WITHOUT ROWID");

        using var tx = _connection.BeginTransaction(deferred: false);

        var count = _connection.QuerySingleOrDefault<int>(
            "SELECT Count FROM MutexCounts WHERE Key = @Key",
            new { Key = key });

        count += 1;

        _connection.Execute("INSERT OR REPLACE INTO MutexCounts (Key, Count) VALUES (@Key, @Count)", new
        {
            Key = key,
            Count = count
        });

        tx.Commit();
        
        return count;
    }

    public void EnsureCreated()
    {
        _connection ??= OpenConnection();
        _connection.Execute(
            "CREATE TABLE IF NOT EXISTS MutexCounts (Key TEXT PRIMARY KEY NOT NULL, Count INTEGER NOT NULL) WITHOUT ROWID");
    }
    
    private SqliteConnection OpenConnection()
    {
        var options = _options.Value;
        var conString = $"Data Source={options.DbPath};Mode=ReadWriteCreate;Pooling=True;Foreign Keys=True";

        var con = new SqliteConnection(conString);
        con.Open();
        return con;
    }
}
