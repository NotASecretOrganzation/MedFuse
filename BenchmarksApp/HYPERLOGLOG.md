# HyperLogLog Implementation

## Overview

HyperLogLog is a probabilistic data structure used for estimating the cardinality (number of distinct elements) in a large dataset with minimal memory usage. This implementation is based on the algorithm described in [Wikipedia - HyperLogLog](https://en.wikipedia.org/wiki/HyperLogLog).

## Features

- **Memory Efficient**: Uses only 16KB (with precision 14) to estimate cardinality of millions of elements
- **Fast**: Constant time O(1) for adding elements
- **Accurate**: Typically achieves error rates under 2% for large datasets
- **Mergeable**: Multiple HyperLogLog instances can be merged to combine estimates

## API Endpoints

### Run All Examples
```bash
POST /api/HyperLogLog/examples
```
Runs all HyperLogLog demonstrations and benchmarks.

### Basic Example
```bash
POST /api/HyperLogLog/basic
```
Demonstrates basic HyperLogLog usage with 10,000 unique elements.

### Duplicate Handling
```bash
POST /api/HyperLogLog/duplicates
```
Shows how HyperLogLog correctly handles duplicate elements.

### Merge Example
```bash
POST /api/HyperLogLog/merge
```
Demonstrates merging multiple HyperLogLog instances.

### Precision Benchmark
```bash
POST /api/HyperLogLog/precision-benchmark
```
Compares accuracy and memory usage across different precision values.

### Performance Benchmark
```bash
POST /api/HyperLogLog/performance-benchmark
```
Benchmarks HyperLogLog performance with large datasets.

### Custom Estimation
```bash
POST /api/HyperLogLog/estimate
Content-Type: application/json

{
  "elements": ["user1", "user2", "user3", "user1"],
  "precision": 12
}
```
Estimates cardinality for a custom dataset.

**Response:**
```json
{
  "precision": 12,
  "actualUniqueCount": 3,
  "estimatedCount": 3,
  "errorPercentage": 0,
  "memoryUsedBytes": 4096
}
```

## Usage Examples

### Basic Usage
```csharp
// Create a HyperLogLog with precision 14 (16KB memory)
var hll = new HyperLogLog(precision: 14);

// Add elements
for (int i = 0; i < 1000000; i++)
{
    hll.Add($"user_{i}");
}

// Estimate cardinality
long estimate = hll.EstimateCardinality();
Console.WriteLine($"Estimated unique users: {estimate}");
```

### Merging Multiple HyperLogLogs
```csharp
var hll1 = new HyperLogLog(precision: 12);
var hll2 = new HyperLogLog(precision: 12);

// Add elements to both
hll1.Add("user_1");
hll1.Add("user_2");

hll2.Add("user_2");
hll2.Add("user_3");

// Merge hll2 into hll1
hll1.Merge(hll2);

// Get combined estimate
long combined = hll1.EstimateCardinality();
```

## Precision Settings

The precision parameter controls the trade-off between memory usage and accuracy:

| Precision | Buckets | Memory | Typical Error |
|-----------|---------|--------|---------------|
| 8         | 256     | 0.25 KB | ~5% |
| 10        | 1,024   | 1 KB    | ~2.5% |
| 12        | 4,096   | 4 KB    | ~1.6% |
| 14        | 16,384  | 16 KB   | ~0.8% |
| 16        | 65,536  | 64 KB   | ~0.4% |

**Recommended**: Precision 14 for most use cases (16KB memory, ~0.8% error)

## Performance Characteristics

Based on benchmarks with precision 14:

| Dataset Size | Add Time | Memory Used | Error Rate |
|--------------|----------|-------------|------------|
| 10,000       | ~21ms    | 16 KB       | ~0.35%     |
| 100,000      | ~180ms   | 16 KB       | ~0.60%     |
| 1,000,000    | ~1.8s    | 16 KB       | ~0.73%     |
| 10,000,000   | ~17.7s   | 16 KB       | ~0.76%     |

**Memory Savings**: 99.97% less memory compared to HashSet for 1M unique elements

## Use Cases

1. **Website Analytics**: Count unique visitors without storing all visitor IDs
2. **Database Query Optimization**: Estimate distinct values for query planning
3. **Network Monitoring**: Track unique IP addresses or connections
4. **Big Data Processing**: Merge cardinality estimates from distributed systems
5. **Real-time Dashboards**: Display approximate unique counts with minimal latency

## Algorithm Details

HyperLogLog works by:

1. Hashing each element to a uniform bit string
2. Using the first `p` bits to select one of `m = 2^p` buckets
3. Counting leading zeros in the remaining bits
4. Storing the maximum leading zero count for each bucket
5. Estimating cardinality using harmonic mean of bucket values

The algorithm applies bias corrections for small and large cardinality ranges to improve accuracy.

## Testing the Implementation

Run the application:
```bash
cd BenchmarksApp
dotnet run
```

Test the endpoints:
```bash
# Basic example
curl -X POST http://localhost:5244/api/HyperLogLog/basic

# All examples
curl -X POST http://localhost:5244/api/HyperLogLog/examples

# Custom estimation
curl -X POST http://localhost:5244/api/HyperLogLog/estimate \
  -H "Content-Type: application/json" \
  -d '{"elements":["a","b","c","a","d"],"precision":12}'
```

## SQL Implementations

In addition to the C# implementation, HyperLogLog is also available for direct use in databases:

### T-SQL (SQL Server)
A complete implementation for Microsoft SQL Server using T-SQL stored procedures and functions. Ideal for:
- Counting unique users, sessions, or events directly in SQL Server
- Merging cardinality estimates from different partitions or time periods
- Persistent storage of HyperLogLog state for incremental updates

See [SQL/HyperLogLog_TSQL.sql](SQL/HyperLogLog_TSQL.sql) for the implementation.

### Snowflake
A JavaScript-based UDF implementation for Snowflake Data Warehouse. Features:
- JavaScript UDFs for all core HyperLogLog operations
- Integration with Snowflake's native data types
- Comparison examples with Snowflake's native `APPROX_COUNT_DISTINCT()`

See [SQL/HyperLogLog_Snowflake.sql](SQL/HyperLogLog_Snowflake.sql) for the implementation.

### SQL Documentation
For detailed usage examples, API reference, and best practices for both SQL implementations, see [SQL/README.md](SQL/README.md).

Both SQL implementations include:
- `APPROX_COUNT_DISTINCT_EXEC` (T-SQL) and `APPROX_COUNT_DISTINCT_HLL` (Snowflake) procedures that provide a simplified interface for approximate distinct counting, similar to native database functions
- Full HyperLogLog API for advanced use cases requiring persistent state and merging capabilities

## References

- [Wikipedia - HyperLogLog](https://en.wikipedia.org/wiki/HyperLogLog)
- [Original Paper: Flajolet et al., 2007](http://algo.inria.fr/flajolet/Publications/FlFuGaMe07.pdf)
- [Google Research Blog: HyperLogLog in Practice](https://research.google/blog/hyperloglog-in-practice-algorithmic-engineering-of-a-state-of-the-art-cardinality-estimation-algorithm/)
