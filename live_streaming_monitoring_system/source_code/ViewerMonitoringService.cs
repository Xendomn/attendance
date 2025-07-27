using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LiveStreamingMonitoringSystem.Services
{
    /// <summary>
    /// 观众监控服务 - 负责实时监控直播观众行为和状态
    /// </summary>
    public class ViewerMonitoringService : IViewerMonitoringService
    {
        private readonly ILogger<ViewerMonitoringService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly IAlarmEventHandler _alarmEventHandler;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, ViewerSession> _activeSessions;
        private readonly Timer _monitoringTimer;

        public ViewerMonitoringService(
            ILogger<ViewerMonitoringService> logger,
            IDatabaseService databaseService,
            IHubContext<MonitoringHub> hubContext,
            IAlarmEventHandler alarmEventHandler,
            IConfiguration configuration)
        {
            _logger = logger;
            _databaseService = databaseService;
            _hubContext = hubContext;
            _alarmEventHandler = alarmEventHandler;
            _configuration = configuration;
            _activeSessions = new Dictionary<string, ViewerSession>();
            
            // 启动定时监控任务
            var monitoringInterval = TimeSpan.FromSeconds(
                _configuration.GetValue<int>("MonitoringSettings:IntervalSeconds", 30));
            _monitoringTimer = new Timer(ExecuteMonitoringTask, null, TimeSpan.Zero, monitoringInterval);
        }

        /// <summary>
        /// 开始监控观众
        /// </summary>
        /// <param name="viewerId">观众ID</param>
        /// <param name="liveRoomId">直播间ID</param>
        /// <param name="deviceInfo">设备信息</param>
        /// <returns>监控结果</returns>
        public async Task<MonitoringResult> StartMonitoringAsync(string viewerId, string liveRoomId, DeviceInfo deviceInfo)
        {
            try
            {
                _logger.LogInformation($"开始监控观众: {viewerId} 在直播间: {liveRoomId}");

                // 创建观众会话
                var session = new ViewerSession
                {
                    ViewerId = viewerId,
                    LiveRoomId = liveRoomId,
                    DeviceInfo = deviceInfo,
                    StartTime = DateTime.Now,
                    LastActivityTime = DateTime.Now,
                    Status = ViewerStatus.Online,
                    BehaviorHistory = new List<ViewerBehavior>(),
                    RiskScore = 0,
                    Warnings = new List<string>()
                };

                // 添加到活跃会话
                lock (_activeSessions)
                {
                    _activeSessions[viewerId] = session;
                }

                // 记录观众进入事件
                await RecordViewerEventAsync(viewerId, liveRoomId, ViewerEventType.Enter, deviceInfo);

                // 初始化监控规则
                await InitializeMonitoringRulesAsync(viewerId, liveRoomId);

                // 发送实时通知
                await _hubContext.Clients.Group($"Room_{liveRoomId}").SendAsync("ViewerJoined", new
                {
                    ViewerId = viewerId,
                    JoinTime = session.StartTime,
                    DeviceInfo = deviceInfo
                });

                _logger.LogInformation($"观众监控启动成功: {viewerId}");
                return new MonitoringResult { Success = true, Message = "监控启动成功" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"启动观众监控时发生异常: {viewerId}");
                return new MonitoringResult { Success = false, Message = $"监控启动失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 停止监控观众
        /// </summary>
        /// <param name="viewerId">观众ID</param>
        /// <param name="reason">停止原因</param>
        /// <returns>停止结果</returns>
        public async Task<MonitoringResult> StopMonitoringAsync(string viewerId, string reason = "正常离开")
        {
            try
            {
                _logger.LogInformation($"停止监控观众: {viewerId}, 原因: {reason}");

                ViewerSession session = null;
                lock (_activeSessions)
                {
                    if (_activeSessions.TryGetValue(viewerId, out session))
                    {
                        _activeSessions.Remove(viewerId);
                    }
                }

                if (session != null)
                {
                    // 计算观看时长
                    var watchDuration = DateTime.Now - session.StartTime;
                    
                    // 记录观众离开事件
                    await RecordViewerEventAsync(viewerId, session.LiveRoomId, ViewerEventType.Leave, 
                        new { Reason = reason, Duration = watchDuration.TotalSeconds });

                    // 更新观众统计信息
                    await UpdateViewerStatisticsAsync(session);

                    // 发送实时通知
                    await _hubContext.Clients.Group($"Room_{session.LiveRoomId}").SendAsync("ViewerLeft", new
                    {
                        ViewerId = viewerId,
                        LeaveTime = DateTime.Now,
                        Duration = watchDuration.TotalSeconds,
                        Reason = reason
                    });

                    _logger.LogInformation($"观众监控停止成功: {viewerId}, 观看时长: {watchDuration.TotalMinutes:F2}分钟");
                }

                return new MonitoringResult { Success = true, Message = "监控停止成功" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"停止观众监控时发生异常: {viewerId}");
                return new MonitoringResult { Success = false, Message = $"监控停止失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 记录观众行为
        /// </summary>
        /// <param name="viewerId">观众ID</param>
        /// <param name="behaviorType">行为类型</param>
        /// <param name="behaviorData">行为数据</param>
        /// <returns>记录结果</returns>
        public async Task<BehaviorRecordResult> RecordBehaviorAsync(string viewerId, ViewerBehaviorType behaviorType, object behaviorData)
        {
            try
            {
                _logger.LogDebug($"记录观众行为: {viewerId} - {behaviorType}");

                ViewerSession session = null;
                lock (_activeSessions)
                {
                    _activeSessions.TryGetValue(viewerId, out session);
                }

                if (session == null)
                {
                    _logger.LogWarning($"观众会话不存在: {viewerId}");
                    return new BehaviorRecordResult { Success = false, Message = "观众会话不存在" };
                }

                // 创建行为记录
                var behavior = new ViewerBehavior
                {
                    ViewerId = viewerId,
                    LiveRoomId = session.LiveRoomId,
                    BehaviorType = behaviorType,
                    BehaviorTime = DateTime.Now,
                    BehaviorData = behaviorData,
                    DeviceId = session.DeviceInfo?.Id,
                    IpAddress = session.DeviceInfo?.IpAddress,
                    UserAgent = session.DeviceInfo?.UserAgent
                };

                // 添加到会话历史
                session.BehaviorHistory.Add(behavior);
                session.LastActivityTime = DateTime.Now;

                // 异步保存到数据库
                _ = Task.Run(async () => await SaveBehaviorToDatabase(behavior));

                // 分析行为风险
                var riskAnalysis = await AnalyzeBehaviorRiskAsync(session, behavior);
                if (riskAnalysis.IsRisky)
                {
                    await HandleRiskyBehaviorAsync(session, behavior, riskAnalysis);
                }

                // 更新实时统计
                await UpdateRealTimeStatisticsAsync(session.LiveRoomId, behaviorType);

                return new BehaviorRecordResult { Success = true, Message = "行为记录成功" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"记录观众行为时发生异常: {viewerId} - {behaviorType}");
                return new BehaviorRecordResult { Success = false, Message = $"行为记录失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 获取观众实时状态
        /// </summary>
        /// <param name="viewerId">观众ID</param>
        /// <returns>观众状态</returns>
        public async Task<ViewerRealTimeStatus> GetViewerRealTimeStatusAsync(string viewerId)
        {
            try
            {
                ViewerSession session = null;
                lock (_activeSessions)
                {
                    _activeSessions.TryGetValue(viewerId, out session);
                }

                if (session == null)
                {
                    return new ViewerRealTimeStatus
                    {
                        ViewerId = viewerId,
                        IsOnline = false,
                        Status = ViewerStatus.Offline
                    };
                }

                // 计算观看时长
                var watchDuration = DateTime.Now - session.StartTime;
                var inactiveTime = DateTime.Now - session.LastActivityTime;

                // 获取最近行为统计
                var recentBehaviors = session.BehaviorHistory
                    .Where(b => b.BehaviorTime >= DateTime.Now.AddMinutes(-5))
                    .GroupBy(b => b.BehaviorType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new ViewerRealTimeStatus
                {
                    ViewerId = viewerId,
                    LiveRoomId = session.LiveRoomId,
                    IsOnline = true,
                    Status = session.Status,
                    JoinTime = session.StartTime,
                    WatchDuration = watchDuration,
                    LastActivityTime = session.LastActivityTime,
                    InactiveTime = inactiveTime,
                    RiskScore = session.RiskScore,
                    DeviceInfo = session.DeviceInfo,
                    RecentBehaviors = recentBehaviors,
                    Warnings = session.Warnings.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取观众实时状态时发生异常: {viewerId}");
                return new ViewerRealTimeStatus
                {
                    ViewerId = viewerId,
                    IsOnline = false,
                    Status = ViewerStatus.Error
                };
            }
        }

        /// <summary>
        /// 获取直播间观众列表
        /// </summary>
        /// <param name="liveRoomId">直播间ID</param>
        /// <returns>观众列表</returns>
        public async Task<List<ViewerRealTimeStatus>> GetLiveRoomViewersAsync(string liveRoomId)
        {
            try
            {
                var viewers = new List<ViewerRealTimeStatus>();

                lock (_activeSessions)
                {
                    var roomSessions = _activeSessions.Values
                        .Where(s => s.LiveRoomId == liveRoomId)
                        .ToList();

                    foreach (var session in roomSessions)
                    {
                        var watchDuration = DateTime.Now - session.StartTime;
                        var inactiveTime = DateTime.Now - session.LastActivityTime;

                        var recentBehaviors = session.BehaviorHistory
                            .Where(b => b.BehaviorTime >= DateTime.Now.AddMinutes(-5))
                            .GroupBy(b => b.BehaviorType)
                            .ToDictionary(g => g.Key, g => g.Count());

                        viewers.Add(new ViewerRealTimeStatus
                        {
                            ViewerId = session.ViewerId,
                            LiveRoomId = session.LiveRoomId,
                            IsOnline = true,
                            Status = session.Status,
                            JoinTime = session.StartTime,
                            WatchDuration = watchDuration,
                            LastActivityTime = session.LastActivityTime,
                            InactiveTime = inactiveTime,
                            RiskScore = session.RiskScore,
                            DeviceInfo = session.DeviceInfo,
                            RecentBehaviors = recentBehaviors,
                            Warnings = session.Warnings.ToList()
                        });
                    }
                }

                _logger.LogInformation($"获取直播间观众列表: {liveRoomId}, 在线观众数: {viewers.Count}");
                return viewers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取直播间观众列表时发生异常: {liveRoomId}");
                return new List<ViewerRealTimeStatus>();
            }
        }

        /// <summary>
        /// 获取监控统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<MonitoringStatistics> GetMonitoringStatisticsAsync()
        {
            try
            {
                var statistics = new MonitoringStatistics();

                lock (_activeSessions)
                {
                    statistics.TotalOnlineViewers = _activeSessions.Count;
                    statistics.ActiveLiveRooms = _activeSessions.Values
                        .Select(s => s.LiveRoomId)
                        .Distinct()
                        .Count();

                    // 按状态统计
                    var statusGroups = _activeSessions.Values
                        .GroupBy(s => s.Status)
                        .ToDictionary(g => g.Key, g => g.Count());

                    statistics.ViewersByStatus = statusGroups;

                    // 风险等级统计
                    statistics.HighRiskViewers = _activeSessions.Values
                        .Count(s => s.RiskScore >= 80);
                    statistics.MediumRiskViewers = _activeSessions.Values
                        .Count(s => s.RiskScore >= 50 && s.RiskScore < 80);
                    statistics.LowRiskViewers = _activeSessions.Values
                        .Count(s => s.RiskScore < 50);

                    // 平均观看时长
                    if (_activeSessions.Count > 0)
                    {
                        statistics.AverageWatchDuration = _activeSessions.Values
                            .Average(s => (DateTime.Now - s.StartTime).TotalMinutes);
                    }
                }

                // 获取今日统计数据
                var todayStats = await GetTodayStatisticsAsync();
                statistics.TodayTotalViewers = todayStats.TotalViewers;
                statistics.TodayPeakConcurrent = todayStats.PeakConcurrent;
                statistics.TodayAverageWatchTime = todayStats.AverageWatchTime;

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监控统计信息时发生异常");
                return new MonitoringStatistics();
            }
        }

        /// <summary>
        /// 执行批量观众操作
        /// </summary>
        /// <param name="viewerIds">观众ID列表</param>
        /// <param name="operation">操作类型</param>
        /// <param name="parameters">操作参数</param>
        /// <returns>操作结果</returns>
        public async Task<BatchOperationResult> BatchOperateViewersAsync(
            List<string> viewerIds, ViewerOperation operation, Dictionary<string, object> parameters)
        {
            var result = new BatchOperationResult();

            try
            {
                _logger.LogInformation($"开始批量操作观众: {viewerIds.Count} 个观众, 操作: {operation}");

                foreach (var viewerId in viewerIds)
                {
                    try
                    {
                        switch (operation)
                        {
                            case ViewerOperation.Block:
                                await BlockViewerAsync(viewerId, parameters?.GetValueOrDefault("reason")?.ToString());
                                break;
                            case ViewerOperation.Unblock:
                                await UnblockViewerAsync(viewerId, parameters?.GetValueOrDefault("reason")?.ToString());
                                break;
                            case ViewerOperation.Warn:
                                await WarnViewerAsync(viewerId, parameters?.GetValueOrDefault("message")?.ToString());
                                break;
                            case ViewerOperation.Kick:
                                await KickViewerAsync(viewerId, parameters?.GetValueOrDefault("reason")?.ToString());
                                break;
                            default:
                                throw new ArgumentException($"不支持的操作类型: {operation}");
                        }

                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"批量操作观众失败: {viewerId}");
                        result.FailureCount++;
                        result.FailureDetails.Add(new BatchOperationFailure
                        {
                            ViewerId = viewerId,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                result.Success = result.FailureCount == 0;
                result.Message = result.Success ? "批量操作成功" : 
                    $"批量操作完成，成功 {result.SuccessCount} 个，失败 {result.FailureCount} 个";

                _logger.LogInformation($"批量操作完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量操作观众时发生异常");
                result.Success = false;
                result.Message = $"批量操作失败: {ex.Message}";
                return result;
            }
        }

        #region 私有方法

        /// <summary>
        /// 执行监控任务
        /// </summary>
        private async void ExecuteMonitoringTask(object state)
        {
            try
            {
                await CheckInactiveViewersAsync();
                await DetectAbnormalBehaviorsAsync();
                await UpdateRealTimeMetricsAsync();
                await CleanupExpiredSessionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行监控任务时发生异常");
            }
        }

        /// <summary>
        /// 检查不活跃观众
        /// </summary>
        private async Task CheckInactiveViewersAsync()
        {
            var inactiveThreshold = TimeSpan.FromMinutes(
                _configuration.GetValue<int>("MonitoringSettings:InactiveThresholdMinutes", 10));

            var inactiveViewers = new List<string>();

            lock (_activeSessions)
            {
                var now = DateTime.Now;
                foreach (var session in _activeSessions.Values)
                {
                    if (now - session.LastActivityTime > inactiveThreshold)
                    {
                        inactiveViewers.Add(session.ViewerId);
                    }
                }
            }

            foreach (var viewerId in inactiveViewers)
            {
                await HandleInactiveViewerAsync(viewerId);
            }
        }

        /// <summary>
        /// 检测异常行为
        /// </summary>
        private async Task DetectAbnormalBehaviorsAsync()
        {
            lock (_activeSessions)
            {
                foreach (var session in _activeSessions.Values)
                {
                    _ = Task.Run(async () => await AnalyzeSessionBehaviorAsync(session));
                }
            }
        }

        /// <summary>
        /// 分析会话行为
        /// </summary>
        private async Task AnalyzeSessionBehaviorAsync(ViewerSession session)
        {
            try
            {
                // 分析最近5分钟的行为
                var recentBehaviors = session.BehaviorHistory
                    .Where(b => b.BehaviorTime >= DateTime.Now.AddMinutes(-5))
                    .ToList();

                // 检测频繁操作
                await DetectFrequentActionsAsync(session, recentBehaviors);

                // 检测异常模式
                await DetectAbnormalPatternsAsync(session, recentBehaviors);

                // 检测设备异常
                await DetectDeviceAnomaliesAsync(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"分析会话行为时发生异常: {session.ViewerId}");
            }
        }

        /// <summary>
        /// 检测频繁操作
        /// </summary>
        private async Task DetectFrequentActionsAsync(ViewerSession session, List<ViewerBehavior> recentBehaviors)
        {
            var behaviorCounts = recentBehaviors
                .GroupBy(b => b.BehaviorType)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var behaviorCount in behaviorCounts)
            {
                var threshold = GetBehaviorThreshold(behaviorCount.Key);
                if (behaviorCount.Value > threshold)
                {
                    await CreateAbnormalBehaviorAlarmAsync(session, behaviorCount.Key, 
                        $"5分钟内{behaviorCount.Key}操作{behaviorCount.Value}次，超过阈值{threshold}");
                }
            }
        }

        /// <summary>
        /// 获取行为阈值
        /// </summary>
        private int GetBehaviorThreshold(ViewerBehaviorType behaviorType)
        {
            return behaviorType switch
            {
                ViewerBehaviorType.Chat => 50,
                ViewerBehaviorType.Like => 100,
                ViewerBehaviorType.Share => 10,
                ViewerBehaviorType.Gift => 20,
                ViewerBehaviorType.Follow => 5,
                _ => 30
            };
        }

        /// <summary>
        /// 创建异常行为报警
        /// </summary>
        private async Task CreateAbnormalBehaviorAlarmAsync(ViewerSession session, ViewerBehaviorType behaviorType, string description)
        {
            var alarmEvent = new AlarmEvent
            {
                EventType = AlarmEventType.AbnormalBehavior,
                Level = AlarmLevel.Medium,
                Title = $"观众异常行为检测",
                Description = description,
                LiveRoomId = session.LiveRoomId,
                ViewerId = session.ViewerId,
                Location = $"直播间:{session.LiveRoomId}",
                AdditionalData = new Dictionary<string, object>
                {
                    { "BehaviorType", behaviorType.ToString() },
                    { "ViewerInfo", session.DeviceInfo },
                    { "RiskScore", session.RiskScore }
                }
            };

            await _alarmEventHandler.AddAlarmEventAsync(alarmEvent);
        }

        /// <summary>
        /// 处理不活跃观众
        /// </summary>
        private async Task HandleInactiveViewerAsync(string viewerId)
        {
            ViewerSession session = null;
            lock (_activeSessions)
            {
                _activeSessions.TryGetValue(viewerId, out session);
            }

            if (session != null)
            {
                session.Status = ViewerStatus.Inactive;
                session.Warnings.Add($"观众已不活跃超过{DateTime.Now - session.LastActivityTime:mm}分钟");

                // 发送不活跃通知
                await _hubContext.Clients.Group($"Room_{session.LiveRoomId}").SendAsync("ViewerInactive", new
                {
                    ViewerId = viewerId,
                    InactiveTime = DateTime.Now - session.LastActivityTime
                });
            }
        }

        /// <summary>
        /// 记录观众事件
        /// </summary>
        private async Task RecordViewerEventAsync(string viewerId, string liveRoomId, ViewerEventType eventType, object eventData)
        {
            try
            {
                var viewerEvent = new ViewerEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    ViewerId = viewerId,
                    LiveRoomId = liveRoomId,
                    EventType = eventType,
                    EventTime = DateTime.Now,
                    EventData = JsonConvert.SerializeObject(eventData),
                    CreateTime = DateTime.Now
                };

                // 异步保存到数据库
                _ = Task.Run(async () => await SaveViewerEventToDatabase(viewerEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"记录观众事件时发生异常: {viewerId} - {eventType}");
            }
        }

        /// <summary>
        /// 保存观众事件到数据库
        /// </summary>
        private async Task SaveViewerEventToDatabase(ViewerEvent viewerEvent)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Id", viewerEvent.Id },
                    { "ViewerId", viewerEvent.ViewerId },
                    { "LiveRoomId", viewerEvent.LiveRoomId },
                    { "EventType", (int)viewerEvent.EventType },
                    { "EventTime", viewerEvent.EventTime },
                    { "EventData", viewerEvent.EventData },
                    { "CreateTime", viewerEvent.CreateTime }
                };

                var sql = @"
                    INSERT INTO ViewerEvents 
                    (Id, ViewerId, LiveRoomId, EventType, EventTime, EventData, CreateTime)
                    VALUES 
                    (@Id, @ViewerId, @LiveRoomId, @EventType, @EventTime, @EventData, @CreateTime)";

                await _databaseService.ExecuteNonQueryAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存观众事件到数据库时发生异常: {viewerEvent.ViewerId}");
            }
        }

        #endregion

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
        }
    }
}
