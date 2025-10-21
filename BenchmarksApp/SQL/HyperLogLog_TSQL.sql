-- =============================================
-- HyperLogLog Implementation for T-SQL (SQL Server)
-- =============================================
-- This implementation provides a probabilistic cardinality estimation
-- algorithm using HyperLogLog for SQL Server databases.
-- Based on the algorithm described in https://en.wikipedia.org/wiki/HyperLogLog
-- =============================================

-- Create table to store HyperLogLog state
IF OBJECT_ID('dbo.HyperLogLog', 'U') IS NOT NULL
    DROP TABLE dbo.HyperLogLog;
GO

CREATE TABLE dbo.HyperLogLog (
    HllId INT PRIMARY KEY,
    Precision INT NOT NULL,
    Buckets VARBINARY(MAX) NOT NULL,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    LastUpdatedDate DATETIME2 DEFAULT GETUTCDATE()
);
GO

-- =============================================
-- Function: ComputeHash
-- Computes a 32-bit hash of the input string
-- =============================================
CREATE OR ALTER FUNCTION dbo.ComputeHash(@input NVARCHAR(MAX))
RETURNS INT
AS
BEGIN
    DECLARE @hash INT;
    -- Use HASHBYTES with MD5 and take first 4 bytes as INT
    SET @hash = CONVERT(INT, CONVERT(VARBINARY(4), SUBSTRING(HASHBYTES('MD5', @input), 1, 4)));
    RETURN @hash;
END;
GO

-- =============================================
-- Function: CountLeadingZeros
-- Counts the number of leading zero bits in a value
-- =============================================
CREATE OR ALTER FUNCTION dbo.CountLeadingZeros(@value BIGINT, @bitWidth INT)
RETURNS INT
AS
BEGIN
    IF @value = 0 RETURN @bitWidth;
    
    DECLARE @count INT = 0;
    DECLARE @normalizedValue BIGINT = @value;
    
    -- Normalize to bitWidth by shifting left
    SET @normalizedValue = @normalizedValue * POWER(CAST(2 AS BIGINT), 32 - @bitWidth);
    
    -- Count leading zeros using binary search
    IF (@normalizedValue & 0xFFFF0000) = 0 BEGIN SET @count = @count + 16; SET @normalizedValue = @normalizedValue * 65536; END
    IF (@normalizedValue & 0xFF000000) = 0 BEGIN SET @count = @count + 8; SET @normalizedValue = @normalizedValue * 256; END
    IF (@normalizedValue & 0xF0000000) = 0 BEGIN SET @count = @count + 4; SET @normalizedValue = @normalizedValue * 16; END
    IF (@normalizedValue & 0xC0000000) = 0 BEGIN SET @count = @count + 2; SET @normalizedValue = @normalizedValue * 4; END
    IF (@normalizedValue & 0x80000000) = 0 BEGIN SET @count = @count + 1; END
    
    RETURN @count;
END;
GO

-- =============================================
-- Function: GetAlphaConstant
-- Returns the alpha constant based on number of buckets
-- =============================================
CREATE OR ALTER FUNCTION dbo.GetAlphaConstant(@numberOfBuckets INT)
RETURNS FLOAT
AS
BEGIN
    DECLARE @alpha FLOAT;
    
    IF @numberOfBuckets = 16
        SET @alpha = 0.673;
    ELSE IF @numberOfBuckets = 32
        SET @alpha = 0.697;
    ELSE IF @numberOfBuckets = 64
        SET @alpha = 0.709;
    ELSE
        SET @alpha = 0.7213 / (1.0 + 1.079 / @numberOfBuckets);
    
    RETURN @alpha * @numberOfBuckets * @numberOfBuckets;
END;
GO

-- =============================================
-- Procedure: InitializeHyperLogLog
-- Creates a new HyperLogLog instance with specified precision
-- =============================================
CREATE OR ALTER PROCEDURE dbo.InitializeHyperLogLog
    @HllId INT,
    @Precision INT = 14
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate precision
    IF @Precision < 4 OR @Precision > 16
    BEGIN
        RAISERROR('Precision must be between 4 and 16', 16, 1);
        RETURN;
    END
    
    DECLARE @numberOfBuckets INT = POWER(2, @Precision);
    DECLARE @buckets VARBINARY(MAX) = CAST(REPLICATE(CAST(0x00 AS VARCHAR(1)), @numberOfBuckets) AS VARBINARY(MAX));
    
    -- Delete existing record if it exists
    DELETE FROM dbo.HyperLogLog WHERE HllId = @HllId;
    
    -- Insert new HyperLogLog
    INSERT INTO dbo.HyperLogLog (HllId, Precision, Buckets)
    VALUES (@HllId, @Precision, @buckets);
END;
GO

-- =============================================
-- Procedure: AddToHyperLogLog
-- Adds an element to the HyperLogLog
-- =============================================
CREATE OR ALTER PROCEDURE dbo.AddToHyperLogLog
    @HllId INT,
    @Element NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get HyperLogLog state
    DECLARE @precision INT, @buckets VARBINARY(MAX);
    SELECT @precision = Precision, @buckets = Buckets
    FROM dbo.HyperLogLog
    WHERE HllId = @HllId;
    
    IF @precision IS NULL
    BEGIN
        RAISERROR('HyperLogLog with specified ID not found', 16, 1);
        RETURN;
    END
    
    DECLARE @numberOfBuckets INT = POWER(2, @precision);
    
    -- Compute hash
    DECLARE @hash INT = dbo.ComputeHash(@Element);
    
    -- Get bucket index (first 'precision' bits)
    DECLARE @bucketIndex INT = @hash & (@numberOfBuckets - 1);
    
    -- Get remaining bits
    DECLARE @remainingBits BIGINT = @hash / POWER(2, @precision);
    
    -- Count leading zeros
    DECLARE @leadingZeros INT = dbo.CountLeadingZeros(@remainingBits, 32 - @precision) + 1;
    
    -- Get current bucket value
    DECLARE @currentValue TINYINT = CAST(SUBSTRING(@buckets, @bucketIndex + 1, 1) AS TINYINT);
    
    -- Update bucket if new value is larger
    IF @leadingZeros > @currentValue
    BEGIN
        -- Create new buckets binary
        DECLARE @newBuckets VARBINARY(MAX);
        
        IF @bucketIndex = 0
            SET @newBuckets = CAST(@leadingZeros AS BINARY(1)) + SUBSTRING(@buckets, 2, DATALENGTH(@buckets) - 1);
        ELSE IF @bucketIndex = @numberOfBuckets - 1
            SET @newBuckets = SUBSTRING(@buckets, 1, @bucketIndex) + CAST(@leadingZeros AS BINARY(1));
        ELSE
            SET @newBuckets = SUBSTRING(@buckets, 1, @bucketIndex) + 
                             CAST(@leadingZeros AS BINARY(1)) + 
                             SUBSTRING(@buckets, @bucketIndex + 2, DATALENGTH(@buckets) - @bucketIndex - 1);
        
        -- Update HyperLogLog
        UPDATE dbo.HyperLogLog
        SET Buckets = @newBuckets,
            LastUpdatedDate = GETUTCDATE()
        WHERE HllId = @HllId;
    END
END;
GO

-- =============================================
-- Function: EstimateCardinality
-- Estimates the cardinality (number of unique elements)
-- =============================================
CREATE OR ALTER FUNCTION dbo.EstimateCardinality(@HllId INT)
RETURNS BIGINT
AS
BEGIN
    DECLARE @estimate BIGINT = 0;
    
    -- Get HyperLogLog state
    DECLARE @precision INT, @buckets VARBINARY(MAX);
    SELECT @precision = Precision, @buckets = Buckets
    FROM dbo.HyperLogLog
    WHERE HllId = @HllId;
    
    IF @precision IS NULL
        RETURN 0;
    
    DECLARE @numberOfBuckets INT = POWER(2, @precision);
    DECLARE @alphaMM FLOAT = dbo.GetAlphaConstant(@numberOfBuckets);
    
    -- Calculate raw estimate (harmonic mean)
    DECLARE @harmonicSum FLOAT = 0;
    DECLARE @zeros INT = 0;
    DECLARE @i INT = 0;
    
    WHILE @i < @numberOfBuckets
    BEGIN
        DECLARE @bucketValue TINYINT = CAST(SUBSTRING(@buckets, @i + 1, 1) AS TINYINT);
        
        IF @bucketValue = 0
            SET @zeros = @zeros + 1;
        
        SET @harmonicSum = @harmonicSum + POWER(2.0, -@bucketValue);
        SET @i = @i + 1;
    END
    
    DECLARE @rawEstimate FLOAT = @alphaMM / @harmonicSum;
    
    -- Apply bias corrections
    IF @rawEstimate <= 2.5 * @numberOfBuckets
    BEGIN
        -- Small range correction
        IF @zeros != 0
            SET @rawEstimate = @numberOfBuckets * LOG(@numberOfBuckets * 1.0 / @zeros);
    END
    ELSE IF @rawEstimate > (1.0 / 30.0) * POWER(CAST(2 AS BIGINT), 32)
    BEGIN
        -- Large range correction
        SET @rawEstimate = -POWER(CAST(2 AS BIGINT), 32) * LOG(1 - @rawEstimate / POWER(CAST(2 AS BIGINT), 32));
    END
    
    SET @estimate = CAST(@rawEstimate AS BIGINT);
    
    RETURN @estimate;
END;
GO

-- =============================================
-- Procedure: MergeHyperLogLog
-- Merges one HyperLogLog into another
-- =============================================
CREATE OR ALTER PROCEDURE dbo.MergeHyperLogLog
    @TargetHllId INT,
    @SourceHllId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get both HyperLogLog states
    DECLARE @targetPrecision INT, @targetBuckets VARBINARY(MAX);
    DECLARE @sourcePrecision INT, @sourceBuckets VARBINARY(MAX);
    
    SELECT @targetPrecision = Precision, @targetBuckets = Buckets
    FROM dbo.HyperLogLog
    WHERE HllId = @TargetHllId;
    
    SELECT @sourcePrecision = Precision, @sourceBuckets = Buckets
    FROM dbo.HyperLogLog
    WHERE HllId = @SourceHllId;
    
    -- Validate
    IF @targetPrecision IS NULL OR @sourcePrecision IS NULL
    BEGIN
        RAISERROR('HyperLogLog with specified ID not found', 16, 1);
        RETURN;
    END
    
    IF @targetPrecision != @sourcePrecision
    BEGIN
        RAISERROR('Cannot merge HyperLogLogs with different precisions', 16, 1);
        RETURN;
    END
    
    DECLARE @numberOfBuckets INT = POWER(2, @targetPrecision);
    DECLARE @newBuckets VARBINARY(MAX) = 0x;
    DECLARE @i INT = 0;
    
    -- Merge buckets (take maximum of each bucket)
    WHILE @i < @numberOfBuckets
    BEGIN
        DECLARE @targetValue TINYINT = CAST(SUBSTRING(@targetBuckets, @i + 1, 1) AS TINYINT);
        DECLARE @sourceValue TINYINT = CAST(SUBSTRING(@sourceBuckets, @i + 1, 1) AS TINYINT);
        DECLARE @maxValue TINYINT = CASE WHEN @targetValue > @sourceValue THEN @targetValue ELSE @sourceValue END;
        
        SET @newBuckets = @newBuckets + CAST(@maxValue AS BINARY(1));
        SET @i = @i + 1;
    END
    
    -- Update target HyperLogLog
    UPDATE dbo.HyperLogLog
    SET Buckets = @newBuckets,
        LastUpdatedDate = GETUTCDATE()
    WHERE HllId = @TargetHllId;
END;
GO

-- =============================================
-- Function: APPROX_COUNT_DISTINCT
-- SQL Server-style approximate distinct count function
-- This provides a simpler interface similar to SQL Server's native APPROX_COUNT_DISTINCT
-- =============================================
CREATE OR ALTER FUNCTION dbo.APPROX_COUNT_DISTINCT(
    @TableName NVARCHAR(256),
    @ColumnName NVARCHAR(256),
    @WhereClause NVARCHAR(MAX) = NULL,
    @Precision INT = 14
)
RETURNS BIGINT
AS
BEGIN
    -- This is a wrapper function that demonstrates the concept
    -- In practice, you would use the HyperLogLog procedures directly
    -- or use SQL Server's native APPROX_COUNT_DISTINCT (available in SQL Server 2019+)
    
    -- For this implementation, we return a placeholder
    -- The proper usage is to create a HyperLogLog instance and add elements to it
    RETURN -1;
END;
GO

-- =============================================
-- Procedure: APPROX_COUNT_DISTINCT_EXEC
-- Executes approximate distinct count on a table column
-- This provides a SQL-like interface for approximate distinct counting
-- =============================================
CREATE OR ALTER PROCEDURE dbo.APPROX_COUNT_DISTINCT_EXEC
    @TableName NVARCHAR(256),
    @ColumnName NVARCHAR(256),
    @WhereClause NVARCHAR(MAX) = NULL,
    @Precision INT = 14,
    @Result BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Generate a unique HLL ID based on current timestamp
    DECLARE @HllId INT = ABS(CHECKSUM(NEWID())) % 2147483647;
    
    -- Initialize HyperLogLog
    EXEC dbo.InitializeHyperLogLog @HllId = @HllId, @Precision = @Precision;
    
    -- Build dynamic SQL to add all distinct values to HyperLogLog
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @whereClauseSQL NVARCHAR(MAX) = ISNULL(' WHERE ' + @WhereClause, '');
    
    SET @sql = N'
        DECLARE @value NVARCHAR(MAX);
        DECLARE value_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT CAST([' + @ColumnName + N'] AS NVARCHAR(MAX))
        FROM [' + @TableName + N']' + @whereClauseSQL + N';
        
        OPEN value_cursor;
        FETCH NEXT FROM value_cursor INTO @value;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC dbo.AddToHyperLogLog @HllId = ' + CAST(@HllId AS NVARCHAR(20)) + N', @Element = @value;
            FETCH NEXT FROM value_cursor INTO @value;
        END;
        
        CLOSE value_cursor;
        DEALLOCATE value_cursor;';
    
    EXEC sp_executesql @sql;
    
    -- Get the estimate
    SET @Result = dbo.EstimateCardinality(@HllId);
    
    -- Clean up temporary HyperLogLog
    DELETE FROM dbo.HyperLogLog WHERE HllId = @HllId;
END;
GO

-- =============================================
-- Example Usage and Tests
-- =============================================

-- Example 1: Basic cardinality estimation
PRINT '=== Example 1: Basic Cardinality Estimation ===';

-- Initialize HyperLogLog with precision 12
EXEC dbo.InitializeHyperLogLog @HllId = 1, @Precision = 12;

-- Add 10,000 unique elements
DECLARE @i INT = 0;
WHILE @i < 10000
BEGIN
    EXEC dbo.AddToHyperLogLog @HllId = 1, @Element = CONCAT('user_', @i);
    SET @i = @i + 1;
END

-- Estimate cardinality
DECLARE @estimate BIGINT = dbo.EstimateCardinality(1);
DECLARE @error FLOAT = ABS(10000.0 - @estimate) / 10000.0 * 100;
PRINT CONCAT('Actual count: 10,000');
PRINT CONCAT('Estimated count: ', @estimate);
PRINT CONCAT('Error: ', CAST(@error AS VARCHAR(10)), '%');
PRINT '';

-- Example 2: Duplicate handling
PRINT '=== Example 2: Duplicate Handling ===';

-- Initialize new HyperLogLog
EXEC dbo.InitializeHyperLogLog @HllId = 2, @Precision = 12;

-- Add 100,000 elements with only 1,000 unique values
SET @i = 0;
WHILE @i < 100000
BEGIN
    EXEC dbo.AddToHyperLogLog @HllId = 2, @Element = CONCAT('user_', @i % 1000);
    SET @i = @i + 1;
END

-- Estimate cardinality
SET @estimate = dbo.EstimateCardinality(2);
SET @error = ABS(1000.0 - @estimate) / 1000.0 * 100;
PRINT CONCAT('Actual unique count: 1,000');
PRINT CONCAT('Estimated count: ', @estimate);
PRINT CONCAT('Error: ', CAST(@error AS VARCHAR(10)), '%');
PRINT '';

-- Example 3: Merging HyperLogLogs
PRINT '=== Example 3: Merging HyperLogLogs ===';

-- Initialize two HyperLogLogs
EXEC dbo.InitializeHyperLogLog @HllId = 3, @Precision = 12;
EXEC dbo.InitializeHyperLogLog @HllId = 4, @Precision = 12;

-- Add different elements to each
SET @i = 0;
WHILE @i < 5000
BEGIN
    EXEC dbo.AddToHyperLogLog @HllId = 3, @Element = CONCAT('user_', @i);
    SET @i = @i + 1;
END

SET @i = 2500;
WHILE @i < 7500
BEGIN
    EXEC dbo.AddToHyperLogLog @HllId = 4, @Element = CONCAT('user_', @i);
    SET @i = @i + 1;
END

DECLARE @estimate1 BIGINT = dbo.EstimateCardinality(3);
DECLARE @estimate2 BIGINT = dbo.EstimateCardinality(4);
PRINT CONCAT('HLL1 estimate: ', @estimate1);
PRINT CONCAT('HLL2 estimate: ', @estimate2);

-- Merge HLL2 into HLL1
EXEC dbo.MergeHyperLogLog @TargetHllId = 3, @SourceHllId = 4;

DECLARE @mergedEstimate BIGINT = dbo.EstimateCardinality(3);
SET @error = ABS(7500.0 - @mergedEstimate) / 7500.0 * 100;
PRINT '';
PRINT 'After merge:';
PRINT CONCAT('Actual unique count: 7,500');
PRINT CONCAT('Estimated count: ', @mergedEstimate);
PRINT CONCAT('Error: ', CAST(@error AS VARCHAR(10)), '%');
GO

-- Example 4: Using APPROX_COUNT_DISTINCT_EXEC
PRINT '=== Example 4: APPROX_COUNT_DISTINCT_EXEC Usage ===';

-- Create a sample table with data
IF OBJECT_ID('tempdb..#SampleData', 'U') IS NOT NULL
    DROP TABLE #SampleData;

CREATE TABLE #SampleData (
    Id INT IDENTITY(1,1),
    UserId NVARCHAR(100),
    Category NVARCHAR(50),
    EventDate DATE
);

-- Insert sample data with duplicates
SET @i = 0;
WHILE @i < 10000
BEGIN
    INSERT INTO #SampleData (UserId, Category, EventDate)
    VALUES (
        CONCAT('user_', @i % 1000), -- 1000 unique users
        CASE @i % 3 WHEN 0 THEN 'A' WHEN 1 THEN 'B' ELSE 'C' END,
        DATEADD(DAY, @i % 30, '2025-01-01')
    );
    SET @i = @i + 1;
END

-- Use APPROX_COUNT_DISTINCT_EXEC to estimate unique users
DECLARE @approxCount BIGINT;
EXEC dbo.APPROX_COUNT_DISTINCT_EXEC 
    @TableName = N'#SampleData',
    @ColumnName = N'UserId',
    @WhereClause = NULL,
    @Precision = 12,
    @Result = @approxCount OUTPUT;

DECLARE @actualCount INT = (SELECT COUNT(DISTINCT UserId) FROM #SampleData);
SET @error = ABS(@actualCount * 1.0 - @approxCount) / (@actualCount * 1.0) * 100;

PRINT CONCAT('Actual unique users: ', @actualCount);
PRINT CONCAT('Estimated unique users: ', @approxCount);
PRINT CONCAT('Error: ', CAST(@error AS VARCHAR(10)), '%');
PRINT '';

-- Use with WHERE clause
EXEC dbo.APPROX_COUNT_DISTINCT_EXEC 
    @TableName = N'#SampleData',
    @ColumnName = N'UserId',
    @WhereClause = N'Category = ''A''',
    @Precision = 12,
    @Result = @approxCount OUTPUT;

SET @actualCount = (SELECT COUNT(DISTINCT UserId) FROM #SampleData WHERE Category = 'A');
SET @error = ABS(@actualCount * 1.0 - @approxCount) / (@actualCount * 1.0) * 100;

PRINT 'With WHERE clause (Category = ''A''):';
PRINT CONCAT('Actual unique users: ', @actualCount);
PRINT CONCAT('Estimated unique users: ', @approxCount);
PRINT CONCAT('Error: ', CAST(@error AS VARCHAR(10)), '%');

-- Clean up
DROP TABLE #SampleData;
GO
