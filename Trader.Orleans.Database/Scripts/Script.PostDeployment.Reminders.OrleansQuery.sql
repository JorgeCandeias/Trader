WITH Source AS
(
	SELECT * FROM (
	VALUES (
		'UpsertReminderRowKey','
		DECLARE @Version AS INT = 0;
		SET XACT_ABORT, NOCOUNT ON;
		BEGIN TRANSACTION;
		UPDATE OrleansRemindersTable WITH(UPDLOCK, ROWLOCK, HOLDLOCK)
		SET
			StartTime = @StartTime,
			Period = @Period,
			GrainHash = @GrainHash,
			@Version = Version = Version + 1
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND GrainId = @GrainId AND @GrainId IS NOT NULL
			AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL;
		INSERT INTO OrleansRemindersTable
		(
			ServiceId,
			GrainId,
			ReminderName,
			StartTime,
			Period,
			GrainHash,
			Version
		)
		SELECT
			@ServiceId,
			@GrainId,
			@ReminderName,
			@StartTime,
			@Period,
			@GrainHash,
			0
		WHERE
			@@ROWCOUNT=0;
		SELECT @Version AS Version;
		COMMIT TRANSACTION;
	'), (
		'ReadReminderRowsKey','
		SELECT
			GrainId,
			ReminderName,
			StartTime,
			Period,
			Version
		FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND GrainId = @GrainId AND @GrainId IS NOT NULL;
	'), (
		'ReadReminderRowKey','
		SELECT
			GrainId,
			ReminderName,
			StartTime,
			Period,
			Version
		FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND GrainId = @GrainId AND @GrainId IS NOT NULL
			AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL;
	'), (
		'ReadRangeRows1Key','
		SELECT
			GrainId,
			ReminderName,
			StartTime,
			Period,
			Version
		FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND GrainHash > @BeginHash AND @BeginHash IS NOT NULL
			AND GrainHash <= @EndHash AND @EndHash IS NOT NULL;
	'), (
		'ReadRangeRows2Key','
		SELECT
			GrainId,
			ReminderName,
			StartTime,
			Period,
			Version
		FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND ((GrainHash > @BeginHash AND @BeginHash IS NOT NULL)
			OR (GrainHash <= @EndHash AND @EndHash IS NOT NULL));
	'), (
		'DeleteReminderRowKey','
		DELETE FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL
			AND GrainId = @GrainId AND @GrainId IS NOT NULL
			AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL
			AND Version = @Version AND @Version IS NOT NULL;
		SELECT @@ROWCOUNT;
	'), (
		'DeleteReminderRowsKey','
		DELETE FROM OrleansRemindersTable
		WHERE
			ServiceId = @ServiceId AND @ServiceId IS NOT NULL;
	')
	) AS T(QueryKey, QueryText)
)
MERGE INTO OrleansQuery AS T
USING Source AS S
ON S.QueryKey = T.QueryKey
WHEN NOT MATCHED THEN INSERT (QueryKey, QueryText) VALUES (QueryKey, QueryText)
WHEN MATCHED THEN UPDATE SET QueryText = S.QueryText
;
