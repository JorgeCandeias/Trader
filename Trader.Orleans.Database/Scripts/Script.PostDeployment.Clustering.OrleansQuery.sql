WITH Source AS
(
	SELECT * FROM (
	VALUES
	(
		'UpdateIAmAlivetimeKey','
		-- This is expected to never fail by Orleans, so return value
		-- is not needed nor is it checked.
		SET NOCOUNT ON;
		UPDATE OrleansMembershipTable
		SET
			IAmAliveTime = @IAmAliveTime
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Address = @Address AND @Address IS NOT NULL
			AND Port = @Port AND @Port IS NOT NULL
			AND Generation = @Generation AND @Generation IS NOT NULL;
	'),	(
		'InsertMembershipVersionKey','
		SET NOCOUNT ON;
		INSERT INTO OrleansMembershipVersionTable
		(
			DeploymentId
		)
		SELECT @DeploymentId
		WHERE NOT EXISTS
		(
			SELECT 1
			FROM
				OrleansMembershipVersionTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
			WHERE
				DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		);
		SELECT @@ROWCOUNT;
	'), (
		'InsertMembershipKey','
		SET XACT_ABORT, NOCOUNT ON;
		DECLARE @ROWCOUNT AS INT;
		BEGIN TRANSACTION;
		INSERT INTO OrleansMembershipTable
		(
			DeploymentId,
			Address,
			Port,
			Generation,
			SiloName,
			HostName,
			Status,
			ProxyPort,
			StartTime,
			IAmAliveTime
		)
		SELECT
			@DeploymentId,
			@Address,
			@Port,
			@Generation,
			@SiloName,
			@HostName,
			@Status,
			@ProxyPort,
			@StartTime,
			@IAmAliveTime
		WHERE NOT EXISTS
		(
			SELECT 1
			FROM
				OrleansMembershipTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
			WHERE
				DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
				AND Address = @Address AND @Address IS NOT NULL
				AND Port = @Port AND @Port IS NOT NULL
				AND Generation = @Generation AND @Generation IS NOT NULL
		);
		UPDATE OrleansMembershipVersionTable
		SET
			Timestamp = GETUTCDATE(),
			Version = Version + 1
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Version = @Version AND @Version IS NOT NULL
			AND @@ROWCOUNT > 0;
		SET @ROWCOUNT = @@ROWCOUNT;
		IF @ROWCOUNT = 0
			ROLLBACK TRANSACTION
		ELSE
			COMMIT TRANSACTION
		SELECT @ROWCOUNT;
	'), (
		'UpdateMembershipKey','
		SET XACT_ABORT, NOCOUNT ON;
		BEGIN TRANSACTION;
		UPDATE OrleansMembershipVersionTable
		SET
			Timestamp = GETUTCDATE(),
			Version = Version + 1
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Version = @Version AND @Version IS NOT NULL;
		UPDATE OrleansMembershipTable
		SET
			Status = @Status,
			SuspectTimes = @SuspectTimes,
			IAmAliveTime = @IAmAliveTime
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Address = @Address AND @Address IS NOT NULL
			AND Port = @Port AND @Port IS NOT NULL
			AND Generation = @Generation AND @Generation IS NOT NULL
			AND @@ROWCOUNT > 0;
		SELECT @@ROWCOUNT;
		COMMIT TRANSACTION;
	'), (
		'GatewaysQueryKey','
		SELECT
			Address,
			ProxyPort,
			Generation
		FROM
			OrleansMembershipTable
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Status = @Status AND @Status IS NOT NULL
			AND ProxyPort > 0;
	'), (
		'MembershipReadRowKey','
		SELECT
			v.DeploymentId,
			m.Address,
			m.Port,
			m.Generation,
			m.SiloName,
			m.HostName,
			m.Status,
			m.ProxyPort,
			m.SuspectTimes,
			m.StartTime,
			m.IAmAliveTime,
			v.Version
		FROM
			OrleansMembershipVersionTable v
			-- This ensures the version table will returned even if there is no matching membership row.
			LEFT OUTER JOIN OrleansMembershipTable m ON v.DeploymentId = m.DeploymentId
			AND Address = @Address AND @Address IS NOT NULL
			AND Port = @Port AND @Port IS NOT NULL
			AND Generation = @Generation AND @Generation IS NOT NULL
		WHERE
			v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	'), (
		'MembershipReadAllKey','
		SELECT
			v.DeploymentId,
			m.Address,
			m.Port,
			m.Generation,
			m.SiloName,
			m.HostName,
			m.Status,
			m.ProxyPort,
			m.SuspectTimes,
			m.StartTime,
			m.IAmAliveTime,
			v.Version
		FROM
			OrleansMembershipVersionTable v LEFT OUTER JOIN OrleansMembershipTable m
			ON v.DeploymentId = m.DeploymentId
		WHERE
			v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	'), (
		'DeleteMembershipTableEntriesKey','
		DELETE FROM OrleansMembershipTable
		WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
		DELETE FROM OrleansMembershipVersionTable
		WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	')
	) AS T(QueryKey, QueryText)
)
MERGE INTO OrleansQuery AS T
USING Source AS S
ON S.QueryKey = T.QueryKey
WHEN NOT MATCHED THEN INSERT (QueryKey, QueryText) VALUES (QueryKey, QueryText)
WHEN MATCHED THEN UPDATE SET QueryText = S.QueryText
;
