# i4Twins Readings Service

Backend technical task implementation for Dana Tadbir Integrated Intelligent Systems Co. / i4Twins.

Repository:

https://github.com/alirezanobakht2004/i4twins_project_alireza_nobakht

---

## 1. Problem Summary

This project implements a small backend service that reads numeric sensor readings from a JSON Lines file, cleans messy input data, stores valid unique readings, and exposes a time-based aggregation API.

The input file is:

```text
assets/readings.jsonl
````

Each line contains one JSON reading.

The service handles:

* Reading JSONL input line by line
* Rejecting invalid records without crashing
* Removing duplicate readings safely
* Handling out-of-order timestamps
* Persisting cleaned readings in SQLite
* Returning an ingestion count report
* Returning time-bucketed aggregation results through an API
* Unit testing deduplication and aggregation logic

---

## 2. Technology Stack

The implementation uses:

```text
C# / .NET 8
ASP.NET Core Minimal API
SQLite
Entity Framework Core SQLite
xUnit
Visual Studio
Git / GitHub
```

### Why .NET 8?

The task document states that C#/.NET 8 is preferred. Therefore, all projects target `net8.0`.

Even if a newer SDK is installed on the machine, the project target framework remains `.NET 8` to match the task expectation.

---

## 3. High-Level Architecture

The project uses a pragmatic Clean Architecture / simple layered architecture.

The goal was to keep the solution clean, readable, testable, and maintainable without over-engineering.

No CQRS, events, generic repositories, message brokers, or unnecessary abstractions were introduced.

Project structure:

```text
src/
  I4Twins.Readings.Api/
  I4Twins.Readings.Application/
  I4Twins.Readings.Domain/
  I4Twins.Readings.Infrastructure/

tests/
  I4Twins.Readings.Tests/

assets/
  readings.jsonl

Documents/
  i4Twins_Backend-Task_Candidate_EN.pdf
```

Dependency direction:

```text
Api
 ├── Application
 └── Infrastructure

Application
 └── Domain

Infrastructure
 ├── Application
 └── Domain

Tests
 ├── Domain
 └── Application
```

The important rule is:

```text
Domain does not depend on API, database, JSON files, EF Core, or ASP.NET Core.
```

This keeps the business rules independent from infrastructure details.

---

## 4. Why This Architecture Was Chosen

The task asks for domain logic to be independent from infrastructure details such as file, database, and API. It also asks that business rules such as validation and duplicate handling live in the domain layer instead of the controller or repository.

For that reason, the solution was split into these layers:

### Domain Layer

Contains the core business concepts and rules:

* `Reading`
* `ReadingIdentity`
* `ReadingDeduplicator`
* `ReadingAggregator`
* `ReadingAggregationBucket`
* `ReadingValidationResult`

The Domain layer is responsible for:

* Reading validation
* Duplicate identity definition
* Deduplication policy
* Aggregation calculation
* Time-bucket logic

### Application Layer

Contains use cases and application contracts:

* `IngestReadingsService`
* `GetAggregatedReadingsService`
* `IReadingSource`
* `IReadingRepository`
* `RawReadingRecord`
* `IngestionReport`
* `AggregationQuery`
* `AggregationBucketResponse`

The Application layer coordinates the flow but does not know about SQLite or JSONL implementation details.

### Infrastructure Layer

Contains technical implementations:

* `JsonlReadingSource`
* `ReadingsDbContext`
* `ReadingEntity`
* `SqliteReadingRepository`
* `DatabaseInitializer`

The Infrastructure layer knows how to read files and persist data.

### API Layer

Contains HTTP endpoints and dependency registration:

* `POST /api/ingestion`
* `GET /api/aggregations`
* `GET /health`

The API layer validates HTTP request parameters and delegates the real logic to the Application and Domain layers.

---

## 5. Storage Choice

SQLite was selected as the persistence layer.

### Why SQLite?

SQLite was chosen because:

* The task is a small backend service.
* The provided data file contains roughly 2,150 readings.
* No production deployment is required.
* No external database server is needed.
* It is easy for reviewers to run locally.
* It still provides real persistence, unlike in-memory storage.
* It supports unique constraints, which are useful for safe deduplication.

### Scalability Trade-Off

SQLite is not the best choice for a high-throughput production system.

For a larger production system, I would consider:

* PostgreSQL for general production use
* TimescaleDB if time-series query performance becomes important
* Batch ingestion for large files
* Streaming ingestion for message broker or HTTP-based inputs
* Database migrations instead of `EnsureCreated`

The current architecture keeps this migration possible because only the Infrastructure layer depends on SQLite. The Domain and Application layers would not need major changes if SQLite was replaced with PostgreSQL or another database.

---

## 6. Database Design

The database table is:

```text
Readings
```

Main fields:

```text
Id
DeviceId
Metric
TimestampUtcTicks
Value
Seq
```

### Timestamp Storage

Timestamps are stored as UTC ticks:

```text
TimestampUtcTicks
```

Reason:

* SQLite has limited native date/time support.
* Ticks make filtering exact and simple.
* Range queries become straightforward.
* The Domain still works with `DateTimeOffset`.

### Indexes

The repository defines a unique index on:

```text
DeviceId + Metric + TimestampUtcTicks + Seq
```

This enforces the deduplication identity at database level.

A second index is defined on:

```text
DeviceId + Metric + TimestampUtcTicks
```

This supports efficient range queries for aggregation.

---

## 7. Reading Input Format

Each reading is expected to have this structure:

```json
{
  "deviceId": "PUMP-01",
  "metric": "temperature",
  "ts": "2025-06-01T08:33:00Z",
  "value": 67.21,
  "seq": 1199
}
```

Fields:

| Field      | Meaning                 |
| ---------- | ----------------------- |
| `deviceId` | Device identifier       |
| `metric`   | Sensor metric name      |
| `ts`       | ISO-8601 UTC timestamp  |
| `value`    | Numeric reading value   |
| `seq`      | Integer sequence number |

---

## 8. Validation Policy

Validation is implemented in the Domain layer through `Reading.Create(...)`.

A record is rejected as invalid when:

* JSON line is malformed
* `deviceId` is missing, null, empty, or whitespace
* `metric` is missing, null, empty, or whitespace
* `ts` is missing
* `ts` is not a valid timestamp
* `ts` is not UTC
* `value` is missing
* `value` is not numeric
* `value` is not finite
* `seq` is missing
* `seq` is not an integer

Invalid records are counted and skipped. They do not crash ingestion.

The infrastructure JSONL reader only parses JSON into a raw record. It does not decide business validity. This separation was intentional so that validation remains in the Domain layer.

---

## 9. Deduplication Policy

The task defines duplicate identity as:

```text
(deviceId, metric, ts, seq)
```

This implementation follows that rule exactly.

### Policy

```text
First valid reading wins.
```

If two readings have the same:

```text
DeviceId + Metric + TimestampUtc + Seq
```

then the first valid occurrence is stored and later occurrences are skipped.

### What if duplicate readings have different values?

If a later duplicate has the same identity but a different `value`, it is still treated as a duplicate because `value` is not part of the identity rule.

Example:

```json
{"deviceId":"PUMP-01","metric":"pressure","ts":"2025-06-01T08:30:40Z","seq":1395,"value":8.68}
{"deviceId":"PUMP-01","metric":"pressure","ts":"2025-06-01T08:30:40Z","seq":1395,"value":10.917}
```

These two records have the same identity. Therefore, the first valid record is kept and the second one is skipped.

The skipped record is counted as:

```text
duplicate
```

If its value differs from the stored value, it is also counted as:

```text
conflicting duplicate
```

### Why not last-write-wins?

Last-write-wins was not selected because:

* It makes results dependent on input order.
* It can overwrite a previously accepted reading.
* It is weaker for idempotent ingestion.
* The file is deliberately messy and out of order.

### Why not average conflicting duplicates?

Averaging was not selected because:

* The task does not define duplicate conflict merging.
* It would create a new value that did not exist in the source data.
* It would make ingestion more complex without a clear business rule.

---

## 10. Out-of-Order Data Policy

The input file is not assumed to be sorted by time.

The service accepts out-of-order readings.

Ingestion does not depend on timestamp order.

Aggregation filters and groups readings by timestamp, so the final aggregation result is correct even if the input order is random.

---

## 11. Aggregation Policy

The aggregation endpoint accepts:

```text
deviceId
metric
from
to
bucketSeconds
```

The range policy is:

```text
[from, to)
```

This means:

* A reading exactly at `from` is included.
* A reading exactly at `to` is excluded.

Each bucket returns:

```text
bucketStartUtc
count
average
min
max
```

### Bucket Calculation

The bucket start is calculated from the requested `from` time:

```text
bucketIndex = floor((reading.TimestampUtc - from) / bucketSize)
bucketStart = from + bucketIndex * bucketSize
```

Example:

```text
from = 2025-06-01T08:00:00Z
bucketSeconds = 300
```

Buckets are:

```text
08:00:00
08:05:00
08:10:00
...
```

### Empty Bucket Policy

Empty buckets are omitted.

Reason:

* The response is smaller.
* It avoids returning large arrays of empty buckets.
* It is easier to read.
* The task allows either omitting empty buckets or returning them with count zero, as long as the behavior is documented.

---

## 12. API Endpoints

### Health Check

```http
GET /health
```

Example:

```bash
curl http://localhost:5137/health
```

Response:

```json
{
  "status": "ok",
  "service": "i4twins-readings-service"
}
```

---

### Ingestion

```http
POST /api/ingestion
```

Example:

```bash
curl -X POST http://localhost:5137/api/ingestion
```

Example response from the provided data:

```json
{
  "totalLines": 2150,
  "storedReadings": 2104,
  "duplicatesRemoved": 38,
  "invalidRecordsRejected": 8,
  "conflictingDuplicates": 8
}
```

Field meanings:

| Field                    | Meaning                                       |
| ------------------------ | --------------------------------------------- |
| `totalLines`             | Number of lines read from the JSONL file      |
| `storedReadings`         | Number of new readings stored in the database |
| `duplicatesRemoved`      | Number of duplicate records skipped           |
| `invalidRecordsRejected` | Number of invalid records rejected            |
| `conflictingDuplicates`  | Number of duplicates with different values    |

### Idempotency Behavior

If ingestion is executed again on the same database, readings are not inserted again.

Example second run:

```json
{
  "totalLines": 2150,
  "storedReadings": 0,
  "duplicatesRemoved": 2142,
  "invalidRecordsRejected": 8,
  "conflictingDuplicates": 8
}
```

This is expected.

Reason:

* 8 records are invalid.
* 2142 records are valid input records.
* All valid records are already represented in the database after the first run.
* The second run therefore stores zero new readings.

---

### Aggregation

```http
GET /api/aggregations
```

Query parameters:

| Parameter       | Required | Example                |
| --------------- | -------- | ---------------------- |
| `deviceId`      | Yes      | `PUMP-01`              |
| `metric`        | Yes      | `temperature`          |
| `from`          | Yes      | `2025-06-01T08:00:00Z` |
| `to`            | Yes      | `2025-06-01T09:00:00Z` |
| `bucketSeconds` | Yes      | `300`                  |

Example:

```bash
curl "http://localhost:5137/api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T08:00:00Z&to=2025-06-01T09:00:00Z&bucketSeconds=300"
```

Example response:

```json
[
  {
    "bucketStartUtc": "2025-06-01T08:00:00+00:00",
    "count": 31,
    "average": 68.88145161290322,
    "min": 67.808,
    "max": 70.5
  },
  {
    "bucketStartUtc": "2025-06-01T08:05:00+00:00",
    "count": 30,
    "average": 68.52343333333332,
    "min": 67.508,
    "max": 69.825
  }
]
```

---

## 13. API Validation

The aggregation endpoint returns `400 Bad Request` when:

* `deviceId` is empty
* `metric` is empty
* `from` is not UTC
* `to` is not UTC
* `from >= to`
* `bucketSeconds <= 0`

A valid request with no matching readings returns:

```json
[]
```

---

## 14. Logging

The API logs important processing events:

* Ingestion started
* Ingestion completed
* Total lines
* Stored readings
* Duplicates removed
* Invalid records rejected
* Conflicting duplicates

The logging uses the standard ASP.NET Core logging infrastructure.

For a larger production version, I would add more structured logging around invalid line numbers and duplicate conflicts, possibly with configurable log levels.

---

## 15. How to Run

### Prerequisites

Install:

```text
.NET 8 SDK
```

Check SDK installation:

```bash
dotnet --version
```

The installed SDK may be newer, but the projects target `.NET 8`.

---

### Clone the Repository

```bash
git clone https://github.com/alirezanobakht2004/i4twins_project_alireza_nobakht.git
cd i4twins_project_alireza_nobakht
```

---

### Restore Packages

```bash
dotnet restore
```

---

### Build

```bash
dotnet build
```

---

### Run Tests

```bash
dotnet test
```

---

### Run API

```bash
dotnet run --project src/I4Twins.Readings.Api/I4Twins.Readings.Api.csproj
```

The API will print a local URL such as:

```text
http://localhost:5137
```

Use the actual port shown in the terminal.

---

### Ingest Data

```bash
curl -X POST http://localhost:5137/api/ingestion
```

---

### Query Aggregation

```bash
curl "http://localhost:5137/api/aggregations?deviceId=PUMP-01&metric=temperature&from=2025-06-01T08:00:00Z&to=2025-06-01T09:00:00Z&bucketSeconds=300"
```

---

## 16. Database File

The SQLite database file is created automatically when the API starts.

Default database:

```text
readings.db
```

The database is ignored by Git.

To reset the local state, stop the API and delete:

```text
readings.db
```

Then run the API and call ingestion again.

---

## 17. Configuration

Configuration is in:

```text
src/I4Twins.Readings.Api/appsettings.json
```

Main settings:

```json
{
  "ConnectionStrings": {
    "ReadingsDatabase": "Data Source=readings.db"
  },
  "Readings": {
    "FilePath": "../../assets/readings.jsonl"
  }
}
```

The file path is relative to the API project content root.

---

## 18. Tests

The test project is:

```text
tests/I4Twins.Readings.Tests
```

The tests focus on the main required logic:

* Deduplication
* Aggregation

### Deduplication Tests

The deduplication tests verify that:

1. Same identity is stored once.
2. Duplicate with the same value is counted as duplicate.
3. Duplicate with a different value keeps the first value and counts a conflicting duplicate.
4. Same timestamp but different sequence number is not considered duplicate.

### Aggregation Tests

The aggregation tests verify that:

1. Count, average, min, and max are calculated correctly.
2. Multiple buckets are created correctly.
3. Out-of-order readings are aggregated correctly.
4. `[from, to)` boundary behavior is respected.
5. Empty buckets are omitted.

---

## 19. Test Assumptions

The tests are based on these assumptions:

* Duplicate identity is based only on `deviceId`, `metric`, `ts`, and `seq`.
* `value` is not part of duplicate identity.
* First valid reading wins.
* Duplicate readings with different values are skipped, not merged.
* Aggregation uses `[from, to)`.
* Empty buckets are omitted.
* Bucket starts are calculated relative to the requested `from`.
* Input order must not affect aggregation correctness.
* Domain tests should not depend on SQLite, JSONL files, or HTTP.

---

## 20. Performance Considerations

The provided file contains roughly 2,150 readings, so the implementation is intentionally simple.

### Ingestion Complexity

Current ingestion flow:

```text
Read JSONL file
Validate records
Deduplicate valid records in memory
Store unique readings in SQLite
```

The in-memory deduplication uses a dictionary keyed by reading identity.

Expected complexity:

```text
O(n)
```

where `n` is the number of valid readings.

### Aggregation Complexity

Aggregation filters readings by device, metric, and time range using the database index, then groups the returned readings in memory.

Expected complexity for the returned range:

```text
O(k)
```

where `k` is the number of readings matching the query.

### Trade-Off

Aggregation is implemented in Domain code instead of SQL.

Reason:

* Easier to test
* Keeps aggregation rules independent from SQLite
* Good enough for the task data size
* Makes the business logic explicit

For a very large production dataset, aggregation could be optimized by:

* Moving grouping to SQL
* Using PostgreSQL or TimescaleDB
* Adding pre-aggregated tables
* Adding pagination or maximum query window limits
* Processing ingestion in batches

---

## 21. Extensibility

The design supports future extension without widespread changes.

### Adding a New Input Source

Current input source:

```text
JsonlReadingSource
```

It implements:

```text
IReadingSource
```

To add a new input source such as HTTP, message broker, or another file format, create a new implementation of `IReadingSource`.

The Application and Domain layers do not need to change.

### Replacing SQLite

Current repository:

```text
SqliteReadingRepository
```

It implements:

```text
IReadingRepository
```

To replace SQLite with PostgreSQL, TimescaleDB, or another database, create a new repository implementation.

The Domain and Application layers do not need to change.

### Adding More Aggregation Types

Current aggregation returns:

```text
count
average
min
max
```

If future requirements include sum, median, or percentile, the preferred approach would be:

* Extend the Domain aggregation model
* Add focused tests for the new statistic
* Keep API response backward compatible if possible

---

## 22. Error Handling

The service avoids crashing on messy data.

Examples:

* Malformed JSON line is returned as a raw record with parse error.
* Invalid business data is rejected by the Domain layer.
* Duplicate database insert conflict is handled by checking the existing record.
* Invalid API query parameters return `400 Bad Request`.

Unexpected errors, such as missing input file or database failure, are allowed to fail visibly because they indicate configuration or environment problems.

---

## 23. Git History and Commit Rationale

The project was implemented step by step with meaningful commits instead of one final commit.

### `Initialization`

Added the original task document and input data file.

Reason:

* Keep the given materials inside the repository.
* Make the repository self-contained for review.
* Preserve the original input file used for testing.

---

### `chore: initialize .NET solution structure.`

Created the solution and projects:

```text
Api
Application
Domain
Infrastructure
Tests
```

Reason:

* Establish the architecture before writing business logic.
* Make the dependency direction clear from the beginning.
* Keep future commits focused.

---

### `feat(domain): add reading model validation and identity`

Added:

```text
Reading
ReadingIdentity
ReadingValidationResult
```

Reason:

* The Domain layer should own business validity.
* Reading identity is central to deduplication.
* Controllers and repositories should not define validity rules.
* Validation is easier to unit test when isolated.

---

### `feat(domain): add reading deduplication policy`

Added:

```text
ReadingDeduplicator
ReadingDeduplicationResult
```

Reason:

* Deduplication is a business rule.
* The duplicate identity is defined by the task.
* First-valid-reading-wins policy is explicit and testable.
* Conflicting duplicates are tracked separately for auditability.

---

### `feat(domain): add reading aggregation logic"`

Added:

```text
ReadingAggregator
ReadingAggregationBucket
```

Reason:

* Aggregation is core business logic.
* Keeping aggregation in Domain makes it independent from SQL and HTTP.
* The `[from, to)` boundary rule is centralized.
* Empty bucket behavior is explicit.

Note: this commit message contains an extra quote character at the end. It does not affect the code or repository correctness.

---

### `feat(application): add ingestion use case`

Added:

```text
IngestReadingsService
RawReadingRecord
IngestionReport
IReadingSource
IReadingRepository
StoreReadingResult
StoreReadingStatus
```

Reason:

* The Application layer coordinates ingestion.
* It connects the source, domain validation, deduplication, and repository.
* It does not know whether the source is JSONL or the storage is SQLite.
* It returns a clear count report required by the task.

---

### `feat(application): add aggregation use case`

Added:

```text
GetAggregatedReadingsService
AggregationQuery
AggregationBucketResponse
```

Reason:

* The API should not directly call repository and domain logic.
* The Application layer provides a clear use case.
* The repository loads relevant readings.
* The Domain aggregator computes the result.

---

### `feat(infrastructure): add jsonl reading source`

Added:

```text
JsonlReadingSource
```

Reason:

* JSONL parsing is an infrastructure concern.
* The Application layer only depends on `IReadingSource`.
* Malformed JSON is handled without crashing ingestion.
* Business validation is still left to the Domain layer.

---

### `feat(infrastructure): add sqlite reading repository`

Added:

```text
ReadingsDbContext
ReadingEntity
SqliteReadingRepository
DatabaseInitializer
```

Reason:

* SQLite provides local persistent storage.
* EF Core keeps database implementation clean.
* Unique index enforces idempotency at database level.
* The repository maps between database entity and Domain model.
* `EnsureCreated` is sufficient for this candidate task.

---

### `feat(api): expose ingestion and aggregation endpoints`

Added:

```text
POST /api/ingestion
GET /api/aggregations
GET /health
```

Reason:

* Exposes the required API behavior.
* Keeps HTTP concerns in the API layer.
* Delegates ingestion and aggregation to Application services.
* Initializes database at startup.
* Logs ingestion start and completion.

---

### `test: add focused deduplication and aggregation tests`

Added focused unit tests for:

```text
ReadingDeduplicator
ReadingAggregator
```

Reason:

* The task specifically asks for tests around deduplication and aggregation.
* Tests stay focused and readable.
* Domain tests do not require database, file system, or HTTP server.
* The most important rules are verified directly.

---

## 24. Known Limitations

This implementation intentionally avoids unnecessary production complexity.

Current limitations:

* No authentication
* No UI
* No Docker setup
* No message broker
* No Kubernetes
* No EF migrations
* No pagination on aggregation results
* No advanced observability
* No distributed ingestion locking
* No production-grade time-series database

These were not included because the task scope focuses on ingestion, cleaning, deduplication, aggregation, tests, count report, and README.

---

## 25. Future Improvements

Possible future improvements:

1. Replace SQLite with PostgreSQL or TimescaleDB for higher scalability.
2. Add EF Core migrations.
3. Add integration tests for API endpoints.
4. Add repository tests using SQLite in-memory mode.
5. Add Docker Compose for easier environment setup.
6. Add support for more aggregation statistics such as sum, median, and percentile.
7. Add batch ingestion for very large files.
8. Add structured logs for invalid line numbers and conflict details.
9. Add request limits for very large aggregation ranges.
10. Add OpenAPI/Swagger documentation.

---

## 26. AI Usage Disclosure

AI assistance was used during this task.

AI was used for:

* Discussing architecture options
* Comparing SQLite and PostgreSQL trade-offs
* Defining the deduplication policy
* Reviewing the task requirements
* Generating draft code structure
* Preparing README wording
* Suggesting focused test cases

All implementation decisions were reviewed and adjusted manually.

The final policies used in the project are:

```text
.NET 8 Minimal API
SQLite persistence
Simple Clean Architecture
Domain-level validation and deduplication
First-valid-reading-wins duplicate policy
Conflicting duplicates are skipped and counted
Invalid records are rejected and counted
Out-of-order input is accepted
Aggregation uses [from, to)
Empty buckets are omitted
Focused unit tests for deduplication and aggregation
```

---

## 27. Summary

This project implements the required backend service with a simple, maintainable architecture.

Main design priorities were:

* Correct messy-data handling
* Clear deduplication policy
* Correct aggregation behavior
* Domain logic independent from infrastructure
* Simple local execution
* Focused unit tests
* Complete documentation
* Meaningful incremental Git history

