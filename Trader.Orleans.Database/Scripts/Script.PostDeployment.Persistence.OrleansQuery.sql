WITH Source AS
(
	SELECT * FROM (
	VALUES (
        'WriteToStorageKey',
        '-- When Orleans is running in normal, non-split state, there will
        -- be only one grain with the given ID and type combination only. This
        -- grain saves states mostly serially if Orleans guarantees are upheld. Even
        -- if not, the updates should work correctly due to version number.
        --
        -- In split brain situations there can be a situation where there are two or more
        -- grains with the given ID and type combination. When they try to INSERT
        -- concurrently, the table needs to be locked pessimistically before one of
        -- the grains gets @GrainStateVersion = 1 in return and the other grains will fail
        -- to update storage. The following arrangement is made to reduce locking in normal operation.
        --
        -- If the version number explicitly returned is still the same, Orleans interprets it so the update did not succeed
        -- and throws an InconsistentStateException.
        --
        -- See further information at https://dotnet.github.io/orleans/Documentation/Core-Features/Grain-Persistence.html.
        BEGIN TRANSACTION;
        SET XACT_ABORT, NOCOUNT ON;
        DECLARE @NewGrainStateVersion AS INT = @GrainStateVersion;
        -- If the @GrainStateVersion is not zero, this branch assumes it exists in this database.
        -- The NULL value is supplied by Orleans when the state is new.
        IF @GrainStateVersion IS NOT NULL
        BEGIN
            UPDATE OrleansStorage
            SET
                PayloadBinary = @PayloadBinary,
                PayloadJson = @PayloadJson,
                PayloadXml = @PayloadXml,
                ModifiedOn = GETUTCDATE(),
                Version = Version + 1,
                @NewGrainStateVersion = Version + 1,
                @GrainStateVersion = Version + 1
            WHERE
                GrainIdHash = @GrainIdHash AND @GrainIdHash IS NOT NULL
                AND GrainTypeHash = @GrainTypeHash AND @GrainTypeHash IS NOT NULL
                AND (GrainIdN0 = @GrainIdN0 OR @GrainIdN0 IS NULL)
                AND (GrainIdN1 = @GrainIdN1 OR @GrainIdN1 IS NULL)
                AND (GrainTypeString = @GrainTypeString OR @GrainTypeString IS NULL)
                AND ((@GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString = @GrainIdExtensionString) OR @GrainIdExtensionString IS NULL AND GrainIdExtensionString IS NULL)
                AND ServiceId = @ServiceId AND @ServiceId IS NOT NULL
                AND Version IS NOT NULL AND Version = @GrainStateVersion AND @GrainStateVersion IS NOT NULL
                OPTION(FAST 1, OPTIMIZE FOR(@GrainIdHash UNKNOWN, @GrainTypeHash UNKNOWN));
        END
        -- The grain state has not been read. The following locks rather pessimistically
        -- to ensure only one INSERT succeeds.
        IF @GrainStateVersion IS NULL
        BEGIN
            INSERT INTO OrleansStorage
            (
                GrainIdHash,
                GrainIdN0,
                GrainIdN1,
                GrainTypeHash,
                GrainTypeString,
                GrainIdExtensionString,
                ServiceId,
                PayloadBinary,
                PayloadJson,
                PayloadXml,
                ModifiedOn,
                Version
            )
            SELECT
                @GrainIdHash,
                @GrainIdN0,
                @GrainIdN1,
                @GrainTypeHash,
                @GrainTypeString,
                @GrainIdExtensionString,
                @ServiceId,
                @PayloadBinary,
                @PayloadJson,
                @PayloadXml,
                GETUTCDATE(),
                1
             WHERE NOT EXISTS
             (
                -- There should not be any version of this grain state.
                SELECT 1
                FROM OrleansStorage WITH(XLOCK, ROWLOCK, HOLDLOCK, INDEX(IX_OrleansStorage))
                WHERE
                    GrainIdHash = @GrainIdHash AND @GrainIdHash IS NOT NULL
                    AND GrainTypeHash = @GrainTypeHash AND @GrainTypeHash IS NOT NULL
                    AND (GrainIdN0 = @GrainIdN0 OR @GrainIdN0 IS NULL)
                    AND (GrainIdN1 = @GrainIdN1 OR @GrainIdN1 IS NULL)
                    AND (GrainTypeString = @GrainTypeString OR @GrainTypeString IS NULL)
                    AND ((@GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString = @GrainIdExtensionString) OR @GrainIdExtensionString IS NULL AND GrainIdExtensionString IS NULL)
                    AND ServiceId = @ServiceId AND @ServiceId IS NOT NULL
             ) OPTION(FAST 1, OPTIMIZE FOR(@GrainIdHash UNKNOWN, @GrainTypeHash UNKNOWN));
            IF @@ROWCOUNT > 0
            BEGIN
                SET @NewGrainStateVersion = 1;
            END
        END
        SELECT @NewGrainStateVersion AS NewGrainStateVersion;
        COMMIT TRANSACTION;
    '), (
        'ClearStorageKey',
        'BEGIN TRANSACTION;
        SET XACT_ABORT, NOCOUNT ON;
        DECLARE @NewGrainStateVersion AS INT = @GrainStateVersion;
        UPDATE OrleansStorage
        SET
            PayloadBinary = NULL,
            PayloadJson = NULL,
            PayloadXml = NULL,
            ModifiedOn = GETUTCDATE(),
            Version = Version + 1,
            @NewGrainStateVersion = Version + 1
        WHERE
            GrainIdHash = @GrainIdHash AND @GrainIdHash IS NOT NULL
            AND GrainTypeHash = @GrainTypeHash AND @GrainTypeHash IS NOT NULL
            AND (GrainIdN0 = @GrainIdN0 OR @GrainIdN0 IS NULL)
            AND (GrainIdN1 = @GrainIdN1 OR @GrainIdN1 IS NULL)
            AND (GrainTypeString = @GrainTypeString OR @GrainTypeString IS NULL)
            AND ((@GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString = @GrainIdExtensionString) OR @GrainIdExtensionString IS NULL AND GrainIdExtensionString IS NULL)
            AND ServiceId = @ServiceId AND @ServiceId IS NOT NULL
            AND Version IS NOT NULL AND Version = @GrainStateVersion AND @GrainStateVersion IS NOT NULL
            OPTION(FAST 1, OPTIMIZE FOR(@GrainIdHash UNKNOWN, @GrainTypeHash UNKNOWN));
        SELECT @NewGrainStateVersion;
        COMMIT TRANSACTION;'
    ), (
        'ReadFromStorageKey',
        '-- The application code will deserialize the relevant result. Not that the query optimizer
        -- estimates the result of rows based on its knowledge on the index. It does not know there
        -- will be only one row returned. Forcing the optimizer to process the first found row quickly
        -- creates an estimate for a one-row result and makes a difference on multi-million row tables.
        -- Also the optimizer is instructed to always use the same plan via index using the OPTIMIZE
        -- FOR UNKNOWN flags. These hints are only available in SQL Server 2008 and later. They
        -- should guarantee the execution time is robustly basically the same from query-to-query.
        SELECT
            PayloadBinary,
            PayloadXml,
            PayloadJson,
            Version
        FROM
            OrleansStorage
        WHERE
            GrainIdHash = @GrainIdHash AND @GrainIdHash IS NOT NULL
            AND GrainTypeHash = @GrainTypeHash AND @GrainTypeHash IS NOT NULL
            AND (GrainIdN0 = @GrainIdN0 OR @GrainIdN0 IS NULL)
            AND (GrainIdN1 = @GrainIdN1 OR @GrainIdN1 IS NULL)
            AND (GrainTypeString = @GrainTypeString OR @GrainTypeString IS NULL)
            AND ((@GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString IS NOT NULL AND GrainIdExtensionString = @GrainIdExtensionString) OR @GrainIdExtensionString IS NULL AND GrainIdExtensionString IS NULL)
            AND ServiceId = @ServiceId AND @ServiceId IS NOT NULL
            OPTION(FAST 1, OPTIMIZE FOR(@GrainIdHash UNKNOWN, @GrainTypeHash UNKNOWN));
    ')
	) AS T(QueryKey, QueryText)
)
MERGE INTO OrleansQuery AS T
USING Source AS S
ON S.QueryKey = T.QueryKey
WHEN NOT MATCHED THEN INSERT (QueryKey, QueryText) VALUES (QueryKey, QueryText)
WHEN MATCHED THEN UPDATE SET QueryText = S.QueryText
;
