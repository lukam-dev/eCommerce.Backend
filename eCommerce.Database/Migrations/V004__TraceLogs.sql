SET XACT_ABORT ON;

CREATE TABLE dbo.TraceLogs (
    Id bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TraceLogs PRIMARY KEY,
    TraceId nvarchar(128) NOT NULL,
    Method nvarchar(32) NOT NULL,
    RouteTemplate nvarchar(256) NULL,
    StatusCode int NOT NULL,
    StartedAt datetimeoffset NOT NULL,
    DurationMs bigint NOT NULL
);

CREATE INDEX IX_TraceLogs_TraceId ON dbo.TraceLogs(TraceId);
CREATE INDEX IX_TraceLogs_StartedAt ON dbo.TraceLogs(StartedAt);

CREATE TABLE dbo.TraceLogErrors (
    TraceLogId bigint NOT NULL CONSTRAINT PK_TraceLogErrors PRIMARY KEY,
    ExceptionType nvarchar(256) NULL,
    CONSTRAINT FK_TraceLogErrors_TraceLogs FOREIGN KEY (TraceLogId)
        REFERENCES dbo.TraceLogs(Id) ON DELETE CASCADE
);
