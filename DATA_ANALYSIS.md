# Data Analysis System Documentation

## Overview

The Data Analysis System is a comprehensive data profiling and quality assessment tool integrated into the SQL Excel App. It provides detailed statistics, pattern detection, and quality scoring for database tables, similar to VS Code's Data Wrangler or Pandas Profiling.

## Features

### 1. **Comprehensive Column Analysis**

For each column in a table, the system provides:

#### Type Detection
- Automatic inference of data types (Numeric, String, DateTime, Boolean, Mixed)
- Detection of mixed-type columns that may need standardization

#### Basic Statistics
- Total values count
- Null count and percentage
- Unique values count and percentage
- Empty string and whitespace detection
- Completeness percentage

#### Type-Specific Statistics

**Numeric Columns:**
- Min, Max, Average values
- Median value
- Standard deviation
- Sum

**String Columns:**
- Min/Max/Average length
- Shortest and longest values

**DateTime Columns:**
- Min and Max dates
- Date range duration

#### Pattern Detection
The system automatically detects common patterns in data:
- Email addresses
- URLs
- Phone numbers
- Postal codes
- UUIDs/GUIDs
- IP addresses

#### Value Distribution
- Top N most frequent values (default: 20)
- Count and percentage for each value
- Visual representation of distribution

#### Quality Scoring
Each column receives a quality score (0-100) based on:
- **Completeness (30%)**: Percentage of non-null values
- **Type Consistency (25%)**: Consistency of data types
- **Uniqueness (20%)**: Appropriate uniqueness for column type
- **Pattern Conformity (15%)**: Adherence to expected patterns
- **Outlier Detection (10%)**: Absence of anomalous values

#### Quality Issues Detection
Automatically identifies problems such as:
- High null rate (>20%)
- Mixed data types
- High empty string rate
- Invalid pattern formats (e.g., invalid emails)
- Low uniqueness in ID columns

### 2. **Overall Analysis Report**

Provides a summary view with:
- Total rows and columns
- Analysis timestamp and duration
- Overall quality score (average of all columns)
- Quick access to problematic columns

### 3. **API Endpoints**

#### Analyze a Table
```
POST /api/dataanalysis/table
Content-Type: application/json

{
  "tableName": "customers",
  "topValueCount": 20,
  "enablePatternDetection": true,
  "enableParallelProcessing": true
}
```

**Response:**
```json
{
  "success": true,
  "analysis": {
    "id": 1,
    "sourceName": "customers",
    "sourceType": "Table",
    "analyzedAt": "2026-01-29T21:19:24Z",
    "totalRows": 10,
    "totalColumns": 5,
    "overallQualityScore": 96.2,
    "analysisDuration": "00:00:00.001",
    "columnAnalyses": [...]
  }
}
```

#### Get Analysis by ID
```
GET /api/dataanalysis/{id}
```

#### Get Analysis Summary
```
GET /api/dataanalysis/{id}/summary
```

#### Get Quality Report
```
GET /api/dataanalysis/{id}/quality-report
```

Includes:
- Overall quality score
- Total issues count
- Issues per column
- Recommendations for improvement

#### Get Analysis History
```
GET /api/dataanalysis/source/{sourceName}/history
```

Returns all previous analyses for a specific source.

## Usage

### From the UI

1. Navigate to the **Data Analysis** tab in the main interface
2. Select a table from the dropdown
3. Click "Analyze" to start the analysis
4. View the comprehensive analysis results:
   - Overview panel with key metrics
   - Expandable column cards with detailed statistics
   - Search and filter columns
   - Visual progress bars and charts

### From Code

```csharp
// Inject the service
private readonly IDataAnalyzerService _analyzerService;

// Analyze a table
var analysis = await _analyzerService.AnalyzeTableAsync("customers");

// Access results
Console.WriteLine($"Quality Score: {analysis.OverallQualityScore}");
foreach (var column in analysis.ColumnAnalyses)
{
    Console.WriteLine($"{column.ColumnName}: {column.QualityScore}/100");
    if (column.QualityIssues.Any())
    {
        Console.WriteLine($"  Issues: {string.Join(", ", column.QualityIssues)}");
    }
}
```

## Architecture

### Models
- **DataAnalysis**: Root model containing overall analysis results
- **ColumnAnalysis**: Detailed analysis for a single column
- **ValueDistribution**: Frequency distribution of values
- **AnalysisConfiguration**: Configuration options for analysis

### Services
- **DataAnalyzerService**: Main service coordinating the analysis
- **ColumnAnalyzer**: Analyzes individual columns
- **PatternDetector**: Detects common patterns using regex
- **QualityScoreCalculator**: Calculates quality scores and identifies issues
- **StatisticsCalculator**: Computes statistical metrics (median, std dev, quartiles)

### Storage
Currently uses in-memory storage with dictionary-based caching. Each analysis is assigned a unique ID and stored in memory. For production use, consider implementing persistent storage with:
- Database tables (SQL Server, PostgreSQL, etc.)
- Document store (MongoDB, CosmosDB)
- Time-series database for trend analysis

## Performance Considerations

### Optimization Strategies

1. **Parallel Processing** (Default: Enabled)
   - Columns are analyzed in parallel using Task.WhenAll
   - Significant speedup for tables with many columns

2. **Sampling** (Threshold: 100,000 rows)
   - For large datasets, consider implementing sampling
   - Stratified sampling maintains distribution
   - Trade-off between speed and accuracy

3. **Caching**
   - Analysis results are cached in memory
   - Subsequent requests for same analysis ID are instant
   - Consider cache invalidation strategy for updated data

### Performance Metrics

Test Results (10 columns, 10 rows):
- Analysis Duration: ~8ms
- API Response Time: <50ms

Expected for larger datasets:
- 100 columns × 10,000 rows: ~200ms
- 50 columns × 100,000 rows: ~2-3 seconds (with parallel processing)

## Example Output

### Email Column Analysis
```
email (Column #2)
Type: TEXT → String/Email
Quality: 93/100

Completeness:  ████████████░░░ 90%
Null: 0% | Empty: 10%
Unique: 100% (9 unique values)

📝 String Statistics:
  Min Length: 13
  Max Length: 17
  Avg Length: 15.9

🎭 Patterns Detected:
  ✓ Email format: 80% (8 records)
  ✗ Invalid format: 10% (1 record)

⚠️ Quality Issues:
  • High empty string rate (10%)
  • Invalid email formats detected

💡 Recommendations:
  • Add email validation for input
  • Consider making field required
```

## Future Enhancements

### Planned Features
1. **Database Persistence**
   - Store analyses in SQL tables
   - Historical trend analysis
   - Comparison between analysis runs

2. **Advanced Visualizations**
   - Charts (histograms, pie charts, time series)
   - Correlation heatmaps
   - Distribution plots

3. **Data Quality Rules**
   - Custom validation rules
   - Automated alerts for rule violations
   - Rule templates library

4. **Machine Learning**
   - Anomaly detection using ML.NET
   - Predictive quality scoring
   - Automated data cleaning suggestions

5. **Workflow Integration**
   - Data profiling as workflow step
   - Quality gates (block workflow if score < threshold)
   - Automated reports

6. **Export Functionality**
   - PDF reports
   - Excel exports
   - CSV data dumps

## Testing

### Manual Testing

1. Load sample data:
```bash
curl -X POST http://localhost:5555/api/sqlite/upload-json \
  -H "Content-Type: application/json" \
  -d '{
    "tableName": "test_table",
    "columns": ["id", "name", "email"],
    "rows": [
      {"id": "1", "name": "John", "email": "john@test.com"},
      {"id": "2", "name": "Jane", "email": "invalid"},
      {"id": "3", "name": "Bob", "email": ""}
    ]
  }'
```

2. Run analysis:
```bash
curl -X POST http://localhost:5555/api/dataanalysis/table \
  -H "Content-Type: application/json" \
  -d '{"tableName": "test_table"}'
```

3. Verify results:
   - Check quality scores
   - Verify pattern detection (should detect 1 valid email)
   - Confirm issue detection (empty email, invalid format)

### Unit Test Examples

```csharp
[Fact]
public void PatternDetector_DetectsValidEmails()
{
    var detector = new PatternDetector();
    var values = new[] { "test@example.com", "invalid", "another@test.com" };
    
    var patterns = detector.DetectPatterns(values);
    
    Assert.Equal(2, patterns["Email"]);
}

[Fact]
public void QualityScoreCalculator_PenalizesHighNullRate()
{
    var calculator = new QualityScoreCalculator();
    var analysis = new ColumnAnalysis
    {
        TotalValues = 100,
        NullCount = 30,
        NullPercentage = 30,
        CompletenessPercentage = 70,
        InferredType = "String"
    };
    
    var score = calculator.CalculateQualityScore(analysis);
    
    Assert.True(score < 90); // Should be penalized
}
```

## License

This feature is part of the SQL Excel App project and inherits its MIT License.
