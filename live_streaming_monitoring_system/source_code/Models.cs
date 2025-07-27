using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LiveStreamingMonitoringSystem.Models
{
    /// <summary>
    /// 报警事件模型
    /// </summary>
    public class AlarmEvent
    {
        public Guid Id { get; set; }
        public AlarmEventType EventType { get; set; }
        public AlarmLevel Level { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LiveRoomId { get; set; }
        public string ViewerId { get; set; }
        public string DeviceId { get; set; }
        public DateTime CreateTime { get; set; }
        public AlarmStatus Status { get; set; }
        public ProcessStatus ProcessStatus { get; set; }
        public string Location { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// 设备信息模型
    /// </summary>
    public class DeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DeviceType Type { get; set; }
        public string Model { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public DateTime CreateTime { get; set; }
        public DeviceStatus Status { get; set; }
        public DateTime? LastHeartbeat { get; set; }
        public float CpuUsage { get; set; }
        public float MemoryUsage { get; set; }
        public NetworkStatus NetworkStatus { get; set; }
        public float Temperature { get; set; }
        public int ErrorCount { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// 观众数据模型
    /// </summary>
    public class ViewerData
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Avatar { get; set; }
        public string LiveRoomId { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public int Duration { get; set; }
        public int InteractionCount { get; set; }
        public int ChatMessageCount { get; set; }
        public int GiftCount { get; set; }
        public int ShareCount { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public LocationInfo LocationInfo { get; set; }
        public NetworkInfo NetworkInfo { get; set; }
    }

    /// <summary>
    /// 观众会话模型
    /// </summary>
    public class ViewerSession
    {
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public ViewerStatus Status { get; set; }
        public List<ViewerBehavior> BehaviorHistory { get; set; }
        public int RiskScore { get; set; }
        public List<string> Warnings { get; set; }
    }

    /// <summary>
    /// 观众行为模型
    /// </summary>
    public class ViewerBehavior
    {
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public ViewerBehaviorType BehaviorType { get; set; }
        public DateTime BehaviorTime { get; set; }
        public object BehaviorData { get; set; }
        public string DeviceId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// 观众实时状态模型
    /// </summary>
    public class ViewerRealTimeStatus
    {
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public bool IsOnline { get; set; }
        public ViewerStatus Status { get; set; }
        public DateTime JoinTime { get; set; }
        public TimeSpan WatchDuration { get; set; }
        public DateTime LastActivityTime { get; set; }
        public TimeSpan InactiveTime { get; set; }
        public int RiskScore { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public Dictionary<ViewerBehaviorType, int> RecentBehaviors { get; set; }
        public List<string> Warnings { get; set; }
    }

    /// <summary>
    /// 直播间统计模型
    /// </summary>
    public class LiveRoomStatistics
    {
        public string LiveRoomId { get; set; }
        public DateTime StatDate { get; set; }
        public int UniqueViewers { get; set; }
        public int TotalViews { get; set; }
        public double AvgWatchDuration { get; set; }
        public int MaxWatchDuration { get; set; }
        public int TotalInteractions { get; set; }
        public int TotalChatMessages { get; set; }
        public int TotalGifts { get; set; }
        public int TotalShares { get; set; }
        public int PeakConcurrentViewers { get; set; }
    }

    /// <summary>
    /// 异常行为模型
    /// </summary>
    public class AbnormalBehavior
    {
        public string Id { get; set; }
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public AbnormalBehaviorType BehaviorType { get; set; }
        public DateTime DetectedTime { get; set; }
        public SeverityLevel Severity { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Evidence { get; set; }
        public ProcessStatus Status { get; set; }
        public string UserName { get; set; }
        public string Avatar { get; set; }
        public string RoomName { get; set; }
        public string StreamerName { get; set; }
    }

    /// <summary>
    /// 位置信息模型
    /// </summary>
    public class LocationInfo
    {
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string IpAddress { get; set; }
        public string ISP { get; set; }
    }

    /// <summary>
    /// 网络信息模型
    /// </summary>
    public class NetworkInfo
    {
        public string ConnectionType { get; set; }
        public int Bandwidth { get; set; }
        public int Latency { get; set; }
        public float PacketLoss { get; set; }
        public string UserAgent { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public string DeviceType { get; set; }
    }

    /// <summary>
    /// 监控统计模型
    /// </summary>
    public class MonitoringStatistics
    {
        public int TotalOnlineViewers { get; set; }
        public int ActiveLiveRooms { get; set; }
        public Dictionary<ViewerStatus, int> ViewersByStatus { get; set; }
        public int HighRiskViewers { get; set; }
        public int MediumRiskViewers { get; set; }
        public int LowRiskViewers { get; set; }
        public double AverageWatchDuration { get; set; }
        public int TodayTotalViewers { get; set; }
        public int TodayPeakConcurrent { get; set; }
        public double TodayAverageWatchTime { get; set; }
    }

    /// <summary>
    /// 设备统计模型
    /// </summary>
    public class DeviceStatistics
    {
        public int TotalCount { get; set; }
        public int OnlineCount { get; set; }
        public int OfflineCount { get; set; }
        public int ErrorCount { get; set; }
        public float AvgCpuUsage { get; set; }
        public float AvgMemoryUsage { get; set; }
        public float AvgTemperature { get; set; }
    }

    /// <summary>
    /// 数据库连接状态模型
    /// </summary>
    public class DatabaseConnectionStatus
    {
        public bool IsConnected { get; set; }
        public string ServerVersion { get; set; }
        public DateTime ServerTime { get; set; }
        public string ConnectionString { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 数据库备份结果模型
    /// </summary>
    public class DatabaseBackupResult
    {
        public bool Success { get; set; }
        public string BackupPath { get; set; }
        public DateTime BackupTime { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 观众事件模型
    /// </summary>
    public class ViewerEvent
    {
        public string Id { get; set; }
        public string ViewerId { get; set; }
        public string LiveRoomId { get; set; }
        public ViewerEventType EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string EventData { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 直播间信息模型
    /// </summary>
    public class LiveRoomInfo
    {
        public string Id { get; set; }
        public string RoomName { get; set; }
        public string StreamerName { get; set; }
        public string StreamerId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public LiveRoomStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int CurrentViewers { get; set; }
        public int TotalViewers { get; set; }
        public int MaxConcurrentViewers { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }

    /// <summary>
    /// BIM模型信息
    /// </summary>
    public class BIMModelInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Version { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string CreatedBy { get; set; }
        public BIMModelStatus Status { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<BIMElement> Elements { get; set; }
    }

    /// <summary>
    /// BIM元素模型
    /// </summary>
    public class BIMElement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public BIMGeometry Geometry { get; set; }
        public string MaterialId { get; set; }
        public List<string> DeviceIds { get; set; }
    }

    /// <summary>
    /// BIM几何信息
    /// </summary>
    public class BIMGeometry
    {
        public Point3D Position { get; set; }
        public Vector3D Rotation { get; set; }
        public Vector3D Scale { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public List<Point3D> Vertices { get; set; }
        public List<int> Indices { get; set; }
    }

    /// <summary>
    /// 3D点坐标
    /// </summary>
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// 3D向量
    /// </summary>
    public class Vector3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// 包围盒
    /// </summary>
    public class BoundingBox
    {
        public Point3D Min { get; set; }
        public Point3D Max { get; set; }
    }

    /// <summary>
    /// 2.5D可视化场景
    /// </summary>
    public class VisualizationScene
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SceneType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<VisualizationLayer> Layers { get; set; }
        public CameraSettings Camera { get; set; }
        public LightingSettings Lighting { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 可视化图层
    /// </summary>
    public class VisualizationLayer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public LayerType Type { get; set; }
        public bool Visible { get; set; }
        public float Opacity { get; set; }
        public int ZIndex { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<VisualizationObject> Objects { get; set; }
    }

    /// <summary>
    /// 可视化对象
    /// </summary>
    public class VisualizationObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ObjectType Type { get; set; }
        public Point3D Position { get; set; }
        public Vector3D Rotation { get; set; }
        public Vector3D Scale { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string MaterialId { get; set; }
        public List<string> Tags { get; set; }
    }

    /// <summary>
    /// 相机设置
    /// </summary>
    public class CameraSettings
    {
        public Point3D Position { get; set; }
        public Point3D Target { get; set; }
        public Vector3D Up { get; set; }
        public float FieldOfView { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public CameraType Type { get; set; }
    }

    /// <summary>
    /// 光照设置
    /// </summary>
    public class LightingSettings
    {
        public Color AmbientColor { get; set; }
        public float AmbientIntensity { get; set; }
        public List<LightSource> Lights { get; set; }
        public bool ShadowsEnabled { get; set; }
        public ShadowQuality ShadowQuality { get; set; }
    }

    /// <summary>
    /// 光源
    /// </summary>
    public class LightSource
    {
        public string Id { get; set; }
        public LightType Type { get; set; }
        public Point3D Position { get; set; }
        public Vector3D Direction { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public float Range { get; set; }
        public float SpotAngle { get; set; }
        public bool CastShadows { get; set; }
    }

    /// <summary>
    /// 颜色
    /// </summary>
    public class Color
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }
    }

    /// <summary>
    /// 报表模型
    /// </summary>
    public class ReportModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ReportType Type { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime GenerateTime { get; set; }
        public string CreatedBy { get; set; }
        public ReportStatus Status { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public List<ReportSection> Sections { get; set; }
        public string FilePath { get; set; }
        public string FileFormat { get; set; }
    }

    /// <summary>
    /// 报表章节
    /// </summary>
    public class ReportSection
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public SectionType Type { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<ReportChart> Charts { get; set; }
        public List<ReportTable> Tables { get; set; }
    }

    /// <summary>
    /// 报表图表
    /// </summary>
    public class ReportChart
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public ChartType Type { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public ChartOptions Options { get; set; }
    }

    /// <summary>
    /// 图表选项
    /// </summary>
    public class ChartOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowGrid { get; set; }
        public string Theme { get; set; }
        public Dictionary<string, object> CustomOptions { get; set; }
    }

    /// <summary>
    /// 报表表格
    /// </summary>
    public class ReportTable
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<TableColumn> Columns { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
        public TableOptions Options { get; set; }
    }

    /// <summary>
    /// 表格列
    /// </summary>
    public class TableColumn
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string DataType { get; set; }
        public int Width { get; set; }
        public bool Sortable { get; set; }
        public string Format { get; set; }
    }

    /// <summary>
    /// 表格选项
    /// </summary>
    public class TableOptions
    {
        public bool ShowHeader { get; set; }
        public bool ShowBorder { get; set; }
        public bool Striped { get; set; }
        public int PageSize { get; set; }
        public bool Pageable { get; set; }
    }
}
