-- For each deployment, there will be only one (active) membership version table version column which will be updated periodically.
CREATE TABLE OrleansMembershipVersionTable
(
	DeploymentId NVARCHAR(150) NOT NULL,
	Timestamp DATETIME2(3) NOT NULL DEFAULT GETUTCDATE(),
	Version INT NOT NULL DEFAULT 0,

	CONSTRAINT PK_OrleansMembershipVersionTable_DeploymentId PRIMARY KEY
	(
		DeploymentId
	)
);