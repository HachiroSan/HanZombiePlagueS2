using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace HanZombiePlagueS2;

public sealed class HZPDatabaseRepository(
    ISwiftlyCore core,
    ILogger<HZPDatabaseRepository> logger,
    IOptionsMonitor<HZPDatabaseConfig> configMonitor)
{
    private enum SqlDialect
    {
        MySql,
        Sqlite
    }

    private const string PlayersTable = "players";
    private const string PlayerPreferencesTable = "player_preferences";
    private const string PlayerStatsTable = "player_stats";
    private const string PlayerCurrencyTable = "player_currency";
    private const string PlayerCurrencyLedgerTable = "player_currency_ledger";

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var dialect = GetDialect(connection);
        var statements = GetEnsureSchemaStatements(dialect);

        await OpenAsync(connection, cancellationToken);

        foreach (var statement in statements)
        {
            await connection.ExecuteAsync(new CommandDefinition(statement, cancellationToken: cancellationToken));
        }

        logger.LogInformation(
            "HZP database schema verified for connection key {ConnectionKey} using {Dialect}.",
            CurrentConfig.ConnectionKey,
            dialect);
    }

    public async Task TouchPlayerAsync(ulong steamId, string? lastKnownName, CancellationToken cancellationToken = default)
    {
        if (steamId == 0)
        {
            return;
        }

        var parameters = new
        {
            SteamId = steamId,
            LastKnownName = TrimOrNull(lastKnownName, 128)
        };

        using var connection = CreateConnection();
        var sql = GetTouchPlayerSql(GetDialect(connection));

        await OpenAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<HZPPlayerPreferenceRecord?> GetPlayerPreferenceAsync(ulong steamId, string preferenceKey, CancellationToken cancellationToken = default)
    {
        ValidateScopedKey(preferenceKey, nameof(preferenceKey));

        var sql = $"""
            SELECT
                steam_id AS SteamId,
                preference_key AS PreferenceKey,
                preference_value AS PreferenceValue,
                updated_utc AS UpdatedUtc
            FROM {PlayerPreferencesTable}
            WHERE steam_id = @SteamId
              AND preference_key = @PreferenceKey
            LIMIT 1;
            """;

        using var connection = CreateConnection();
        await OpenAsync(connection, cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<HZPPlayerPreferenceRecord>(
            new CommandDefinition(
                sql,
                new { SteamId = steamId, PreferenceKey = preferenceKey },
                cancellationToken: cancellationToken));
    }

    public async Task SavePlayerPreferenceAsync(ulong steamId, string preferenceKey, string? preferenceValue, CancellationToken cancellationToken = default)
    {
        ValidateScopedKey(preferenceKey, nameof(preferenceKey));

        using var connection = CreateConnection();
        var sql = GetSavePlayerPreferenceSql(GetDialect(connection));

        await OpenAsync(connection, cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    SteamId = steamId,
                    PreferenceKey = preferenceKey,
                    PreferenceValue = TrimOrNull(preferenceValue, 255)
                },
                cancellationToken: cancellationToken));
    }

    public async Task<HZPPlayerStatsRecord> GetPlayerStatsAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT
                steam_id AS SteamId,
                infections AS Infections,
                deaths AS Deaths,
                rounds_played AS RoundsPlayed,
                rounds_won AS RoundsWon,
                updated_utc AS UpdatedUtc
            FROM {PlayerStatsTable}
            WHERE steam_id = @SteamId
            LIMIT 1;
            """;

        using var connection = CreateConnection();
        await OpenAsync(connection, cancellationToken);
        var record = await connection.QuerySingleOrDefaultAsync<HZPPlayerStatsRecord>(
            new CommandDefinition(sql, new { SteamId = steamId }, cancellationToken: cancellationToken));

        return record ?? new HZPPlayerStatsRecord
        {
            SteamId = steamId,
            UpdatedUtc = DateTime.UtcNow
        };
    }

    public async Task IncrementPlayerStatsAsync(ulong steamId, HZPPlayerStatsDelta delta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delta);

        using var connection = CreateConnection();
        var sql = GetIncrementPlayerStatsSql(GetDialect(connection));

        await OpenAsync(connection, cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    SteamId = steamId,
                    delta.Infections,
                    delta.Deaths,
                    delta.RoundsPlayed,
                    delta.RoundsWon
                },
                cancellationToken: cancellationToken));
    }

    public async Task<HZPPlayerCurrencyRecord> GetPlayerCurrencyAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT
                steam_id AS SteamId,
                balance AS Balance,
                lifetime_earned AS LifetimeEarned,
                lifetime_spent AS LifetimeSpent,
                updated_utc AS UpdatedUtc
            FROM {PlayerCurrencyTable}
            WHERE steam_id = @SteamId
            LIMIT 1;
            """;

        using var connection = CreateConnection();
        await OpenAsync(connection, cancellationToken);
        var record = await connection.QuerySingleOrDefaultAsync<HZPPlayerCurrencyRecord>(
            new CommandDefinition(sql, new { SteamId = steamId }, cancellationToken: cancellationToken));

        return record ?? new HZPPlayerCurrencyRecord
        {
            SteamId = steamId,
            UpdatedUtc = DateTime.UtcNow
        };
    }

    public async Task AddCurrencyAsync(ulong steamId, int amount, string reason, CancellationToken cancellationToken = default)
    {
        if (steamId == 0 || amount <= 0)
        {
            return;
        }

        using var connection = CreateDbConnection();
        await OpenAsync(connection, cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var dialect = GetDialect(connection);
            await EnsureCurrencyRowAsync(connection, transaction, dialect, steamId, cancellationToken);

            var updateSql = GetAddCurrencySql(dialect);
            await connection.ExecuteAsync(new CommandDefinition(
                updateSql,
                new { SteamId = steamId, Amount = amount },
                transaction,
                cancellationToken: cancellationToken));

            await InsertCurrencyLedgerAsync(connection, transaction, dialect, steamId, amount, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> TrySpendCurrencyAsync(ulong steamId, int amount, string reason, CancellationToken cancellationToken = default)
    {
        if (steamId == 0)
        {
            return false;
        }

        if (amount <= 0)
        {
            return true;
        }

        using var connection = CreateDbConnection();
        await OpenAsync(connection, cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var dialect = GetDialect(connection);
            await EnsureCurrencyRowAsync(connection, transaction, dialect, steamId, cancellationToken);

            var spendSql = GetSpendCurrencySql(dialect);
            var affected = await connection.ExecuteAsync(new CommandDefinition(
                spendSql,
                new { SteamId = steamId, Amount = amount },
                transaction,
                cancellationToken: cancellationToken));

            if (affected <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await InsertCurrencyLedgerAsync(connection, transaction, dialect, steamId, -amount, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private HZPDatabaseConfig CurrentConfig => configMonitor.CurrentValue;

    private IDbConnection CreateConnection()
    {
        return core.Database.GetConnection(CurrentConfig.ConnectionKey);
    }

    private static SqlDialect GetDialect(IDbConnection connection)
    {
        var typeName = connection.GetType().FullName ?? connection.GetType().Name;
        return typeName.Contains("sqlite", StringComparison.OrdinalIgnoreCase)
            ? SqlDialect.Sqlite
            : SqlDialect.MySql;
    }

    private static IReadOnlyList<string> GetEnsureSchemaStatements(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite =>
            [
                $"""
                CREATE TABLE IF NOT EXISTS {PlayersTable} (
                    steam_id INTEGER NOT NULL PRIMARY KEY,
                    last_known_name TEXT NULL,
                    first_seen_utc TEXT NOT NULL,
                    last_seen_utc TEXT NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerPreferencesTable} (
                    steam_id INTEGER NOT NULL,
                    preference_key TEXT NOT NULL,
                    preference_value TEXT NULL,
                    updated_utc TEXT NOT NULL,
                    PRIMARY KEY (steam_id, preference_key)
                )
                """,
                $"""
                CREATE INDEX IF NOT EXISTS idx_player_preferences_steam_id
                    ON {PlayerPreferencesTable} (steam_id)
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerStatsTable} (
                    steam_id INTEGER NOT NULL PRIMARY KEY,
                    infections INTEGER NOT NULL DEFAULT 0,
                    deaths INTEGER NOT NULL DEFAULT 0,
                    rounds_played INTEGER NOT NULL DEFAULT 0,
                    rounds_won INTEGER NOT NULL DEFAULT 0,
                    updated_utc TEXT NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerCurrencyTable} (
                    steam_id INTEGER NOT NULL PRIMARY KEY,
                    balance INTEGER NOT NULL DEFAULT 0,
                    lifetime_earned INTEGER NOT NULL DEFAULT 0,
                    lifetime_spent INTEGER NOT NULL DEFAULT 0,
                    updated_utc TEXT NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerCurrencyLedgerTable} (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    steam_id INTEGER NOT NULL,
                    reason TEXT NOT NULL,
                    amount INTEGER NOT NULL,
                    created_utc TEXT NOT NULL
                )
                """,
                $"""
                CREATE INDEX IF NOT EXISTS idx_player_currency_ledger_steam_id
                    ON {PlayerCurrencyLedgerTable} (steam_id)
                """
            ],
            _ =>
            [
                $"""
                CREATE TABLE IF NOT EXISTS {PlayersTable} (
                    steam_id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                    last_known_name VARCHAR(128) NULL,
                    first_seen_utc DATETIME(6) NOT NULL,
                    last_seen_utc DATETIME(6) NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerPreferencesTable} (
                    steam_id BIGINT UNSIGNED NOT NULL,
                    preference_key VARCHAR(64) NOT NULL,
                    preference_value VARCHAR(255) NULL,
                    updated_utc DATETIME(6) NOT NULL,
                    PRIMARY KEY (steam_id, preference_key),
                    KEY idx_player_preferences_steam_id (steam_id)
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerStatsTable} (
                    steam_id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                    infections INT NOT NULL DEFAULT 0,
                    deaths INT NOT NULL DEFAULT 0,
                    rounds_played INT NOT NULL DEFAULT 0,
                    rounds_won INT NOT NULL DEFAULT 0,
                    updated_utc DATETIME(6) NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerCurrencyTable} (
                    steam_id BIGINT UNSIGNED NOT NULL PRIMARY KEY,
                    balance INT NOT NULL DEFAULT 0,
                    lifetime_earned INT NOT NULL DEFAULT 0,
                    lifetime_spent INT NOT NULL DEFAULT 0,
                    updated_utc DATETIME(6) NOT NULL
                )
                """,
                $"""
                CREATE TABLE IF NOT EXISTS {PlayerCurrencyLedgerTable} (
                    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    steam_id BIGINT UNSIGNED NOT NULL,
                    reason VARCHAR(128) NOT NULL,
                    amount INT NOT NULL,
                    created_utc DATETIME(6) NOT NULL,
                    KEY idx_player_currency_ledger_steam_id (steam_id)
                )
                """
            ]
        };
    }

    private static string GetTouchPlayerSql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                INSERT INTO {PlayersTable} (steam_id, last_known_name, first_seen_utc, last_seen_utc)
                VALUES (@SteamId, @LastKnownName, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT(steam_id) DO UPDATE SET
                    last_known_name = excluded.last_known_name,
                    last_seen_utc = CURRENT_TIMESTAMP
                """,
            _ => $"""
                INSERT INTO {PlayersTable} (steam_id, last_known_name, first_seen_utc, last_seen_utc)
                VALUES (@SteamId, @LastKnownName, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE
                    last_known_name = VALUES(last_known_name),
                    last_seen_utc = UTC_TIMESTAMP(6)
                """
        };
    }

    private static string GetSavePlayerPreferenceSql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                INSERT INTO {PlayerPreferencesTable} (steam_id, preference_key, preference_value, updated_utc)
                VALUES (@SteamId, @PreferenceKey, @PreferenceValue, CURRENT_TIMESTAMP)
                ON CONFLICT(steam_id, preference_key) DO UPDATE SET
                    preference_value = excluded.preference_value,
                    updated_utc = CURRENT_TIMESTAMP
                """,
            _ => $"""
                INSERT INTO {PlayerPreferencesTable} (steam_id, preference_key, preference_value, updated_utc)
                VALUES (@SteamId, @PreferenceKey, @PreferenceValue, UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE
                    preference_value = VALUES(preference_value),
                    updated_utc = UTC_TIMESTAMP(6)
                """
        };
    }

    private static string GetIncrementPlayerStatsSql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                INSERT INTO {PlayerStatsTable} (
                    steam_id,
                    infections,
                    deaths,
                    rounds_played,
                    rounds_won,
                    updated_utc)
                VALUES (
                    @SteamId,
                    @Infections,
                    @Deaths,
                    @RoundsPlayed,
                    @RoundsWon,
                    CURRENT_TIMESTAMP)
                ON CONFLICT(steam_id) DO UPDATE SET
                    infections = infections + excluded.infections,
                    deaths = deaths + excluded.deaths,
                    rounds_played = rounds_played + excluded.rounds_played,
                    rounds_won = rounds_won + excluded.rounds_won,
                    updated_utc = CURRENT_TIMESTAMP
                """,
            _ => $"""
                INSERT INTO {PlayerStatsTable} (
                    steam_id,
                    infections,
                    deaths,
                    rounds_played,
                    rounds_won,
                    updated_utc)
                VALUES (
                    @SteamId,
                    @Infections,
                    @Deaths,
                    @RoundsPlayed,
                    @RoundsWon,
                    UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE
                    infections = infections + VALUES(infections),
                    deaths = deaths + VALUES(deaths),
                    rounds_played = rounds_played + VALUES(rounds_played),
                    rounds_won = rounds_won + VALUES(rounds_won),
                    updated_utc = UTC_TIMESTAMP(6)
                """
        };
    }

    private static string GetEnsureCurrencyRowSql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                INSERT INTO {PlayerCurrencyTable} (steam_id, balance, lifetime_earned, lifetime_spent, updated_utc)
                VALUES (@SteamId, 0, 0, 0, CURRENT_TIMESTAMP)
                ON CONFLICT(steam_id) DO NOTHING
                """,
            _ => $"""
                INSERT INTO {PlayerCurrencyTable} (steam_id, balance, lifetime_earned, lifetime_spent, updated_utc)
                VALUES (@SteamId, 0, 0, 0, UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE updated_utc = updated_utc
                """
        };
    }

    private static string GetAddCurrencySql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                UPDATE {PlayerCurrencyTable}
                SET balance = balance + @Amount,
                    lifetime_earned = lifetime_earned + @Amount,
                    updated_utc = CURRENT_TIMESTAMP
                WHERE steam_id = @SteamId
                """,
            _ => $"""
                UPDATE {PlayerCurrencyTable}
                SET balance = balance + @Amount,
                    lifetime_earned = lifetime_earned + @Amount,
                    updated_utc = UTC_TIMESTAMP(6)
                WHERE steam_id = @SteamId
                """
        };
    }

    private static string GetSpendCurrencySql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                UPDATE {PlayerCurrencyTable}
                SET balance = balance - @Amount,
                    lifetime_spent = lifetime_spent + @Amount,
                    updated_utc = CURRENT_TIMESTAMP
                WHERE steam_id = @SteamId
                  AND balance >= @Amount
                """,
            _ => $"""
                UPDATE {PlayerCurrencyTable}
                SET balance = balance - @Amount,
                    lifetime_spent = lifetime_spent + @Amount,
                    updated_utc = UTC_TIMESTAMP(6)
                WHERE steam_id = @SteamId
                  AND balance >= @Amount
                """
        };
    }

    private static string GetInsertCurrencyLedgerSql(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.Sqlite => $"""
                INSERT INTO {PlayerCurrencyLedgerTable} (steam_id, reason, amount, created_utc)
                VALUES (@SteamId, @Reason, @Amount, CURRENT_TIMESTAMP)
                """,
            _ => $"""
                INSERT INTO {PlayerCurrencyLedgerTable} (steam_id, reason, amount, created_utc)
                VALUES (@SteamId, @Reason, @Amount, UTC_TIMESTAMP(6))
                """
        };
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength];
    }

    private static void ValidateScopedKey(string key, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Preference key is required.", parameterName);
        }

        foreach (var ch in key)
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '.' or '-'))
            {
                throw new ArgumentException("Preference key may only contain letters, digits, '_', '.', and '-'.", parameterName);
            }
        }
    }

    private static async Task OpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not System.Data.Common.DbConnection dbConnection)
        {
            connection.Open();
            return;
        }

        await dbConnection.OpenAsync(cancellationToken);
    }

    private DbConnection CreateDbConnection()
    {
        return (DbConnection)CreateConnection();
    }

    private static async Task EnsureCurrencyRowAsync(DbConnection connection, DbTransaction transaction, SqlDialect dialect, ulong steamId, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            GetEnsureCurrencyRowSql(dialect),
            new { SteamId = steamId },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task InsertCurrencyLedgerAsync(DbConnection connection, DbTransaction transaction, SqlDialect dialect, ulong steamId, int amount, string reason, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            GetInsertCurrencyLedgerSql(dialect),
            new { SteamId = steamId, Amount = amount, Reason = TrimOrNull(reason, 128) ?? "unknown" },
            transaction,
            cancellationToken: cancellationToken));
    }
}
