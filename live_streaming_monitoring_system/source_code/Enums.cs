using System;

namespace LiveStreamingMonitoringSystem.Models
{
    /// <summary>
    /// 报警事件类型
    /// </summary>
    public enum AlarmEventType
    {
        Unknown = 0,
        DeviceOffline = 1,
        DeviceError = 2,
        AbnormalBehavior = 3,
        SecurityThreat = 4,
        SystemError = 5,
        NetworkIssue = 6,
        PerformanceIssue = 7,
        DataAbnormal = 8,
        UserViolation = 9,
        ContentViolation = 10
    }

    /// <summary>
    /// 报警级别
    /// </summary>
    public enum AlarmLevel
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Emergency = 5
    }

    /// <summary>
    /// 报警状态
    /// </summary>
    public enum AlarmStatus
    {
        Unknown = 0,
        Active = 1,
        Acknowledged = 2,
        Resolved = 3,
        Closed = 4,
        Deleted = 5
    }

    /// <summary>
    /// 处理状态
    /// </summary>
    public enum ProcessStatus
    {
        Unknown = 0,
        Pending = 1,
        Processing = 2,
        Resolved = 3,
        Rejected = 4,
        Deleted = 5
    }

    /// <summary>
    /// 设备类型
    /// </summary>
    public enum DeviceType
    {
        Unknown = 0,
        Camera = 1,
        Microphone = 2,
        Speaker = 3,
        Sensor = 4,
        Gateway = 5,
        Router = 6,
        Switch = 7,
        Server = 8,
        Workstation = 9,
        Mobile = 10,
        Tablet = 11,
        SmartTV = 12,
        IoTDevice = 13
    }

    /// <summary>
    /// 设备状态
    /// </summary>
    public enum DeviceStatus
    {
        Unknown = 0,
        Offline = 1,
        Online = 2,
        Error = 3,
        Maintenance = 4,
        Disabled = 5
    }

    /// <summary>
    /// 网络状态
    /// </summary>
    public enum NetworkStatus
    {
        Unknown = 0,
        Disconnected = 1,
        Connected = 2,
        Unstable = 3,
        Slow = 4,
        Error = 5
    }

    /// <summary>
    /// 观众状态
    /// </summary>
    public enum ViewerStatus
    {
        Unknown = 0,
        Offline = 1,
        Online = 2,
        Inactive = 3,
        Blocked = 4,
        Warned = 5,
        Error = 6
    }

    /// <summary>
    /// 观众行为类型
    /// </summary>
    public enum ViewerBehaviorType
    {
        Unknown = 0,
        Join = 1,
        Leave = 2,
        Chat = 3,
        Like = 4,
        Share = 5,
        Gift = 6,
        Follow = 7,
        Subscribe = 8,
        Report = 9,
        Block = 10,
        Click = 11,
        Scroll = 12,
        Pause = 13,
        Resume = 14,
        VolumeChange = 15,
        QualityChange = 16,
        FullScreen = 17,
        ExitFullScreen = 18
    }

    /// <summary>
    /// 观众事件类型
    /// </summary>
    public enum ViewerEventType
    {
        Unknown = 0,
        Enter = 1,
        Leave = 2,
        Reconnect = 3,
        Disconnect = 4,
        StatusChange = 5,
        BehaviorDetected = 6,
        RiskScoreChanged = 7,
        WarningIssued = 8,
        ActionTaken = 9
    }

    /// <summary>
    /// 异常行为类型
    /// </summary>
    public enum AbnormalBehaviorType
    {
        Unknown = 0,
        FrequentActions = 1,
        SuspiciousPattern = 2,
        BotBehavior = 3,
        SpamMessages = 4,
        InappropriateContent = 5,
        MultipleAccounts = 6,
        FakeEngagement = 7,
        SecurityThreat = 8,
        PolicyViolation = 9,
        TechnicalAnomaly = 10
    }

    /// <summary>
    /// 严重程度
    /// </summary>
    public enum SeverityLevel
    {
        Unknown = 0,
        Info = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }

    /// <summary>
    /// 直播间状态
    /// </summary>
    public enum LiveRoomStatus
    {
        Unknown = 0,
        Created = 1,
        Preparing = 2,
        Live = 3,
        Paused = 4,
        Ended = 5,
        Closed = 6,
        Banned = 7,
        Error = 8
    }

    /// <summary>
    /// BIM模型状态
    /// </summary>
    public enum BIMModelStatus
    {
        Unknown = 0,
        Uploading = 1,
        Processing = 2,
        Ready = 3,
        Error = 4,
        Archived = 5,
        Deleted = 6
    }

    /// <summary>
    /// 场景类型
    /// </summary>
    public enum SceneType
    {
        Unknown = 0,
        Indoor = 1,
        Outdoor = 2,
        Mixed = 3,
        Virtual = 4,
        Augmented = 5
    }

    /// <summary>
    /// 图层类型
    /// </summary>
    public enum LayerType
    {
        Unknown = 0,
        Background = 1,
        Terrain = 2,
        Building = 3,
        Device = 4,
        Annotation = 5,
        Effect = 6,
        UI = 7,
        Data = 8
    }

    /// <summary>
    /// 对象类型
    /// </summary>
    public enum ObjectType
    {
        Unknown = 0,
        Mesh = 1,
        Sprite = 2,
        Text = 3,
        Line = 4,
        Point = 5,
        Polygon = 6,
        Circle = 7,
        Rectangle = 8,
        Curve = 9,
        Surface = 10
    }

    /// <summary>
    /// 相机类型
    /// </summary>
    public enum CameraType
    {
        Unknown = 0,
        Perspective = 1,
        Orthographic = 2,
        Panoramic = 3,
        VR = 4,
        AR = 5
    }

    /// <summary>
    /// 光源类型
    /// </summary>
    public enum LightType
    {
        Unknown = 0,
        Directional = 1,
        Point = 2,
        Spot = 3,
        Area = 4,
        Ambient = 5,
        Environment = 6
    }

    /// <summary>
    /// 阴影质量
    /// </summary>
    public enum ShadowQuality
    {
        Disabled = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Ultra = 4
    }

    /// <summary>
    /// 报表类型
    /// </summary>
    public enum ReportType
    {
        Unknown = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        Yearly = 5,
        Custom = 6,
        RealTime = 7,
        Summary = 8,
        Detailed = 9,
        Comparison = 10
    }

    /// <summary>
    /// 报表状态
    /// </summary>
    public enum ReportStatus
    {
        Unknown = 0,
        Pending = 1,
        Generating = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6
    }

    /// <summary>
    /// 章节类型
    /// </summary>
    public enum SectionType
    {
        Unknown = 0,
        Summary = 1,
        Chart = 2,
        Table = 3,
        Text = 4,
        Image = 5,
        Mixed = 6
    }

    /// <summary>
    /// 图表类型
    /// </summary>
    public enum ChartType
    {
        Unknown = 0,
        Line = 1,
        Bar = 2,
        Pie = 3,
        Doughnut = 4,
        Area = 5,
        Scatter = 6,
        Bubble = 7,
        Radar = 8,
        Polar = 9,
        Heatmap = 10,
        Treemap = 11,
        Sankey = 12,
        Funnel = 13,
        Gauge = 14,
        Waterfall = 15
    }

    /// <summary>
    /// 观众操作类型
    /// </summary>
    public enum ViewerOperation
    {
        Unknown = 0,
        Block = 1,
        Unblock = 2,
        Warn = 3,
        Kick = 4,
        Mute = 5,
        Unmute = 6,
        Promote = 7,
        Demote = 8,
        Ban = 9,
        Unban = 10
    }

    /// <summary>
    /// 设备操作类型
    /// </summary>
    public enum DeviceOperation
    {
        Unknown = 0,
        Add = 1,
        Update = 2,
        Delete = 3,
        Enable = 4,
        Disable = 5,
        Restart = 6,
        Reset = 7,
        Configure = 8,
        Calibrate = 9,
        Upgrade = 10
    }

    /// <summary>
    /// 设备命令类型
    /// </summary>
    public enum DeviceCommand
    {
        Unknown = 0,
        PowerOn = 1,
        PowerOff = 2,
        Restart = 3,
        Reset = 4,
        StartRecording = 5,
        StopRecording = 6,
        TakeSnapshot = 7,
        SetVolume = 8,
        SetBrightness = 9,
        SetContrast = 10,
        SetResolution = 11,
        SetFrameRate = 12,
        StartStream = 13,
        StopStream = 14,
        PanTilt = 15,
        Zoom = 16,
        Focus = 17,
        GetStatus = 18,
        GetConfig = 19,
        SetConfig = 20
    }

    /// <summary>
    /// 报警事件动作
    /// </summary>
    public enum AlarmEventAction
    {
        Unknown = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        Acknowledge = 4,
        Resolve = 5,
        Close = 6,
        Escalate = 7,
        Assign = 8,
        Comment = 9,
        Archive = 10
    }

    /// <summary>
    /// 通知类型
    /// </summary>
    public enum NotificationType
    {
        Unknown = 0,
        Email = 1,
        SMS = 2,
        Push = 3,
        WebSocket = 4,
        Webhook = 5,
        InApp = 6,
        Desktop = 7,
        Mobile = 8,
        Voice = 9,
        Fax = 10
    }

    /// <summary>
    /// 通知优先级
    /// </summary>
    public enum NotificationPriority
    {
        Unknown = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Urgent = 4,
        Critical = 5
    }

    /// <summary>
    /// 通知状态
    /// </summary>
    public enum NotificationStatus
    {
        Unknown = 0,
        Pending = 1,
        Sending = 2,
        Sent = 3,
        Delivered = 4,
        Read = 5,
        Failed = 6,
        Cancelled = 7,
        Expired = 8
    }

    /// <summary>
    /// 用户角色
    /// </summary>
    public enum UserRole
    {
        Unknown = 0,
        Guest = 1,
        Viewer = 2,
        Moderator = 3,
        Administrator = 4,
        SuperAdmin = 5,
        System = 6
    }

    /// <summary>
    /// 权限类型
    /// </summary>
    public enum PermissionType
    {
        Unknown = 0,
        Read = 1,
        Write = 2,
        Delete = 3,
        Execute = 4,
        Admin = 5,
        Owner = 6
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Unknown = 0,
        Trace = 1,
        Debug = 2,
        Information = 3,
        Warning = 4,
        Error = 5,
        Critical = 6,
        None = 7
    }

    /// <summary>
    /// 数据源类型
    /// </summary>
    public enum DataSourceType
    {
        Unknown = 0,
        Database = 1,
        API = 2,
        File = 3,
        Stream = 4,
        Cache = 5,
        Memory = 6,
        Queue = 7,
        WebSocket = 8,
        EventHub = 9,
        MessageBus = 10
    }

    /// <summary>
    /// 导出格式
    /// </summary>
    public enum ExportFormat
    {
        Unknown = 0,
        PDF = 1,
        Excel = 2,
        CSV = 3,
        JSON = 4,
        XML = 5,
        HTML = 6,
        Word = 7,
        PowerPoint = 8,
        Image = 9,
        Video = 10
    }

    /// <summary>
    /// 压缩类型
    /// </summary>
    public enum CompressionType
    {
        None = 0,
        Zip = 1,
        Gzip = 2,
        Bzip2 = 3,
        Lz4 = 4,
        Lzma = 5,
        Snappy = 6,
        Zstd = 7
    }

    /// <summary>
    /// 加密类型
    /// </summary>
    public enum EncryptionType
    {
        None = 0,
        AES128 = 1,
        AES256 = 2,
        RSA = 3,
        DES = 4,
        TripleDES = 5,
        Blowfish = 6,
        Twofish = 7,
        ChaCha20 = 8
    }

    /// <summary>
    /// 哈希算法
    /// </summary>
    public enum HashAlgorithm
    {
        Unknown = 0,
        MD5 = 1,
        SHA1 = 2,
        SHA256 = 3,
        SHA384 = 4,
        SHA512 = 5,
        CRC32 = 6,
        Blake2 = 7,
        Argon2 = 8
    }

    /// <summary>
    /// 协议类型
    /// </summary>
    public enum ProtocolType
    {
        Unknown = 0,
        HTTP = 1,
        HTTPS = 2,
        WebSocket = 3,
        WebSocketSecure = 4,
        TCP = 5,
        UDP = 6,
        MQTT = 7,
        CoAP = 8,
        AMQP = 9,
        STOMP = 10,
        SignalR = 11,
        gRPC = 12,
        GraphQL = 13,
        REST = 14,
        SOAP = 15
    }

    /// <summary>
    /// 存储类型
    /// </summary>
    public enum StorageType
    {
        Unknown = 0,
        Local = 1,
        Network = 2,
        Cloud = 3,
        Database = 4,
        Memory = 5,
        Cache = 6,
        CDN = 7,
        ObjectStorage = 8,
        BlockStorage = 9,
        FileStorage = 10
    }

    /// <summary>
    /// 备份类型
    /// </summary>
    public enum BackupType
    {
        Unknown = 0,
        Full = 1,
        Incremental = 2,
        Differential = 3,
        Snapshot = 4,
        Mirror = 5,
        Archive = 6,
        Sync = 7
    }

    /// <summary>
    /// 恢复类型
    /// </summary>
    public enum RestoreType
    {
        Unknown = 0,
        Full = 1,
        Partial = 2,
        PointInTime = 3,
        Selective = 4,
        Emergency = 5,
        Test = 6
    }
}
