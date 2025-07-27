using System;
using System.Collections.Generic;

namespace LiveStreamingMonitoringSystem.Models
{
    /// <summary>
    /// 基础结果类型
    /// </summary>
    public class BaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ErrorCode { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 报警事件操作结果
    /// </summary>
    public class AlarmEventResult : BaseResult
    {
        public Guid? EventId { get; set; }
        public AlarmEventType? EventType { get; set; }
        public AlarmLevel? Level { get; set; }
        public ProcessStatus? Status { get; set; }
    }

    /// <summary>
    /// 设备操作结果
    /// </summary>
    public class DeviceOperationResult : BaseResult
    {
        public string DeviceId { get; set; }
        public DeviceType? DeviceType { get; set; }
        public DeviceStatus? Status { get; set; }
        public Dictionary<string, object> DeviceInfo { get; set; }
    }

    /// <summary>
    /// 设备命令执行结果
    /// </summary>
    public class DeviceCommandResult : BaseResult
    {
        public string DeviceId { get; set; }
        public DeviceCommand Command { get; set; }
        public Dictionary<string, object> CommandParameters { get; set; }
        public Dictionary<string, object> CommandResult { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 监控结果
    /// </summary>
    public class MonitoringResult : BaseResult
    {
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public ViewerStatus? ViewerStatus { get; set; }
        public int? RiskScore { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 行为记录结果
    /// </summary>
    public class BehaviorRecordResult : BaseResult
    {
        public string ViewerId { get; set; }
        public ViewerBehaviorType BehaviorType { get; set; }
        public DateTime BehaviorTime { get; set; }
        public bool IsRisky { get; set; }
        public int RiskScore { get; set; }
    }

    /// <summary>
    /// 批量操作结果
    /// </summary>
    public class BatchOperationResult : BaseResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BatchOperationFailure> FailureDetails { get; set; } = new List<BatchOperationFailure>();
        public TimeSpan TotalExecutionTime { get; set; }
    }

    /// <summary>
    /// 批量操作失败详情
    /// </summary>
    public class BatchOperationFailure
    {
        public string ViewerId { get; set; }
        public string DeviceId { get; set; }
        public string ItemId { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public DateTime FailureTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 批量处理结果
    /// </summary>
    public class BatchProcessResult : BaseResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BatchProcessFailure> FailureDetails { get; set; } = new List<BatchProcessFailure>();
    }

    /// <summary>
    /// 批量处理失败详情
    /// </summary>
    public class BatchProcessFailure
    {
        public Guid EventId { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public DateTime FailureTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 批量插入结果
    /// </summary>
    public class BatchInsertResult : BaseResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> FailedItems { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 事务执行结果
    /// </summary>
    public class TransactionResult : BaseResult
    {
        public List<OperationResult> OperationResults { get; set; } = new List<OperationResult>();
        public TimeSpan ExecutionTime { get; set; }
        public bool RolledBack { get; set; }
    }

    /// <summary>
    /// 操作结果
    /// </summary>
    public class OperationResult
    {
        public string Sql { get; set; }
        public int RowsAffected { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public object AttemptedValue { get; set; }
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    /// <summary>
    /// 风险分析结果
    /// </summary>
    public class RiskAnalysisResult
    {
        public bool IsRisky { get; set; }
        public int RiskScore { get; set; }
        public SeverityLevel Severity { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> AnalysisData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 通知发送结果
    /// </summary>
    public class NotificationResult : BaseResult
    {
        public string NotificationId { get; set; }
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; }
        public string Recipient { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// 报表生成结果
    /// </summary>
    public class ReportGenerationResult : BaseResult
    {
        public string ReportId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public ExportFormat Format { get; set; }
        public long FileSize { get; set; }
        public TimeSpan GenerationTime { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    /// <summary>
    /// 导出结果
    /// </summary>
    public class ExportResult : BaseResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public ExportFormat Format { get; set; }
        public long FileSize { get; set; }
        public int RecordCount { get; set; }
        public TimeSpan ExportTime { get; set; }
        public CompressionType? CompressionType { get; set; }
        public EncryptionType? EncryptionType { get; set; }
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult : BaseResult
    {
        public string FilePath { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
        public TimeSpan ImportTime { get; set; }
    }

    /// <summary>
    /// 导入错误
    /// </summary>
    public class ImportError
    {
        public int LineNumber { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public string RawData { get; set; }
    }

    /// <summary>
    /// 备份结果
    /// </summary>
    public class BackupResult : BaseResult
    {
        public string BackupId { get; set; }
        public string BackupPath { get; set; }
        public BackupType Type { get; set; }
        public long BackupSize { get; set; }
        public TimeSpan BackupTime { get; set; }
        public DateTime BackupDate { get; set; }
        public CompressionType? CompressionType { get; set; }
        public EncryptionType? EncryptionType { get; set; }
    }

    /// <summary>
    /// 恢复结果
    /// </summary>
    public class RestoreResult : BaseResult
    {
        public string BackupId { get; set; }
        public string BackupPath { get; set; }
        public RestoreType Type { get; set; }
        public int RestoredRecords { get; set; }
        public TimeSpan RestoreTime { get; set; }
        public DateTime RestoreDate { get; set; }
        public List<string> RestoredTables { get; set; } = new List<string>();
    }

    /// <summary>
    /// 同步结果
    /// </summary>
    public class SyncResult : BaseResult
    {
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public int TotalRecords { get; set; }
        public int SyncedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int ConflictRecords { get; set; }
        public TimeSpan SyncTime { get; set; }
        public DateTime LastSyncTime { get; set; }
    }

    /// <summary>
    /// 连接测试结果
    /// </summary>
    public class ConnectionTestResult : BaseResult
    {
        public string ConnectionString { get; set; }
        public string ServerVersion { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime TestTime { get; set; }
        public Dictionary<string, object> ServerInfo { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 性能测试结果
    /// </summary>
    public class PerformanceTestResult : BaseResult
    {
        public string TestName { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public long MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public int ThroughputPerSecond { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 健康检查结果
    /// </summary>
    public class HealthCheckResult : BaseResult
    {
        public string ComponentName { get; set; }
        public HealthStatus Status { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public List<HealthCheckResult> Dependencies { get; set; } = new List<HealthCheckResult>();
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public enum HealthStatus
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2,
        Unhealthy = 3,
        Critical = 4
    }

    /// <summary>
    /// 缓存操作结果
    /// </summary>
    public class CacheResult : BaseResult
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public TimeSpan? ExpiryTime { get; set; }
        public bool Hit { get; set; }
        public string CacheProvider { get; set; }
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    /// <typeparam name="T">搜索结果类型</typeparam>
    public class SearchResult<T> : PagedResult<T>
    {
        public string Query { get; set; }
        public TimeSpan SearchTime { get; set; }
        public Dictionary<string, int> Facets { get; set; } = new Dictionary<string, int>();
        public List<string> Suggestions { get; set; } = new List<string>();
        public double MaxScore { get; set; }
    }

    /// <summary>
    /// 文件操作结果
    /// </summary>
    public class FileOperationResult : BaseResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string Hash { get; set; }
        public HashAlgorithm HashAlgorithm { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    /// <summary>
    /// 认证结果
    /// </summary>
    public class AuthenticationResult : BaseResult
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }
        public DateTime TokenExpiry { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public Dictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 授权结果
    /// </summary>
    public class AuthorizationResult : BaseResult
    {
        public string UserId { get; set; }
        public string Resource { get; set; }
        public string Action { get; set; }
        public bool IsAuthorized { get; set; }
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public List<string> GrantedPermissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 配置更新结果
    /// </summary>
    public class ConfigurationUpdateResult : BaseResult
    {
        public string ConfigurationKey { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public DateTime UpdateTime { get; set; }
        public string UpdatedBy { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>
    /// 日志查询结果
    /// </summary>
    public class LogQueryResult : PagedResult<LogEntry>
    {
        public string Query { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public LogLevel MinLevel { get; set; }
        public TimeSpan QueryTime { get; set; }
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string Logger { get; set; }
        public string Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 统计查询结果
    /// </summary>
    public class StatisticsResult : BaseResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public List<TimeSeriesData> TimeSeries { get; set; } = new List<TimeSeriesData>();
    }

    /// <summary>
    /// 时间序列数据
    /// </summary>
    public class TimeSeriesData
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 今日统计数据
    /// </summary>
    public class TodayStatistics
    {
        public int TotalViewers { get; set; }
        public int PeakConcurrent { get; set; }
        public double AverageWatchTime { get; set; }
        public int TotalInteractions { get; set; }
        public int TotalAlarms { get; set; }
        public int ResolvedAlarms { get; set; }
        public double SystemUptime { get; set; }
    }
}
