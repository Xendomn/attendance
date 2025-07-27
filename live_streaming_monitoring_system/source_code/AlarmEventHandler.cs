using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LiveStreamingMonitoringSystem.Core
{
    /// <summary>
    /// 报警事件处理器 - 负责处理直播平台观众监控的报警事件
    /// </summary>
    public class AlarmEventHandler : IAlarmEventHandler
    {
        private readonly ILogger<AlarmEventHandler> _logger;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly string _connectionString;
        private readonly IDeviceManager _deviceManager;
        private readonly INotificationService _notificationService;

        public AlarmEventHandler(
            ILogger<AlarmEventHandler> logger,
            IHubContext<MonitoringHub> hubContext,
            IConfiguration configuration,
            IDeviceManager deviceManager,
            INotificationService notificationService)
        {
            _logger = logger;
            _hubContext = hubContext;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _deviceManager = deviceManager;
            _notificationService = notificationService;
        }

        /// <summary>
        /// 添加报警事件
        /// </summary>
        /// <param name="alarmEvent">报警事件对象</param>
        /// <returns>添加结果</returns>
        public async Task<AlarmEventResult> AddAlarmEventAsync(AlarmEvent alarmEvent)
        {
            try
            {
                _logger.LogInformation($"开始添加报警事件: {alarmEvent.EventType} - {alarmEvent.Description}");

                // 验证报警事件数据
                var validationResult = ValidateAlarmEvent(alarmEvent);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning($"报警事件验证失败: {validationResult.ErrorMessage}");
                    return new AlarmEventResult { Success = false, Message = validationResult.ErrorMessage };
                }

                // 设置事件基本信息
                alarmEvent.Id = Guid.NewGuid();
                alarmEvent.CreateTime = DateTime.Now;
                alarmEvent.Status = AlarmStatus.Active;
                alarmEvent.ProcessStatus = ProcessStatus.Pending;

                // 保存到数据库
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 插入主表
                            var insertSql = @"
                                INSERT INTO AlarmEvents 
                                (Id, EventType, Level, Title, Description, LiveRoomId, ViewerId, 
                                 DeviceId, CreateTime, Status, ProcessStatus, Location, 
                                 AdditionalData, CreatedBy)
                                VALUES 
                                (@Id, @EventType, @Level, @Title, @Description, @LiveRoomId, @ViewerId,
                                 @DeviceId, @CreateTime, @Status, @ProcessStatus, @Location,
                                 @AdditionalData, @CreatedBy)";

                            using (var command = new SqlCommand(insertSql, connection, transaction))
                            {
                                AddAlarmEventParameters(command, alarmEvent);
                                await command.ExecuteNonQueryAsync();
                            }

                            // 插入事件详情表
                            await InsertAlarmEventDetailsAsync(connection, transaction, alarmEvent);

                            // 更新统计信息
                            await UpdateAlarmStatisticsAsync(connection, transaction, alarmEvent);

                            transaction.Commit();
                            _logger.LogInformation($"报警事件添加成功: {alarmEvent.Id}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"添加报警事件时发生数据库错误: {alarmEvent.Id}");
                            throw;
                        }
                    }
                }

                // 触发实时通知
                await TriggerRealTimeNotificationAsync(alarmEvent);

                // 执行自动处理规则
                await ExecuteAutoProcessingRulesAsync(alarmEvent);

                // 发送外部通知
                await SendExternalNotificationsAsync(alarmEvent);

                return new AlarmEventResult 
                { 
                    Success = true, 
                    Message = "报警事件添加成功", 
                    EventId = alarmEvent.Id 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加报警事件时发生异常: {alarmEvent?.EventType}");
                return new AlarmEventResult 
                { 
                    Success = false, 
                    Message = $"添加报警事件失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 删除报警事件
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="deleteReason">删除原因</param>
        /// <returns>删除结果</returns>
        public async Task<AlarmEventResult> DeleteAlarmEventAsync(Guid eventId, string deleteReason)
        {
            try
            {
                _logger.LogInformation($"开始删除报警事件: {eventId}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 检查事件是否存在
                            var existsQuery = "SELECT COUNT(1) FROM AlarmEvents WHERE Id = @Id";
                            using (var checkCommand = new SqlCommand(existsQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@Id", eventId);
                                var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
                                
                                if (!exists)
                                {
                                    return new AlarmEventResult 
                                    { 
                                        Success = false, 
                                        Message = "报警事件不存在" 
                                    };
                                }
                            }

                            // 软删除 - 更新状态而不是物理删除
                            var updateSql = @"
                                UPDATE AlarmEvents 
                                SET Status = @Status, 
                                    ProcessStatus = @ProcessStatus,
                                    DeleteTime = @DeleteTime,
                                    DeleteReason = @DeleteReason,
                                    DeletedBy = @DeletedBy
                                WHERE Id = @Id";

                            using (var command = new SqlCommand(updateSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", eventId);
                                command.Parameters.AddWithValue("@Status", (int)AlarmStatus.Deleted);
                                command.Parameters.AddWithValue("@ProcessStatus", (int)ProcessStatus.Deleted);
                                command.Parameters.AddWithValue("@DeleteTime", DateTime.Now);
                                command.Parameters.AddWithValue("@DeleteReason", deleteReason ?? "");
                                command.Parameters.AddWithValue("@DeletedBy", GetCurrentUserId());
                                
                                await command.ExecuteNonQueryAsync();
                            }

                            // 记录删除日志
                            await InsertAlarmEventLogAsync(connection, transaction, eventId, 
                                AlarmEventAction.Delete, deleteReason);

                            // 更新统计信息
                            await UpdateAlarmStatisticsAfterDeleteAsync(connection, transaction, eventId);

                            transaction.Commit();
                            _logger.LogInformation($"报警事件删除成功: {eventId}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"删除报警事件时发生数据库错误: {eventId}");
                            throw;
                        }
                    }
                }

                // 发送删除通知
                await _hubContext.Clients.All.SendAsync("AlarmEventDeleted", eventId);

                return new AlarmEventResult 
                { 
                    Success = true, 
                    Message = "报警事件删除成功" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除报警事件时发生异常: {eventId}");
                return new AlarmEventResult 
                { 
                    Success = false, 
                    Message = $"删除报警事件失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 更新报警事件
        /// </summary>
        /// <param name="alarmEvent">更新的报警事件对象</param>
        /// <returns>更新结果</returns>
        public async Task<AlarmEventResult> UpdateAlarmEventAsync(AlarmEvent alarmEvent)
        {
            try
            {
                _logger.LogInformation($"开始更新报警事件: {alarmEvent.Id}");

                // 验证报警事件数据
                var validationResult = ValidateAlarmEvent(alarmEvent);
                if (!validationResult.IsValid)
                {
                    return new AlarmEventResult { Success = false, Message = validationResult.ErrorMessage };
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 获取原始事件信息
                            var originalEvent = await GetAlarmEventByIdAsync(connection, transaction, alarmEvent.Id);
                            if (originalEvent == null)
                            {
                                return new AlarmEventResult 
                                { 
                                    Success = false, 
                                    Message = "报警事件不存在" 
                                };
                            }

                            // 更新主表
                            var updateSql = @"
                                UPDATE AlarmEvents 
                                SET EventType = @EventType,
                                    Level = @Level,
                                    Title = @Title,
                                    Description = @Description,
                                    Status = @Status,
                                    ProcessStatus = @ProcessStatus,
                                    Location = @Location,
                                    AdditionalData = @AdditionalData,
                                    UpdateTime = @UpdateTime,
                                    UpdatedBy = @UpdatedBy
                                WHERE Id = @Id";

                            using (var command = new SqlCommand(updateSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", alarmEvent.Id);
                                command.Parameters.AddWithValue("@EventType", (int)alarmEvent.EventType);
                                command.Parameters.AddWithValue("@Level", (int)alarmEvent.Level);
                                command.Parameters.AddWithValue("@Title", alarmEvent.Title ?? "");
                                command.Parameters.AddWithValue("@Description", alarmEvent.Description ?? "");
                                command.Parameters.AddWithValue("@Status", (int)alarmEvent.Status);
                                command.Parameters.AddWithValue("@ProcessStatus", (int)alarmEvent.ProcessStatus);
                                command.Parameters.AddWithValue("@Location", alarmEvent.Location ?? "");
                                command.Parameters.AddWithValue("@AdditionalData", 
                                    JsonConvert.SerializeObject(alarmEvent.AdditionalData) ?? "{}");
                                command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                command.Parameters.AddWithValue("@UpdatedBy", GetCurrentUserId());
                                
                                await command.ExecuteNonQueryAsync();
                            }

                            // 记录变更日志
                            await InsertAlarmEventChangeLogAsync(connection, transaction, 
                                originalEvent, alarmEvent);

                            // 如果状态发生变化，更新统计信息
                            if (originalEvent.Status != alarmEvent.Status || 
                                originalEvent.ProcessStatus != alarmEvent.ProcessStatus)
                            {
                                await UpdateAlarmStatisticsAfterStatusChangeAsync(connection, transaction, 
                                    originalEvent, alarmEvent);
                            }

                            transaction.Commit();
                            _logger.LogInformation($"报警事件更新成功: {alarmEvent.Id}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"更新报警事件时发生数据库错误: {alarmEvent.Id}");
                            throw;
                        }
                    }
                }

                // 发送更新通知
                await _hubContext.Clients.All.SendAsync("AlarmEventUpdated", alarmEvent);

                // 如果是处理状态变更，执行相应的后续操作
                if (alarmEvent.ProcessStatus == ProcessStatus.Resolved)
                {
                    await HandleAlarmEventResolvedAsync(alarmEvent);
                }

                return new AlarmEventResult 
                { 
                    Success = true, 
                    Message = "报警事件更新成功" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新报警事件时发生异常: {alarmEvent.Id}");
                return new AlarmEventResult 
                { 
                    Success = false, 
                    Message = $"更新报警事件失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 获取报警事件列表
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <returns>报警事件列表</returns>
        public async Task<PagedResult<AlarmEvent>> GetAlarmEventsAsync(AlarmEventFilter filter)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 构建查询条件
                    var whereClause = BuildWhereClause(filter);
                    var orderClause = BuildOrderClause(filter.SortField, filter.SortDirection);

                    // 查询总数
                    var countSql = $"SELECT COUNT(1) FROM AlarmEvents WHERE {whereClause}";
                    var totalCount = 0;
                    
                    using (var countCommand = new SqlCommand(countSql, connection))
                    {
                        AddFilterParameters(countCommand, filter);
                        totalCount = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // 查询数据
                    var dataSql = $@"
                        SELECT * FROM (
                            SELECT *, ROW_NUMBER() OVER ({orderClause}) as RowNum
                            FROM AlarmEvents 
                            WHERE {whereClause}
                        ) AS PagedResults
                        WHERE RowNum BETWEEN @StartRow AND @EndRow";

                    var events = new List<AlarmEvent>();
                    using (var dataCommand = new SqlCommand(dataSql, connection))
                    {
                        AddFilterParameters(dataCommand, filter);
                        dataCommand.Parameters.AddWithValue("@StartRow", 
                            (filter.PageIndex - 1) * filter.PageSize + 1);
                        dataCommand.Parameters.AddWithValue("@EndRow", 
                            filter.PageIndex * filter.PageSize);

                        using (var reader = await dataCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                events.Add(MapAlarmEventFromReader(reader));
                            }
                        }
                    }

                    return new PagedResult<AlarmEvent>
                    {
                        Data = events,
                        TotalCount = totalCount,
                        PageIndex = filter.PageIndex,
                        PageSize = filter.PageSize,
                        TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报警事件列表时发生异常");
                return new PagedResult<AlarmEvent>
                {
                    Data = new List<AlarmEvent>(),
                    TotalCount = 0,
                    PageIndex = filter.PageIndex,
                    PageSize = filter.PageSize,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// 批量处理报警事件
        /// </summary>
        /// <param name="eventIds">事件ID列表</param>
        /// <param name="action">处理动作</param>
        /// <param name="processNote">处理备注</param>
        /// <returns>处理结果</returns>
        public async Task<BatchProcessResult> BatchProcessAlarmEventsAsync(
            List<Guid> eventIds, AlarmEventAction action, string processNote)
        {
            var result = new BatchProcessResult();
            
            try
            {
                _logger.LogInformation($"开始批量处理报警事件: {eventIds.Count} 个事件, 动作: {action}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var eventId in eventIds)
                            {
                                try
                                {
                                    await ProcessSingleAlarmEventAsync(connection, transaction, 
                                        eventId, action, processNote);
                                    result.SuccessCount++;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"处理报警事件失败: {eventId}");
                                    result.FailureCount++;
                                    result.FailureDetails.Add(new BatchProcessFailure
                                    {
                                        EventId = eventId,
                                        ErrorMessage = ex.Message
                                    });
                                }
                            }

                            transaction.Commit();
                            _logger.LogInformation($"批量处理完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "批量处理报警事件时发生数据库错误");
                            throw;
                        }
                    }
                }

                // 发送批量处理通知
                await _hubContext.Clients.All.SendAsync("AlarmEventsBatchProcessed", new
                {
                    Action = action,
                    SuccessCount = result.SuccessCount,
                    FailureCount = result.FailureCount,
                    EventIds = eventIds
                });

                result.Success = result.FailureCount == 0;
                result.Message = result.Success ? "批量处理成功" : 
                    $"批量处理完成，成功 {result.SuccessCount} 个，失败 {result.FailureCount} 个";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理报警事件时发生异常");
                result.Success = false;
                result.Message = $"批量处理失败: {ex.Message}";
                return result;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 验证报警事件数据
        /// </summary>
        private ValidationResult ValidateAlarmEvent(AlarmEvent alarmEvent)
        {
            if (alarmEvent == null)
                return new ValidationResult { IsValid = false, ErrorMessage = "报警事件对象不能为空" };

            if (string.IsNullOrWhiteSpace(alarmEvent.Title))
                return new ValidationResult { IsValid = false, ErrorMessage = "报警事件标题不能为空" };

            if (string.IsNullOrWhiteSpace(alarmEvent.Description))
                return new ValidationResult { IsValid = false, ErrorMessage = "报警事件描述不能为空" };

            if (alarmEvent.Level == AlarmLevel.Unknown)
                return new ValidationResult { IsValid = false, ErrorMessage = "报警级别不能为未知" };

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 添加报警事件参数
        /// </summary>
        private void AddAlarmEventParameters(SqlCommand command, AlarmEvent alarmEvent)
        {
            command.Parameters.AddWithValue("@Id", alarmEvent.Id);
            command.Parameters.AddWithValue("@EventType", (int)alarmEvent.EventType);
            command.Parameters.AddWithValue("@Level", (int)alarmEvent.Level);
            command.Parameters.AddWithValue("@Title", alarmEvent.Title ?? "");
            command.Parameters.AddWithValue("@Description", alarmEvent.Description ?? "");
            command.Parameters.AddWithValue("@LiveRoomId", alarmEvent.LiveRoomId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ViewerId", alarmEvent.ViewerId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DeviceId", alarmEvent.DeviceId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreateTime", alarmEvent.CreateTime);
            command.Parameters.AddWithValue("@Status", (int)alarmEvent.Status);
            command.Parameters.AddWithValue("@ProcessStatus", (int)alarmEvent.ProcessStatus);
            command.Parameters.AddWithValue("@Location", alarmEvent.Location ?? "");
            command.Parameters.AddWithValue("@AdditionalData", 
                JsonConvert.SerializeObject(alarmEvent.AdditionalData) ?? "{}");
            command.Parameters.AddWithValue("@CreatedBy", GetCurrentUserId());
        }

        /// <summary>
        /// 插入报警事件详情
        /// </summary>
        private async Task InsertAlarmEventDetailsAsync(SqlConnection connection, 
            SqlTransaction transaction, AlarmEvent alarmEvent)
        {
            var detailsSql = @"
                INSERT INTO AlarmEventDetails 
                (Id, EventId, DetailType, DetailValue, CreateTime)
                VALUES (@Id, @EventId, @DetailType, @DetailValue, @CreateTime)";

            // 插入设备信息详情
            if (!string.IsNullOrEmpty(alarmEvent.DeviceId))
            {
                var deviceInfo = await _deviceManager.GetDeviceInfoAsync(alarmEvent.DeviceId);
                if (deviceInfo != null)
                {
                    using (var command = new SqlCommand(detailsSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                        command.Parameters.AddWithValue("@EventId", alarmEvent.Id);
                        command.Parameters.AddWithValue("@DetailType", "DeviceInfo");
                        command.Parameters.AddWithValue("@DetailValue", JsonConvert.SerializeObject(deviceInfo));
                        command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            // 插入直播间信息详情
            if (!string.IsNullOrEmpty(alarmEvent.LiveRoomId))
            {
                var roomInfo = await GetLiveRoomInfoAsync(alarmEvent.LiveRoomId);
                if (roomInfo != null)
                {
                    using (var command = new SqlCommand(detailsSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                        command.Parameters.AddWithValue("@EventId", alarmEvent.Id);
                        command.Parameters.AddWithValue("@DetailType", "LiveRoomInfo");
                        command.Parameters.AddWithValue("@DetailValue", JsonConvert.SerializeObject(roomInfo));
                        command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 触发实时通知
        /// </summary>
        private async Task TriggerRealTimeNotificationAsync(AlarmEvent alarmEvent)
        {
            try
            {
                // 发送SignalR通知
                await _hubContext.Clients.All.SendAsync("NewAlarmEvent", alarmEvent);

                // 根据报警级别发送不同的通知
                switch (alarmEvent.Level)
                {
                    case AlarmLevel.Critical:
                        await _hubContext.Clients.Group("Administrators").SendAsync("CriticalAlarm", alarmEvent);
                        break;
                    case AlarmLevel.High:
                        await _hubContext.Clients.Group("Managers").SendAsync("HighPriorityAlarm", alarmEvent);
                        break;
                    case AlarmLevel.Medium:
                        await _hubContext.Clients.Group("Operators").SendAsync("MediumPriorityAlarm", alarmEvent);
                        break;
                }

                _logger.LogInformation($"实时通知发送成功: {alarmEvent.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送实时通知时发生异常: {alarmEvent.Id}");
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            // 这里应该从当前上下文中获取用户ID
            // 简化实现，实际应该从HttpContext或其他认证上下文获取
            return "system";
        }

        /// <summary>
        /// 从数据读取器映射报警事件对象
        /// </summary>
        private AlarmEvent MapAlarmEventFromReader(SqlDataReader reader)
        {
            return new AlarmEvent
            {
                Id = reader.GetGuid("Id"),
                EventType = (AlarmEventType)reader.GetInt32("EventType"),
                Level = (AlarmLevel)reader.GetInt32("Level"),
                Title = reader.GetString("Title"),
                Description = reader.GetString("Description"),
                LiveRoomId = reader.IsDBNull("LiveRoomId") ? null : reader.GetString("LiveRoomId"),
                ViewerId = reader.IsDBNull("ViewerId") ? null : reader.GetString("ViewerId"),
                DeviceId = reader.IsDBNull("DeviceId") ? null : reader.GetString("DeviceId"),
                CreateTime = reader.GetDateTime("CreateTime"),
                Status = (AlarmStatus)reader.GetInt32("Status"),
                ProcessStatus = (ProcessStatus)reader.GetInt32("ProcessStatus"),
                Location = reader.IsDBNull("Location") ? "" : reader.GetString("Location"),
                AdditionalData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    reader.IsDBNull("AdditionalData") ? "{}" : reader.GetString("AdditionalData"))
            };
        }

        #endregion
    }
}
