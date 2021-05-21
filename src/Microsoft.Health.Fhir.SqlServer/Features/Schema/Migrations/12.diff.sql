IF TYPE_ID(N'IndexTableType_1') IS NULL
BEGIN
CREATE TYPE dbo.IndexTableType_1 AS TABLE
(
    TableName nvarchar(128) COLLATE Latin1_General_CI_AI NOT NULL,
    IndexName nvarchar(128) COLLATE Latin1_General_CI_AI NOT NULL
)
END

GO

/*************************************************************
    Stored procedures for delete batch resources
**************************************************************/
--
-- STORED PROCEDURE
--     DeleteBatchResources
--
-- DESCRIPTION
--     Delete batch resources
--
-- PARAMETERS
--     @resourceTypeId
--         * The resoruce type id
--     @startResourceSurrogateId
--         * The start ResourceSurrogateId
--     @endResourceSurrogateId
--         * The end ResourceSurrogateId
CREATE OR ALTER PROCEDURE dbo.DeleteBatchResources
    @resourceTypeId smallint,
    @startResourceSurrogateId bigint,
    @endResourceSurrogateId bigint
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    DELETE FROM dbo.Resource
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.ResourceWriteClaim
    WHERE ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.CompartmentAssignment
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.ReferenceSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenText
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.StringSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.UriSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.NumberSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.QuantitySearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.DateTimeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.ReferenceTokenCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenTokenCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenDateTimeCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenQuantityCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenStringCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId

    DELETE FROM dbo.TokenNumberNumberCompositeSearchParam
    WHERE ResourceTypeId = @resourceTypeId AND ResourceSurrogateId >= @startResourceSurrogateId AND ResourceSurrogateId < @endResourceSurrogateId
        
    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for disable indexes
**************************************************************/
--
-- STORED PROCEDURE
--     DisableIndexes
--
-- DESCRIPTION
--     Stored procedures for disable indexes
--
-- PARAMETERS
--     @indexes indexes table
CREATE OR ALTER PROCEDURE [dbo].[DisableIndexes]
    @indexes dbo.IndexTableType_1 READONLY
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    declare commands cursor for
    SELECT N'ALTER INDEX [' + indexes.IndexName + '] ON ' + indexes.TableName + ' Disable;'
    FROM @indexes as indexes

    declare @cmd varchar(max)

    open commands
    fetch next from commands into @cmd
    while @@FETCH_STATUS=0
    begin
      exec(@cmd)
      fetch next from commands into @cmd
    end

    select indexName, tableName from @indexes

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for rebuild indexes
**************************************************************/
--
-- STORED PROCEDURE
--     RebuildIndexes
--
-- DESCRIPTION
--     Stored procedures for rebuild indexes
--
-- PARAMETERS
--     @indexes indexes table
CREATE OR ALTER PROCEDURE [dbo].[RebuildIndexes]
    @indexes dbo.IndexTableType_1 READONLY
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    declare commands cursor for
    SELECT N'ALTER INDEX [' + indexes.IndexName + '] ON ' + indexes.TableName + ' Rebuild;'
    FROM @indexes as indexes

    declare @cmd varchar(max)

    open commands
    fetch next from commands into @cmd
    while @@FETCH_STATUS=0
    begin
      exec(@cmd)
      fetch next from commands into @cmd
    end

    select indexName, tableName from @indexes

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for remove duplicate resources
**************************************************************/
--
-- STORED PROCEDURE
--     DeleteDuplicatedResources
--
-- DESCRIPTION
--     Delete duplicated resources
--
-- PARAMETERS
--
CREATE OR ALTER PROCEDURE dbo.DeleteDuplicatedResources
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

    DELETE rank FROM
		(
			SELECT *
			, DupRank = ROW_NUMBER() OVER (
						  PARTITION BY ResourceId
						  ORDER BY ResourceSurrogateId desc)
			From dbo.Resource
		) as rank
    where rank.DupRank > 1

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for remove duplicate search parameters
**************************************************************/
--
-- STORED PROCEDURE
--     DeleteDuplicatedSearchParams
--
-- DESCRIPTION
--     Delete duplicated search parameters
--
-- PARAMETERS
--     @tableName search params table name
CREATE OR ALTER PROCEDURE dbo.DeleteDuplicatedSearchParams
    @tableName nvarchar(128)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
    BEGIN TRANSACTION

	DECLARE @Sql NVARCHAR(MAX);

	SET @Sql = N'DELETE FROM ' + @TableName
	+ N' WHERE ResourceSurrogateId not IN (SELECT ResourceSurrogateId FROM dbo.Resource)'

	EXECUTE sp_executesql @Sql

    COMMIT TRANSACTION
GO

/*************************************************************
    Stored procedures for general task
**************************************************************/
--
-- STORED PROCEDURE
--     CreateTask_2
--
-- DESCRIPTION
--     Create task for given task payload.
--
-- PARAMETERS
--     @taskId
--         * The ID of the task record to create
--     @queueId
--         * The number of seconds that must pass before an export job is considered stale
--     @taskTypeId
--         * The maximum number of running jobs we can have at once
--     @maxRetryCount
--         * The maximum number for retry operation
--     @inputData
--         * Input data payload for the task
--     @isUniqueTaskByType
--         * Only create task if there's no other active task with same task type id
--
CREATE OR ALTER PROCEDURE [dbo].[CreateTask_2]
    @taskId varchar(64),
    @queueId varchar(64),
	@taskTypeId smallint,
    @maxRetryCount smallint = 3,
    @inputData varchar(max),
    @isUniqueTaskByType bit
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @heartbeatDateTime datetime2(7) = SYSUTCDATETIME()
	DECLARE @status smallint = 1
    DECLARE @retryCount smallint = 0
	DECLARE @isCanceled bit = 0

    -- Check if the task already be created
    IF (@isUniqueTaskByType = 1) BEGIN
        IF EXISTS
        (
            SELECT *
            FROM [dbo].[TaskInfo]
            WHERE TaskId = @taskId or (TaskTypeId = @taskTypeId and (Status <> 3 and IsCanceled = 0))
        ) 
        BEGIN
            THROW 50409, 'Task already existed', 1;
        END
    END 
    ELSE BEGIN
        IF EXISTS
        (
            SELECT *
            FROM [dbo].[TaskInfo]
            WHERE TaskId = @taskId
        ) 
        BEGIN
            THROW 50409, 'Task already existed', 1;
        END
    END

    -- Create new task
    INSERT INTO [dbo].[TaskInfo]
        (TaskId, QueueId, Status, TaskTypeId, IsCanceled, RetryCount, MaxRetryCount, HeartbeatDateTime, InputData)
    VALUES
        (@taskId, @queueId, @status, @taskTypeId, @isCanceled, @retryCount, @maxRetryCount, @heartbeatDateTime, @inputData)

    SELECT TaskId, QueueId, Status, TaskTypeId, RunId, IsCanceled, RetryCount, MaxRetryCount, HeartbeatDateTime, InputData
	FROM [dbo].[TaskInfo]
	where TaskId = @taskId

    COMMIT TRANSACTION
GO
