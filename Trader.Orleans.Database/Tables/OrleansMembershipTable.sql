-- Every silo instance has a row in the membership table.
CREATE TABLE OrleansMembershipTable
(
	DeploymentId NVARCHAR(150) NOT NULL,
	Address VARCHAR(45) NOT NULL,
	Port INT NOT NULL,
	Generation INT NOT NULL,
	SiloName NVARCHAR(150) NOT NULL,
	HostName NVARCHAR(150) NOT NULL,
	Status INT NOT NULL,
	ProxyPort INT NULL,
	SuspectTimes VARCHAR(8000) NULL,
	StartTime DATETIME2(3) NOT NULL,
	IAmAliveTime DATETIME2(3) NOT NULL,

	CONSTRAINT PK_MembershipTable_DeploymentId PRIMARY KEY
	(
		DeploymentId,
		Address,
		Port,
		Generation
	),

	CONSTRAINT FK_MembershipTable_MembershipVersionTable_DeploymentId FOREIGN KEY
	(
		DeploymentId
	)
	REFERENCES OrleansMembershipVersionTable
	(
		DeploymentId
	)
);