using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LiveStreamingMonitoringSystem.Data
{
    /// <summary>
    /// 数据库服务 - 提供与SQL Server 2019R2的交互功能
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// 执行查询并返回数据表
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>数据表</returns>
        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogInformation($"执行查询: {sql}");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        // 添加参数
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            
                            _logger.LogInformation($"查询完成，返回 {dataTable.Rows.Count} 行数据");
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行查询时发生异常: {sql}");
                throw;
            }
        }

        /// <summary>
        /// 执行非查询命令（INSERT, UPDATE, DELETE）
        /// </summary>
        /// <param name="sql">SQL命令</param>
        /// <param name="parameters">参数</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogInformation($"执行非查询命令: {sql}");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        // 添加参数
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation($"命令执行完成，影响 {rowsAffected} 行");
                        
                        return rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行非查询命令时发生异常: {sql}");
                throw;
            }
        }

        /// <summary>
        /// 执行标量查询，返回单个值
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>查询结果</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogInformation($"执行标量查询: {sql}");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        // 添加参数
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                            }
                        }

                        var result = await command.ExecuteScalarAsync();
                        _logger.LogInformation($"标量查询完成，结果: {result}");
                        
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行标量查询时发生异常: {sql}");
                throw;
            }
        }

        /// <summary>
        /// 执行事务操作
        /// </summary>
        /// <param name="operations">事务操作列表</param>
        /// <returns>执行结果</returns>
        public async Task<TransactionResult> ExecuteTransactionAsync(List<DatabaseOperation> operations)
        {
            var result = new TransactionResult();
            
            try
            {
                _logger.LogInformation($"开始执行事务，包含 {operations.Count} 个操作");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var operation in operations)
                            {
                                using (var command = new SqlCommand(operation.Sql, connection, transaction))
                                {
                                    // 添加参数
                                    if (operation.Parameters != null)
                                    {
                                        foreach (var param in operation.Parameters)
                                        {
                                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                                        }
                                    }

                                    var rowsAffected = await command.ExecuteNonQueryAsync();
                                    result.OperationResults.Add(new OperationResult
                                    {
                                        Sql = operation.Sql,
                                        RowsAffected = rowsAffected,
                                        Success = true
                                    });
                                }
                            }

                            transaction.Commit();
                            result.Success = true;
                            result.Message = "事务执行成功";
                            
                            _logger.LogInformation("事务提交成功");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            result.Success = false;
                            result.Message = $"事务执行失败: {ex.Message}";
                            
                            _logger.LogError(ex, "事务执行失败，已回滚");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行事务时发生异常");
                result.Success = false;
                result.Message = $"事务执行异常: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 获取直播间观众数据
        /// </summary>
        /// <param name="roomId">直播间ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>观众数据列表</returns>
        public async Task<List<ViewerData>> GetViewerDataAsync(string roomId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var sql = @"
                    SELECT v.*, vd.JoinTime, vd.LeaveTime, vd.Duration, vd.InteractionCount,
                           vd.ChatMessageCount, vd.GiftCount, vd.ShareCount, vd.DeviceInfo,
                           vd.LocationInfo, vd.NetworkInfo
                    FROM Viewers v
                    INNER JOIN ViewerDetails vd ON v.Id = vd.ViewerId
                    WHERE v.LiveRoomId = @RoomId 
                      AND vd.JoinTime >= @StartTime 
                      AND vd.JoinTime <= @EndTime
                    ORDER BY vd.JoinTime DESC";

                var parameters = new Dictionary<string, object>
                {
                    { "RoomId", roomId },
                    { "StartTime", startTime },
                    { "EndTime", endTime }
                };

                var dataTable = await ExecuteQueryAsync(sql, parameters);
                var viewerDataList = new List<ViewerData>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var viewerData = new ViewerData
                    {
                        Id = row["Id"].ToString(),
                        UserId = row["UserId"].ToString(),
                        UserName = row["UserName"].ToString(),
                        Avatar = row["Avatar"].ToString(),
                        LiveRoomId = row["LiveRoomId"].ToString(),
                        JoinTime = Convert.ToDateTime(row["JoinTime"]),
                        LeaveTime = row["LeaveTime"] == DBNull.Value ? null : Convert.ToDateTime(row["LeaveTime"]),
                        Duration = Convert.ToInt32(row["Duration"]),
                        InteractionCount = Convert.ToInt32(row["InteractionCount"]),
                        ChatMessageCount = Convert.ToInt32(row["ChatMessageCount"]),
                        GiftCount = Convert.ToInt32(row["GiftCount"]),
                        ShareCount = Convert.ToInt32(row["ShareCount"]),
                        DeviceInfo = JsonConvert.DeserializeObject<DeviceInfo>(row["DeviceInfo"].ToString()),
                        LocationInfo = JsonConvert.DeserializeObject<LocationInfo>(row["LocationInfo"].ToString()),
                        NetworkInfo = JsonConvert.DeserializeObject<NetworkInfo>(row["NetworkInfo"].ToString())
                    };

                    viewerDataList.Add(viewerData);
                }

                _logger.LogInformation($"获取到 {viewerDataList.Count} 条观众数据");
                return viewerDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取观众数据时发生异常: {roomId}");
                return new List<ViewerData>();
            }
        }

        /// <summary>
        /// 获取直播间统计数据
        /// </summary>
        /// <param name="roomId">直播间ID</param>
        /// <param name="date">统计日期</param>
        /// <returns>统计数据</returns>
        public async Task<LiveRoomStatistics> GetLiveRoomStatisticsAsync(string roomId, DateTime date)
        {
            try
            {
                var sql = @"
                    SELECT 
                        COUNT(DISTINCT v.UserId) as UniqueViewers,
                        COUNT(v.Id) as TotalViews,
                        AVG(CAST(vd.Duration as FLOAT)) as AvgWatchDuration,
                        MAX(vd.Duration) as MaxWatchDuration,
                        SUM(vd.InteractionCount) as TotalInteractions,
                        SUM(vd.ChatMessageCount) as TotalChatMessages,
                        SUM(vd.GiftCount) as TotalGifts,
                        SUM(vd.ShareCount) as TotalShares,
                        MAX(concurrent.MaxConcurrent) as PeakConcurrentViewers
                    FROM Viewers v
                    INNER JOIN ViewerDetails vd ON v.Id = vd.ViewerId
                    LEFT JOIN (
                        SELECT LiveRoomId, MAX(ConcurrentCount) as MaxConcurrent
                        FROM LiveRoomConcurrentStats 
                        WHERE LiveRoomId = @RoomId 
                          AND CAST(StatTime as DATE) = @Date
                        GROUP BY LiveRoomId
                    ) concurrent ON v.LiveRoomId = concurrent.LiveRoomId
                    WHERE v.LiveRoomId = @RoomId 
                      AND CAST(vd.JoinTime as DATE) = @Date";

                var parameters = new Dictionary<string, object>
                {
                    { "RoomId", roomId },
                    { "Date", date.Date }
                };

                var dataTable = await ExecuteQueryAsync(sql, parameters);
                
                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    return new LiveRoomStatistics
                    {
                        LiveRoomId = roomId,
                        StatDate = date.Date,
                        UniqueViewers = Convert.ToInt32(row["UniqueViewers"]),
                        TotalViews = Convert.ToInt32(row["TotalViews"]),
                        AvgWatchDuration = row["AvgWatchDuration"] == DBNull.Value ? 0 : Convert.ToDouble(row["AvgWatchDuration"]),
                        MaxWatchDuration = Convert.ToInt32(row["MaxWatchDuration"]),
                        TotalInteractions = Convert.ToInt32(row["TotalInteractions"]),
                        TotalChatMessages = Convert.ToInt32(row["TotalChatMessages"]),
                        TotalGifts = Convert.ToInt32(row["TotalGifts"]),
                        TotalShares = Convert.ToInt32(row["TotalShares"]),
                        PeakConcurrentViewers = row["PeakConcurrentViewers"] == DBNull.Value ? 0 : Convert.ToInt32(row["PeakConcurrentViewers"])
                    };
                }

                return new LiveRoomStatistics { LiveRoomId = roomId, StatDate = date.Date };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取直播间统计数据时发生异常: {roomId}");
                return new LiveRoomStatistics { LiveRoomId = roomId, StatDate = date.Date };
            }
        }

        /// <summary>
        /// 批量插入观众行为数据
        /// </summary>
        /// <param name="behaviorData">行为数据列表</param>
        /// <returns>插入结果</returns>
        public async Task<BatchInsertResult> BatchInsertViewerBehaviorAsync(List<ViewerBehavior> behaviorData)
        {
            var result = new BatchInsertResult();
            
            try
            {
                _logger.LogInformation($"开始批量插入观众行为数据，共 {behaviorData.Count} 条");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var sql = @"
                                INSERT INTO ViewerBehaviors 
                                (Id, ViewerId, LiveRoomId, BehaviorType, BehaviorTime, 
                                 BehaviorData, DeviceId, IpAddress, UserAgent, CreateTime)
                                VALUES 
                                (@Id, @ViewerId, @LiveRoomId, @BehaviorType, @BehaviorTime,
                                 @BehaviorData, @DeviceId, @IpAddress, @UserAgent, @CreateTime)";

                            foreach (var behavior in behaviorData)
                            {
                                using (var command = new SqlCommand(sql, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                                    command.Parameters.AddWithValue("@ViewerId", behavior.ViewerId);
                                    command.Parameters.AddWithValue("@LiveRoomId", behavior.LiveRoomId);
                                    command.Parameters.AddWithValue("@BehaviorType", (int)behavior.BehaviorType);
                                    command.Parameters.AddWithValue("@BehaviorTime", behavior.BehaviorTime);
                                    command.Parameters.AddWithValue("@BehaviorData", JsonConvert.SerializeObject(behavior.BehaviorData));
                                    command.Parameters.AddWithValue("@DeviceId", behavior.DeviceId ?? (object)DBNull.Value);
                                    command.Parameters.AddWithValue("@IpAddress", behavior.IpAddress ?? "");
                                    command.Parameters.AddWithValue("@UserAgent", behavior.UserAgent ?? "");
                                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);

                                    await command.ExecuteNonQueryAsync();
                                    result.SuccessCount++;
                                }
                            }

                            transaction.Commit();
                            result.Success = true;
                            result.Message = $"批量插入成功，共插入 {result.SuccessCount} 条数据";
                            
                            _logger.LogInformation($"批量插入观众行为数据完成，成功 {result.SuccessCount} 条");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            result.Success = false;
                            result.Message = $"批量插入失败: {ex.Message}";
                            
                            _logger.LogError(ex, "批量插入观众行为数据时发生异常");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量插入观众行为数据时发生异常");
                result.Success = false;
                result.Message = $"批量插入异常: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 获取异常观众行为数据
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <returns>异常行为数据</returns>
        public async Task<List<AbnormalBehavior>> GetAbnormalBehaviorsAsync(AbnormalBehaviorFilter filter)
        {
            try
            {
                var whereConditions = new List<string>();
                var parameters = new Dictionary<string, object>();

                // 构建查询条件
                if (!string.IsNullOrEmpty(filter.LiveRoomId))
                {
                    whereConditions.Add("ab.LiveRoomId = @LiveRoomId");
                    parameters.Add("LiveRoomId", filter.LiveRoomId);
                }

                if (filter.StartTime.HasValue)
                {
                    whereConditions.Add("ab.DetectedTime >= @StartTime");
                    parameters.Add("StartTime", filter.StartTime.Value);
                }

                if (filter.EndTime.HasValue)
                {
                    whereConditions.Add("ab.DetectedTime <= @EndTime");
                    parameters.Add("EndTime", filter.EndTime.Value);
                }

                if (filter.BehaviorTypes != null && filter.BehaviorTypes.Count > 0)
                {
                    var typeConditions = new List<string>();
                    for (int i = 0; i < filter.BehaviorTypes.Count; i++)
                    {
                        typeConditions.Add($"@BehaviorType{i}");
                        parameters.Add($"BehaviorType{i}", (int)filter.BehaviorTypes[i]);
                    }
                    whereConditions.Add($"ab.BehaviorType IN ({string.Join(",", typeConditions)})");
                }

                var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) : "";

                var sql = $@"
                    SELECT ab.*, v.UserName, v.Avatar, lr.RoomName, lr.StreamerName
                    FROM AbnormalBehaviors ab
                    LEFT JOIN Viewers v ON ab.ViewerId = v.Id
                    LEFT JOIN LiveRooms lr ON ab.LiveRoomId = lr.Id
                    {whereClause}
                    ORDER BY ab.DetectedTime DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                parameters.Add("Offset", (filter.PageIndex - 1) * filter.PageSize);
                parameters.Add("PageSize", filter.PageSize);

                var dataTable = await ExecuteQueryAsync(sql, parameters);
                var abnormalBehaviors = new List<AbnormalBehavior>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var abnormalBehavior = new AbnormalBehavior
                    {
                        Id = row["Id"].ToString(),
                        ViewerId = row["ViewerId"].ToString(),
                        LiveRoomId = row["LiveRoomId"].ToString(),
                        BehaviorType = (AbnormalBehaviorType)Convert.ToInt32(row["BehaviorType"]),
                        DetectedTime = Convert.ToDateTime(row["DetectedTime"]),
                        Severity = (SeverityLevel)Convert.ToInt32(row["Severity"]),
                        Description = row["Description"].ToString(),
                        Evidence = JsonConvert.DeserializeObject<Dictionary<string, object>>(row["Evidence"].ToString()),
                        Status = (ProcessStatus)Convert.ToInt32(row["Status"]),
                        UserName = row["UserName"]?.ToString(),
                        Avatar = row["Avatar"]?.ToString(),
                        RoomName = row["RoomName"]?.ToString(),
                        StreamerName = row["StreamerName"]?.ToString()
                    };

                    abnormalBehaviors.Add(abnormalBehavior);
                }

                _logger.LogInformation($"获取到 {abnormalBehaviors.Count} 条异常行为数据");
                return abnormalBehaviors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取异常观众行为数据时发生异常");
                return new List<AbnormalBehavior>();
            }
        }

        /// <summary>
        /// 更新观众状态
        /// </summary>
        /// <param name="viewerId">观众ID</param>
        /// <param name="status">新状态</param>
        /// <param name="reason">更新原因</param>
        /// <returns>更新结果</returns>
        public async Task<bool> UpdateViewerStatusAsync(string viewerId, ViewerStatus status, string reason)
        {
            try
            {
                var operations = new List<DatabaseOperation>
                {
                    new DatabaseOperation
                    {
                        Sql = @"
                            UPDATE Viewers 
                            SET Status = @Status, 
                                UpdateTime = @UpdateTime,
                                UpdateReason = @Reason
                            WHERE Id = @ViewerId",
                        Parameters = new Dictionary<string, object>
                        {
                            { "ViewerId", viewerId },
                            { "Status", (int)status },
                            { "UpdateTime", DateTime.Now },
                            { "Reason", reason ?? "" }
                        }
                    },
                    new DatabaseOperation
                    {
                        Sql = @"
                            INSERT INTO ViewerStatusLogs 
                            (Id, ViewerId, OldStatus, NewStatus, ChangeTime, Reason, CreatedBy)
                            VALUES 
                            (@Id, @ViewerId, @OldStatus, @NewStatus, @ChangeTime, @Reason, @CreatedBy)",
                        Parameters = new Dictionary<string, object>
                        {
                            { "Id", Guid.NewGuid() },
                            { "ViewerId", viewerId },
                            { "OldStatus", DBNull.Value }, // 这里应该先查询原状态
                            { "NewStatus", (int)status },
                            { "ChangeTime", DateTime.Now },
                            { "Reason", reason ?? "" },
                            { "CreatedBy", "system" }
                        }
                    }
                };

                var result = await ExecuteTransactionAsync(operations);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新观众状态时发生异常: {viewerId}");
                return false;
            }
        }

        /// <summary>
        /// 检查数据库连接
        /// </summary>
        /// <returns>连接状态</returns>
        public async Task<DatabaseConnectionStatus> CheckConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = "SELECT @@VERSION as Version, GETDATE() as ServerTime";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new DatabaseConnectionStatus
                                {
                                    IsConnected = true,
                                    ServerVersion = reader["Version"].ToString(),
                                    ServerTime = Convert.ToDateTime(reader["ServerTime"]),
                                    ConnectionString = _connectionString.Replace(_configuration.GetConnectionString("Password"), "****"),
                                    Message = "数据库连接正常"
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查数据库连接时发生异常");
                return new DatabaseConnectionStatus
                {
                    IsConnected = false,
                    Message = $"数据库连接失败: {ex.Message}"
                };
            }

            return new DatabaseConnectionStatus
            {
                IsConnected = false,
                Message = "未知错误"
            };
        }

        /// <summary>
        /// 执行数据库备份
        /// </summary>
        /// <param name="backupPath">备份路径</param>
        /// <returns>备份结果</returns>
        public async Task<DatabaseBackupResult> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                _logger.LogInformation($"开始数据库备份到: {backupPath}");

                var databaseName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
                var sql = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = @BackupPath 
                    WITH FORMAT, INIT, 
                    NAME = N'{databaseName}-Full Database Backup', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                var parameters = new Dictionary<string, object>
                {
                    { "BackupPath", backupPath }
                };

                await ExecuteNonQueryAsync(sql, parameters);

                _logger.LogInformation("数据库备份完成");
                return new DatabaseBackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    BackupTime = DateTime.Now,
                    Message = "数据库备份成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据库备份时发生异常: {backupPath}");
                return new DatabaseBackupResult
                {
                    Success = false,
                    Message = $"数据库备份失败: {ex.Message}"
                };
            }
        }
    }
}
