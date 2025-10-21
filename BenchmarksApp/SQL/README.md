# HyperLogLog SQL Implementations

This directory contains SQL implementations of the HyperLogLog probabilistic cardinality estimation algorithm for both T-SQL (SQL Server) and Snowflake.

## Overview

HyperLogLog is a probabilistic data structure used for estimating the cardinality (number of distinct elements) in a large dataset with minimal memory usage. These SQL implementations provide the same functionality as the C# implementation but can be used directly within your database.

## Files

- **HyperLogLog_TSQL.sql** - Implementation for Microsoft SQL Server (T-SQL)
- **HyperLogLog_Snowflake.sql** - Implementation for Snowflake Data Warehouse

## Features

- **Memory Efficient**: Uses minimal memory to estimate cardinality of millions of elements
- **Persistent Storage**: HyperLogLog state is stored in database tables for reuse
- **Mergeable**: Multiple HyperLogLog instances can be merged to combine estimates
- **Configurable Precision**: Adjustable precision parameter (4-16) to balance accuracy vs memory

## T-SQL Implementation (SQL Server)

### Installation

Run the `HyperLogLog_TSQL.sql` script in your SQL Server database to create:
- `HyperLogLog` table for storing state
- Functions: `ComputeHash`, `CountLeadingZeros`, `GetAlphaConstant`, `EstimateCardinality`
- Procedures: `InitializeHyperLogLog`, `AddToHyperLogLog`, `MergeHyperLogLog`

### Quick Start

```sql
-- 1. Initialize a new HyperLogLog with precision 12
EXEC dbo.InitializeHyperLogLog @HllId = 1, @Precision = 12;

-- 2. Add elements
EXEC dbo.AddToHyperLogLog @HllId = 1, @Element = 'user_12345';
EXEC dbo.AddToHyperLogLog @HllId = 1, @Element = 'user_67890';
-- Add more elements...

-- 3. Get cardinality estimate
SELECT dbo.EstimateCardinality(1) AS EstimatedUniqueUsers;
```

### Use Cases

#### 1. Count Unique Visitors
```sql
-- Initialize HyperLogLog
EXEC dbo.InitializeHyperLogLog @HllId = 100, @Precision = 14;

-- Add all visitors from your events table
INSERT INTO @visitors
SELECT DISTINCT visitor_id FROM page_views WHERE date = '2025-10-21';

DECLARE @visitor_id NVARCHAR(100);
DECLARE visitor_cursor CURSOR FOR SELECT visitor_id FROM @visitors;

OPEN visitor_cursor;
FETCH NEXT FROM visitor_cursor INTO @visitor_id;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.AddToHyperLogLog @HllId = 100, @Element = @visitor_id;
    FETCH NEXT FROM visitor_cursor INTO @visitor_id;
END;

CLOSE visitor_cursor;
DEALLOCATE visitor_cursor;

-- Get estimate
SELECT dbo.EstimateCardinality(100) AS UniqueVisitors;
```

#### 2. Merge Estimates from Different Time Periods
```sql
-- Create HyperLogLogs for two different weeks
EXEC dbo.InitializeHyperLogLog @HllId = 201, @Precision = 12;
EXEC dbo.InitializeHyperLogLog @HllId = 202, @Precision = 12;

-- Add week 1 users to HLL 201
-- Add week 2 users to HLL 202
-- ... (add logic here)

-- Get individual estimates
SELECT dbo.EstimateCardinality(201) AS Week1Users;
SELECT dbo.EstimateCardinality(202) AS Week2Users;

-- Merge to get combined unique users across both weeks
EXEC dbo.MergeHyperLogLog @TargetHllId = 201, @SourceHllId = 202;
SELECT dbo.EstimateCardinality(201) AS CombinedUniqueUsers;
```

### Performance Characteristics

| Precision | Buckets | Memory | Typical Error |
|-----------|---------|--------|---------------|
| 8         | 256     | 256 B  | ~5%           |
| 10        | 1,024   | 1 KB   | ~2.5%         |
| 12        | 4,096   | 4 KB   | ~1.6%         |
| 14        | 16,384  | 16 KB  | ~0.8%         |
| 16        | 65,536  | 64 KB  | ~0.4%         |

## Snowflake Implementation

### Installation

Run the `HyperLogLog_Snowflake.sql` script in your Snowflake database to create:
- `HyperLogLog` table for storing state
- JavaScript UDFs: `ComputeHash`, `CountLeadingZeros`, `GetAlphaConstant`, `EstimateCardinality`
- JavaScript Stored Procedures: `InitializeHyperLogLog`, `AddToHyperLogLog`, `MergeHyperLogLog`

### Quick Start

```sql
-- 1. Initialize a new HyperLogLog with precision 12
CALL InitializeHyperLogLog(1, 12);

-- 2. Add elements
CALL AddToHyperLogLog(1, 'user_12345');
CALL AddToHyperLogLog(1, 'user_67890');
-- Add more elements...

-- 3. Get cardinality estimate
SELECT EstimateCardinality(1) AS EstimatedUniqueUsers;
```

### Use Cases

#### 1. Count Unique Users with Snowflake Scripting
```sql
-- Initialize HyperLogLog
CALL InitializeHyperLogLog(100, 14);

-- Add all users from events table
DECLARE
    user_cursor CURSOR FOR SELECT DISTINCT user_id FROM user_events WHERE event_date = '2025-10-21';
BEGIN
    FOR user_record IN user_cursor DO
        CALL AddToHyperLogLog(100, user_record.user_id);
    END FOR;
    
    RETURN TABLE(SELECT EstimateCardinality(100) AS unique_users);
END;
```

#### 2. Compare Custom vs Native HyperLogLog
```sql
-- Snowflake has native APPROX_COUNT_DISTINCT
-- Compare with custom implementation

CALL InitializeHyperLogLog(200, 14);

-- Add data (in practice, use a loop or external application)
-- CALL AddToHyperLogLog(200, user_id) for each user

-- Compare results
SELECT 
    COUNT(DISTINCT user_id) AS exact_count,
    APPROX_COUNT_DISTINCT(user_id) AS native_hll,
    EstimateCardinality(200) AS custom_hll
FROM user_events;
```

#### 3. Partition-Based Estimation
```sql
-- Create separate HyperLogLogs for each partition
CALL InitializeHyperLogLog(301, 12); -- January
CALL InitializeHyperLogLog(302, 12); -- February
CALL InitializeHyperLogLog(303, 12); -- March

-- Add data to each partition
-- ... (add logic here)

-- Get per-month estimates
SELECT EstimateCardinality(301) AS january_users;
SELECT EstimateCardinality(302) AS february_users;
SELECT EstimateCardinality(303) AS march_users;

-- Merge all for Q1 total
CALL MergeHyperLogLog(301, 302);
CALL MergeHyperLogLog(301, 303);
SELECT EstimateCardinality(301) AS q1_total_users;
```

### When to Use Custom HyperLogLog vs Native APPROX_COUNT_DISTINCT

**Use Snowflake's native `APPROX_COUNT_DISTINCT()`** when:
- You need a one-time cardinality estimate in a query
- You don't need to persist the HyperLogLog state
- You don't need to merge estimates from different sources
- Maximum simplicity is desired

**Use the `APPROX_COUNT_DISTINCT_HLL` procedure** when:
- You want approximate distinct counts with custom HyperLogLog implementation
- You need a simple one-call interface without managing HyperLogLog IDs
- You want to compare results with the native function
- You need custom precision control

**Use the full custom HyperLogLog implementation** when:
- You need precise control over precision parameter
- You want to persist HyperLogLog state for incremental updates
- You need to merge estimates from different partitions or time periods
- You want to build complex workflows around cardinality estimation
- You need to reuse and update the same HyperLogLog over time

**T-SQL Considerations:**
- SQL Server 2019+ has a native `APPROX_COUNT_DISTINCT()` aggregate function
- Use `APPROX_COUNT_DISTINCT_EXEC` for a simplified custom HyperLogLog interface
- Use the full HyperLogLog procedures for persistent state and merging capabilities

## API Reference

### T-SQL API

#### InitializeHyperLogLog
```sql
EXEC dbo.InitializeHyperLogLog 
    @HllId INT,        -- Unique identifier for this HyperLogLog
    @Precision INT = 14 -- Precision (4-16), default 14
```

#### AddToHyperLogLog
```sql
EXEC dbo.AddToHyperLogLog 
    @HllId INT,               -- HyperLogLog identifier
    @Element NVARCHAR(MAX)    -- Element to add
```

#### EstimateCardinality
```sql
SELECT dbo.EstimateCardinality(@HllId INT) AS estimate
```

#### MergeHyperLogLog
```sql
EXEC dbo.MergeHyperLogLog 
    @TargetHllId INT,  -- Target HyperLogLog (will be modified)
    @SourceHllId INT   -- Source HyperLogLog (unchanged)
```

#### APPROX_COUNT_DISTINCT_EXEC
```sql
EXEC dbo.APPROX_COUNT_DISTINCT_EXEC
    @TableName NVARCHAR(256),     -- Table name
    @ColumnName NVARCHAR(256),    -- Column to count distinct values
    @WhereClause NVARCHAR(MAX) = NULL,  -- Optional WHERE clause (without WHERE keyword)
    @Precision INT = 14,          -- HyperLogLog precision (4-16)
    @Result BIGINT OUTPUT         -- Output parameter for the estimate
```

**Example:**
```sql
DECLARE @distinctCount BIGINT;
EXEC dbo.APPROX_COUNT_DISTINCT_EXEC 
    @TableName = 'Users',
    @ColumnName = 'UserId',
    @WhereClause = 'CreatedDate > ''2025-01-01''',
    @Precision = 12,
    @Result = @distinctCount OUTPUT;
SELECT @distinctCount AS ApproximateDistinctUsers;
```

### Snowflake API

#### InitializeHyperLogLog
```sql
CALL InitializeHyperLogLog(
    HllId INTEGER,      -- Unique identifier for this HyperLogLog
    Precision INTEGER   -- Precision (4-16)
)
```

#### AddToHyperLogLog
```sql
CALL AddToHyperLogLog(
    HllId INTEGER,     -- HyperLogLog identifier
    Element STRING     -- Element to add
)
```

#### EstimateCardinality
```sql
SELECT EstimateCardinality(HllId INTEGER) AS estimate
```

#### MergeHyperLogLog
```sql
CALL MergeHyperLogLog(
    TargetHllId INTEGER,  -- Target HyperLogLog (will be modified)
    SourceHllId INTEGER   -- Source HyperLogLog (unchanged)
)
```

#### APPROX_COUNT_DISTINCT_HLL
```sql
CALL APPROX_COUNT_DISTINCT_HLL(
    TableName STRING,       -- Table name
    ColumnName STRING,      -- Column to count distinct values
    WhereClause STRING,     -- Optional WHERE clause (without WHERE keyword, use NULL if not needed)
    Precision INTEGER       -- HyperLogLog precision (4-16)
)
RETURNS INTEGER;
```

**Example:**
```sql
-- Count distinct users
CALL APPROX_COUNT_DISTINCT_HLL('users', 'user_id', NULL, 12);

-- Count distinct users with filter
CALL APPROX_COUNT_DISTINCT_HLL(
    'users', 
    'user_id', 
    'created_date > ''2025-01-01''', 
    14
);
```

## Best Practices

1. **Choose Appropriate Precision**
   - Use precision 12 (4KB) for most use cases
   - Use precision 14 (16KB) when higher accuracy is needed
   - Higher precision = more memory but better accuracy

2. **Batch Operations**
   - Add multiple elements in a batch when possible
   - Use cursors or loops efficiently to minimize overhead

3. **Reuse HyperLogLog Instances**
   - Store HyperLogLog state in the database table
   - Update incrementally instead of rebuilding from scratch

4. **Partitioning Strategy**
   - Create separate HyperLogLogs for different time periods or categories
   - Merge them later to get combined estimates

5. **Memory Management**
   - Each HyperLogLog uses 2^precision bytes
   - Plan storage accordingly for large numbers of HyperLogLogs

## Limitations

1. **Accuracy**: Provides approximate counts with typical error rates of 0.4% - 5% depending on precision
2. **One-Way Operation**: Cannot remove elements once added
3. **Fixed Precision**: Cannot change precision after initialization
4. **Merge Requirements**: Can only merge HyperLogLogs with same precision

## Performance Comparison

### Memory Usage
For 1 million unique elements:
- Exact counting (HashSet): ~48 MB
- HyperLogLog (precision 14): 16 KB
- **Memory savings: 99.97%**

### Accuracy
With precision 14 on 1 million elements:
- Typical error: 0.5% - 1%
- Maximum error: ~2%

## References

- [Wikipedia - HyperLogLog](https://en.wikipedia.org/wiki/HyperLogLog)
- [Original Paper: Flajolet et al., 2007](http://algo.inria.fr/flajolet/Publications/FlFuGaMe07.pdf)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/t-sql/)
- [Snowflake JavaScript UDFs](https://docs.snowflake.com/en/sql-reference/udf-js.html)

## License

See the main repository LICENSE file for details.
