-- Orleans Reminders table - https://dotnet.github.io/orleans/Documentation/Core-Features/Timers-and-Reminders.html
CREATE TABLE OrleansRemindersTable
(
	ServiceId NVARCHAR(150) NOT NULL,
	GrainId VARCHAR(150) NOT NULL,
	ReminderName NVARCHAR(150) NOT NULL,
	StartTime DATETIME2(3) NOT NULL,
	Period BIGINT NOT NULL,
	GrainHash INT NOT NULL,
	Version INT NOT NULL,

	CONSTRAINT PK_RemindersTable_ServiceId_GrainId_ReminderName PRIMARY KEY
	(
		ServiceId,
		GrainId,
		ReminderName
	)
);