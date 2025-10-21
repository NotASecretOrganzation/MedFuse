-- =============================================
-- HyperLogLog Implementation for Snowflake
-- =============================================
-- This implementation provides a probabilistic cardinality estimation
-- algorithm using HyperLogLog for Snowflake databases.
-- Based on the algorithm described in https://en.wikipedia.org/wiki/HyperLogLog
-- 
-- Note: Snowflake has a native APPROX_COUNT_DISTINCT() function that uses HyperLogLog,
-- but this implementation demonstrates how to build a custom HyperLogLog from scratch
-- with full control over precision and merging capabilities.
-- =============================================

-- Create table to store HyperLogLog state
CREATE OR REPLACE TABLE HyperLogLog (
    HllId INTEGER PRIMARY KEY,
    Precision INTEGER NOT NULL,
    Buckets BINARY NOT NULL,
    CreatedDate TIMESTAMP_NTZ DEFAULT CURRENT_TIMESTAMP(),
    LastUpdatedDate TIMESTAMP_NTZ DEFAULT CURRENT_TIMESTAMP()
);

-- =============================================
-- Function: ComputeHash
-- Computes a 32-bit hash of the input string
-- =============================================
CREATE OR REPLACE FUNCTION ComputeHash(input STRING)
RETURNS INTEGER
LANGUAGE JAVASCRIPT
AS
$$
    // Simple hash function (similar to Java's hashCode)
    var hash = 0;
    if (input.length === 0) return hash;
    
    for (var i = 0; i < input.length; i++) {
        var char = input.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash; // Convert to 32-bit integer
    }
    
    return Math.abs(hash);
$$;

-- =============================================
-- Function: CountLeadingZeros
-- Counts the number of leading zero bits in a value
-- =============================================
CREATE OR REPLACE FUNCTION CountLeadingZeros(value INTEGER, bitWidth INTEGER)
RETURNS INTEGER
LANGUAGE JAVASCRIPT
AS
$$
    if (VALUE === 0) return BITWIDTH;
    
    // Normalize to bitWidth
    var normalizedValue = VALUE << (32 - BITWIDTH);
    
    var count = 0;
    if ((normalizedValue & 0xFFFF0000) === 0) { count += 16; normalizedValue <<= 16; }
    if ((normalizedValue & 0xFF000000) === 0) { count += 8; normalizedValue <<= 8; }
    if ((normalizedValue & 0xF0000000) === 0) { count += 4; normalizedValue <<= 4; }
    if ((normalizedValue & 0xC0000000) === 0) { count += 2; normalizedValue <<= 2; }
    if ((normalizedValue & 0x80000000) === 0) { count += 1; }
    
    return count;
$$;

-- =============================================
-- Function: GetAlphaConstant
-- Returns the alpha constant based on number of buckets
-- =============================================
CREATE OR REPLACE FUNCTION GetAlphaConstant(numberOfBuckets INTEGER)
RETURNS FLOAT
LANGUAGE JAVASCRIPT
AS
$$
    var alpha;
    
    if (NUMBEROFBUCKETS === 16) {
        alpha = 0.673;
    } else if (NUMBEROFBUCKETS === 32) {
        alpha = 0.697;
    } else if (NUMBEROFBUCKETS === 64) {
        alpha = 0.709;
    } else {
        alpha = 0.7213 / (1.0 + 1.079 / NUMBEROFBUCKETS);
    }
    
    return alpha * NUMBEROFBUCKETS * NUMBEROFBUCKETS;
$$;

-- =============================================
-- Procedure: InitializeHyperLogLog
-- Creates a new HyperLogLog instance with specified precision
-- =============================================
CREATE OR REPLACE PROCEDURE InitializeHyperLogLog(HllId INTEGER, Precision INTEGER)
RETURNS STRING
LANGUAGE JAVASCRIPT
AS
$$
    // Validate precision
    if (PRECISION < 4 || PRECISION > 16) {
        throw new Error('Precision must be between 4 and 16');
    }
    
    var numberOfBuckets = Math.pow(2, PRECISION);
    
    // Create buckets as array of zeros
    var buckets = new Uint8Array(numberOfBuckets);
    
    // Convert to hex string for storage
    var hexString = Array.from(buckets, b => b.toString(16).padStart(2, '0')).join('');
    
    // Delete existing record if it exists
    snowflake.execute({
        sqlText: "DELETE FROM HyperLogLog WHERE HllId = ?",
        binds: [HLLID]
    });
    
    // Insert new HyperLogLog
    snowflake.execute({
        sqlText: "INSERT INTO HyperLogLog (HllId, Precision, Buckets) VALUES (?, ?, TO_BINARY(?, 'HEX'))",
        binds: [HLLID, PRECISION, hexString]
    });
    
    return 'HyperLogLog initialized with ID: ' + HLLID + ', Precision: ' + PRECISION;
$$;

-- =============================================
-- Procedure: AddToHyperLogLog
-- Adds an element to the HyperLogLog
-- =============================================
CREATE OR REPLACE PROCEDURE AddToHyperLogLog(HllId INTEGER, Element STRING)
RETURNS STRING
LANGUAGE JAVASCRIPT
AS
$$
    // Get HyperLogLog state
    var result = snowflake.execute({
        sqlText: "SELECT Precision, TO_VARCHAR(Buckets, 'HEX') as BucketsHex FROM HyperLogLog WHERE HllId = ?",
        binds: [HLLID]
    });
    
    if (!result.next()) {
        throw new Error('HyperLogLog with specified ID not found');
    }
    
    var precision = result.getColumnValue(1);
    var bucketsHex = result.getColumnValue(2);
    
    // Convert hex string to byte array
    var buckets = new Uint8Array(bucketsHex.match(/.{2}/g).map(byte => parseInt(byte, 16)));
    var numberOfBuckets = Math.pow(2, precision);
    
    // Compute hash using the ComputeHash function
    var hashResult = snowflake.execute({
        sqlText: "SELECT ComputeHash(?)",
        binds: [ELEMENT]
    });
    hashResult.next();
    var hash = hashResult.getColumnValue(1);
    
    // Get bucket index (first 'precision' bits)
    var bucketIndex = hash & (numberOfBuckets - 1);
    
    // Get remaining bits
    var remainingBits = Math.floor(hash / Math.pow(2, precision));
    
    // Count leading zeros using the CountLeadingZeros function
    var lzResult = snowflake.execute({
        sqlText: "SELECT CountLeadingZeros(?, ?)",
        binds: [remainingBits, 32 - precision]
    });
    lzResult.next();
    var leadingZeros = lzResult.getColumnValue(1) + 1;
    
    // Get current bucket value
    var currentValue = buckets[bucketIndex];
    
    // Update bucket if new value is larger
    if (leadingZeros > currentValue) {
        buckets[bucketIndex] = leadingZeros;
        
        // Convert back to hex string
        var newHexString = Array.from(buckets, b => b.toString(16).padStart(2, '0')).join('');
        
        // Update HyperLogLog
        snowflake.execute({
            sqlText: "UPDATE HyperLogLog SET Buckets = TO_BINARY(?, 'HEX'), LastUpdatedDate = CURRENT_TIMESTAMP() WHERE HllId = ?",
            binds: [newHexString, HLLID]
        });
        
        return 'Element added to HyperLogLog';
    }
    
    return 'Element already accounted for';
$$;

-- =============================================
-- Function: EstimateCardinality
-- Estimates the cardinality (number of unique elements)
-- =============================================
CREATE OR REPLACE FUNCTION EstimateCardinality(HllId INTEGER)
RETURNS INTEGER
LANGUAGE JAVASCRIPT
AS
$$
    // Get HyperLogLog state
    var result = snowflake.execute({
        sqlText: "SELECT Precision, TO_VARCHAR(Buckets, 'HEX') as BucketsHex FROM HyperLogLog WHERE HllId = ?",
        binds: [HLLID]
    });
    
    if (!result.next()) {
        return 0;
    }
    
    var precision = result.getColumnValue(1);
    var bucketsHex = result.getColumnValue(2);
    
    // Convert hex string to byte array
    var buckets = new Uint8Array(bucketsHex.match(/.{2}/g).map(byte => parseInt(byte, 16)));
    var numberOfBuckets = Math.pow(2, precision);
    
    // Get alpha constant using the GetAlphaConstant function
    var alphaResult = snowflake.execute({
        sqlText: "SELECT GetAlphaConstant(?)",
        binds: [numberOfBuckets]
    });
    alphaResult.next();
    var alphaMM = alphaResult.getColumnValue(1);
    
    // Calculate raw estimate (harmonic mean)
    var harmonicSum = 0;
    var zeros = 0;
    
    for (var i = 0; i < numberOfBuckets; i++) {
        var bucketValue = buckets[i];
        
        if (bucketValue === 0) {
            zeros++;
        }
        
        harmonicSum += Math.pow(2, -bucketValue);
    }
    
    var rawEstimate = alphaMM / harmonicSum;
    
    // Apply bias corrections
    if (rawEstimate <= 2.5 * numberOfBuckets) {
        // Small range correction
        if (zeros !== 0) {
            rawEstimate = numberOfBuckets * Math.log(numberOfBuckets / zeros);
        }
    } else if (rawEstimate > (1.0 / 30.0) * Math.pow(2, 32)) {
        // Large range correction
        rawEstimate = -Math.pow(2, 32) * Math.log(1 - rawEstimate / Math.pow(2, 32));
    }
    
    return Math.round(rawEstimate);
$$;

-- =============================================
-- Procedure: MergeHyperLogLog
-- Merges one HyperLogLog into another
-- =============================================
CREATE OR REPLACE PROCEDURE MergeHyperLogLog(TargetHllId INTEGER, SourceHllId INTEGER)
RETURNS STRING
LANGUAGE JAVASCRIPT
AS
$$
    // Get both HyperLogLog states
    var targetResult = snowflake.execute({
        sqlText: "SELECT Precision, TO_VARCHAR(Buckets, 'HEX') as BucketsHex FROM HyperLogLog WHERE HllId = ?",
        binds: [TARGETHLLID]
    });
    
    var sourceResult = snowflake.execute({
        sqlText: "SELECT Precision, TO_VARCHAR(Buckets, 'HEX') as BucketsHex FROM HyperLogLog WHERE HllId = ?",
        binds: [SOURCEHLLID]
    });
    
    if (!targetResult.next() || !sourceResult.next()) {
        throw new Error('HyperLogLog with specified ID not found');
    }
    
    var targetPrecision = targetResult.getColumnValue(1);
    var targetBucketsHex = targetResult.getColumnValue(2);
    var sourcePrecision = sourceResult.getColumnValue(1);
    var sourceBucketsHex = sourceResult.getColumnValue(2);
    
    // Validate
    if (targetPrecision !== sourcePrecision) {
        throw new Error('Cannot merge HyperLogLogs with different precisions');
    }
    
    // Convert hex strings to byte arrays
    var targetBuckets = new Uint8Array(targetBucketsHex.match(/.{2}/g).map(byte => parseInt(byte, 16)));
    var sourceBuckets = new Uint8Array(sourceBucketsHex.match(/.{2}/g).map(byte => parseInt(byte, 16)));
    var numberOfBuckets = Math.pow(2, targetPrecision);
    
    // Merge buckets (take maximum of each bucket)
    var newBuckets = new Uint8Array(numberOfBuckets);
    for (var i = 0; i < numberOfBuckets; i++) {
        newBuckets[i] = Math.max(targetBuckets[i], sourceBuckets[i]);
    }
    
    // Convert to hex string
    var newHexString = Array.from(newBuckets, b => b.toString(16).padStart(2, '0')).join('');
    
    // Update target HyperLogLog
    snowflake.execute({
        sqlText: "UPDATE HyperLogLog SET Buckets = TO_BINARY(?, 'HEX'), LastUpdatedDate = CURRENT_TIMESTAMP() WHERE HllId = ?",
        binds: [newHexString, TARGETHLLID]
    });
    
    return 'HyperLogLogs merged successfully';
$$;

-- =============================================
-- Procedure: APPROX_COUNT_DISTINCT_HLL
-- Snowflake-style approximate distinct count using custom HyperLogLog
-- This provides a simpler interface for approximate distinct counting
-- =============================================
CREATE OR REPLACE PROCEDURE APPROX_COUNT_DISTINCT_HLL(
    TableName STRING,
    ColumnName STRING,
    WhereClause STRING,
    Precision INTEGER
)
RETURNS INTEGER
LANGUAGE JAVASCRIPT
AS
$$
    // Generate a unique HLL ID
    var hllId = Math.floor(Math.random() * 2147483647);
    
    // Initialize HyperLogLog
    snowflake.execute({
        sqlText: "CALL InitializeHyperLogLog(?, ?)",
        binds: [hllId, PRECISION]
    });
    
    // Build query to get distinct values
    var whereClauseSQL = WHERECLAUSE ? ' WHERE ' + WHERECLAUSE : '';
    var query = 'SELECT DISTINCT ' + COLUMNNAME + ' FROM ' + TABLENAME + whereClauseSQL;
    
    // Add all distinct values to HyperLogLog
    var resultSet = snowflake.execute({sqlText: query});
    
    while (resultSet.next()) {
        var value = resultSet.getColumnValue(1);
        if (value !== null) {
            snowflake.execute({
                sqlText: "CALL AddToHyperLogLog(?, ?)",
                binds: [hllId, String(value)]
            });
        }
    }
    
    // Get estimate
    var estimateResult = snowflake.execute({
        sqlText: "SELECT EstimateCardinality(?)",
        binds: [hllId]
    });
    
    estimateResult.next();
    var estimate = estimateResult.getColumnValue(1);
    
    // Clean up temporary HyperLogLog
    snowflake.execute({
        sqlText: "DELETE FROM HyperLogLog WHERE HllId = ?",
        binds: [hllId]
    });
    
    return estimate;
$$;

-- =============================================
-- Example Usage and Tests
-- =============================================

-- Example 1: Basic cardinality estimation
-- Initialize HyperLogLog with precision 12
CALL InitializeHyperLogLog(1, 12);

-- Add 10,000 unique elements
-- Note: In practice, you would loop this in a client application or use a cursor
-- For demonstration, we'll show the concept:
/*
BEGIN
    FOR i IN 0 TO 9999 DO
        CALL AddToHyperLogLog(1, 'user_' || i::STRING);
    END FOR;
END;
*/

-- Estimate cardinality
-- SELECT EstimateCardinality(1) AS estimated_count;

-- Example 2: Using HyperLogLog with a table
-- Create sample data table
CREATE OR REPLACE TABLE user_events (
    event_id INTEGER,
    user_id STRING,
    event_timestamp TIMESTAMP_NTZ
);

-- Example query to add all users to HyperLogLog
/*
DECLARE
    user_cursor CURSOR FOR SELECT DISTINCT user_id FROM user_events;
BEGIN
    CALL InitializeHyperLogLog(10, 14);
    
    FOR user_record IN user_cursor DO
        CALL AddToHyperLogLog(10, user_record.user_id);
    END FOR;
    
    RETURN EstimateCardinality(10);
END;
*/

-- Example 3: Comparing with native APPROX_COUNT_DISTINCT
-- This demonstrates how custom HyperLogLog compares with Snowflake's native function
/*
SELECT 
    COUNT(DISTINCT user_id) AS exact_count,
    APPROX_COUNT_DISTINCT(user_id) AS native_hll_estimate,
    EstimateCardinality(10) AS custom_hll_estimate
FROM user_events;
*/

-- Example 3b: Using APPROX_COUNT_DISTINCT_HLL procedure
-- Insert sample data
/*
INSERT INTO user_events
SELECT 
    ROW_NUMBER() OVER (ORDER BY SEQ4()) as event_id,
    'user_' || (UNIFORM(1, 1000, RANDOM())::STRING) as user_id,
    DATEADD(HOUR, UNIFORM(0, 720, RANDOM()), '2025-01-01'::TIMESTAMP_NTZ) as event_timestamp
FROM TABLE(GENERATOR(ROWCOUNT => 10000));

-- Use the APPROX_COUNT_DISTINCT_HLL procedure
DECLARE
    approx_count INTEGER;
    actual_count INTEGER;
BEGIN
    -- Get approximate count using custom HyperLogLog
    CALL APPROX_COUNT_DISTINCT_HLL('user_events', 'user_id', NULL, 12) INTO :approx_count;
    
    -- Get actual count
    SELECT COUNT(DISTINCT user_id) INTO :actual_count FROM user_events;
    
    -- Compare results
    SELECT 
        :actual_count as actual_unique_users,
        :approx_count as estimated_unique_users,
        ABS(:actual_count - :approx_count) / :actual_count * 100 as error_percentage;
END;

-- Use with WHERE clause
CALL APPROX_COUNT_DISTINCT_HLL(
    'user_events', 
    'user_id', 
    'event_timestamp >= ''2025-01-01'' AND event_timestamp < ''2025-01-15''',
    12
);
*/

-- Example 4: Merging HyperLogLogs from different partitions
-- Initialize two HyperLogLogs for different time periods
CALL InitializeHyperLogLog(20, 12); -- For January data
CALL InitializeHyperLogLog(21, 12); -- For February data

-- Add data from January
/*
DECLARE
    jan_cursor CURSOR FOR SELECT DISTINCT user_id FROM user_events WHERE MONTH(event_timestamp) = 1;
BEGIN
    FOR user_record IN jan_cursor DO
        CALL AddToHyperLogLog(20, user_record.user_id);
    END FOR;
END;
*/

-- Add data from February
/*
DECLARE
    feb_cursor CURSOR FOR SELECT DISTINCT user_id FROM user_events WHERE MONTH(event_timestamp) = 2;
BEGIN
    FOR user_record IN feb_cursor DO
        CALL AddToHyperLogLog(21, user_record.user_id);
    END FOR;
END;
*/

-- Merge February into January to get combined estimate
-- CALL MergeHyperLogLog(20, 21);
-- SELECT EstimateCardinality(20) AS combined_estimate;

-- =============================================
-- Performance Tips
-- =============================================
-- 1. Use appropriate precision based on your needs:
--    - Precision 12 (4KB): Good for most use cases, ~1.6% error
--    - Precision 14 (16KB): Better accuracy, ~0.8% error
--    - Precision 16 (64KB): Highest accuracy, ~0.4% error
--
-- 2. Batch operations when possible to reduce procedure call overhead
--
-- 3. Consider using Snowflake's native APPROX_COUNT_DISTINCT() for simple cases
--
-- 4. Use custom HyperLogLog when you need:
--    - Precise control over precision
--    - Ability to merge estimates from different partitions
--    - Persistent storage of HyperLogLog state for incremental updates
--
-- 5. For very large datasets, consider partitioning by time or other dimensions
--    and using multiple HyperLogLog instances that can be merged

-- =============================================
-- Cleanup (optional)
-- =============================================
-- DROP TABLE IF EXISTS HyperLogLog;
-- DROP TABLE IF EXISTS user_events;
-- DROP FUNCTION IF EXISTS ComputeHash(STRING);
-- DROP FUNCTION IF EXISTS CountLeadingZeros(INTEGER, INTEGER);
-- DROP FUNCTION IF EXISTS GetAlphaConstant(INTEGER);
-- DROP FUNCTION IF EXISTS EstimateCardinality(INTEGER);
-- DROP PROCEDURE IF EXISTS InitializeHyperLogLog(INTEGER, INTEGER);
-- DROP PROCEDURE IF EXISTS AddToHyperLogLog(INTEGER, STRING);
-- DROP PROCEDURE IF EXISTS MergeHyperLogLog(INTEGER, INTEGER);
