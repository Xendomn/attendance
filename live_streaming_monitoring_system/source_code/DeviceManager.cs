using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace LiveStreamingMonitoringSystem.Core
{
    /// <summary>
    /// 设备管理器 - 负责直播监控设备的管理和交互
    /// </summary>
    public class DeviceManager : IDeviceManager
    {
        private readonly ILogger<DeviceManager> _logger;
        private readonly string _connectionString;
        private readonly IDeviceStatusMonitor _statusMonitor;
        private readonly IDeviceCommandExecutor _commandExecutor;

        public DeviceManager(
            ILogger<DeviceManager> logger,
            IConfiguration configuration,
            IDeviceStatusMonitor statusMonitor,
            IDeviceCommandExecutor commandExecutor)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _statusMonitor = statusMonitor;
            _commandExecutor = commandExecutor;
        }

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备信息</returns>
        public async Task<DeviceInfo> GetDeviceInfoAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation($"获取设备信息: {deviceId}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT d.*, ds.Status, ds.LastHeartbeat, ds.CpuUsage, ds.MemoryUsage, 
                               ds.NetworkStatus, ds.Temperature, ds.ErrorCount
                        FROM Devices d
                        LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                        WHERE d.Id = @DeviceId AND d.IsDeleted = 0";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DeviceId", deviceId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapDeviceInfoFromReader(reader);
                            }
                        }
                    }
                }

                _logger.LogWarning($"设备不存在: {deviceId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备信息时发生异常: {deviceId}");
                return null;
            }
        }

        /// <summary>
        /// 获取设备列表
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <returns>设备列表</returns>
        public async Task<PagedResult<DeviceInfo>> GetDevicesAsync(DeviceFilter filter)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 构建查询条件
                    var whereClause = BuildDeviceWhereClause(filter);
                    var orderClause = BuildDeviceOrderClause(filter.SortField, filter.SortDirection);

                    // 查询总数
                    var countSql = $@"
                        SELECT COUNT(1) 
                        FROM Devices d
                        LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                        WHERE {whereClause}";
                    
                    var totalCount = 0;
                    using (var countCommand = new SqlCommand(countSql, connection))
                    {
                        AddDeviceFilterParameters(countCommand, filter);
                        totalCount = (int)await countCommand.ExecuteScalarAsync();
                    }

                    // 查询数据
                    var dataSql = $@"
                        SELECT * FROM (
                            SELECT d.*, ds.Status, ds.LastHeartbeat, ds.CpuUsage, ds.MemoryUsage, 
                                   ds.NetworkStatus, ds.Temperature, ds.ErrorCount,
                                   ROW_NUMBER() OVER ({orderClause}) as RowNum
                            FROM Devices d
                            LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                            WHERE {whereClause}
                        ) AS PagedResults
                        WHERE RowNum BETWEEN @StartRow AND @EndRow";

                    var devices = new List<DeviceInfo>();
                    using (var dataCommand = new SqlCommand(dataSql, connection))
                    {
                        AddDeviceFilterParameters(dataCommand, filter);
                        dataCommand.Parameters.AddWithValue("@StartRow", 
                            (filter.PageIndex - 1) * filter.PageSize + 1);
                        dataCommand.Parameters.AddWithValue("@EndRow", 
                            filter.PageIndex * filter.PageSize);

                        using (var reader = await dataCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                devices.Add(MapDeviceInfoFromReader(reader));
                            }
                        }
                    }

                    return new PagedResult<DeviceInfo>
                    {
                        Data = devices,
                        TotalCount = totalCount,
                        PageIndex = filter.PageIndex,
                        PageSize = filter.PageSize,
                        TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备列表时发生异常");
                return new PagedResult<DeviceInfo>
                {
                    Data = new List<DeviceInfo>(),
                    TotalCount = 0,
                    PageIndex = filter.PageIndex,
                    PageSize = filter.PageSize,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>添加结果</returns>
        public async Task<DeviceOperationResult> AddDeviceAsync(DeviceInfo device)
        {
            try
            {
                _logger.LogInformation($"添加设备: {device.Name} - {device.IpAddress}");

                // 验证设备信息
                var validationResult = ValidateDeviceInfo(device);
                if (!validationResult.IsValid)
                {
                    return new DeviceOperationResult 
                    { 
                        Success = false, 
                        Message = validationResult.ErrorMessage 
                    };
                }

                // 检查设备是否已存在
                var existingDevice = await GetDeviceByIpAsync(device.IpAddress);
                if (existingDevice != null)
                {
                    return new DeviceOperationResult 
                    { 
                        Success = false, 
                        Message = "该IP地址的设备已存在" 
                    };
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 设置设备基本信息
                            device.Id = Guid.NewGuid().ToString();
                            device.CreateTime = DateTime.Now;
                            device.Status = DeviceStatus.Offline;

                            // 插入设备主表
                            var insertDeviceSql = @"
                                INSERT INTO Devices 
                                (Id, Name, Type, Model, IpAddress, Port, Location, Description, 
                                 CreateTime, CreatedBy, IsDeleted, Configuration)
                                VALUES 
                                (@Id, @Name, @Type, @Model, @IpAddress, @Port, @Location, @Description,
                                 @CreateTime, @CreatedBy, @IsDeleted, @Configuration)";

                            using (var command = new SqlCommand(insertDeviceSql, connection, transaction))
                            {
                                AddDeviceParameters(command, device);
                                await command.ExecuteNonQueryAsync();
                            }

                            // 插入设备状态表
                            var insertStatusSql = @"
                                INSERT INTO DeviceStatus 
                                (DeviceId, Status, LastHeartbeat, CpuUsage, MemoryUsage, 
                                 NetworkStatus, Temperature, ErrorCount, CreateTime)
                                VALUES 
                                (@DeviceId, @Status, @LastHeartbeat, @CpuUsage, @MemoryUsage,
                                 @NetworkStatus, @Temperature, @ErrorCount, @CreateTime)";

                            using (var command = new SqlCommand(insertStatusSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DeviceId", device.Id);
                                command.Parameters.AddWithValue("@Status", (int)DeviceStatus.Offline);
                                command.Parameters.AddWithValue("@LastHeartbeat", DBNull.Value);
                                command.Parameters.AddWithValue("@CpuUsage", 0);
                                command.Parameters.AddWithValue("@MemoryUsage", 0);
                                command.Parameters.AddWithValue("@NetworkStatus", (int)NetworkStatus.Disconnected);
                                command.Parameters.AddWithValue("@Temperature", 0);
                                command.Parameters.AddWithValue("@ErrorCount", 0);
                                command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                                await command.ExecuteNonQueryAsync();
                            }

                            // 记录操作日志
                            await InsertDeviceOperationLogAsync(connection, transaction, 
                                device.Id, DeviceOperation.Add, "设备添加");

                            transaction.Commit();
                            _logger.LogInformation($"设备添加成功: {device.Id}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"添加设备时发生数据库错误: {device.Name}");
                            throw;
                        }
                    }
                }

                // 启动设备监控
                await _statusMonitor.StartMonitoringAsync(device.Id);

                return new DeviceOperationResult 
                { 
                    Success = true, 
                    Message = "设备添加成功",
                    DeviceId = device.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加设备时发生异常: {device?.Name}");
                return new DeviceOperationResult 
                { 
                    Success = false, 
                    Message = $"添加设备失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 更新设备信息
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>更新结果</returns>
        public async Task<DeviceOperationResult> UpdateDeviceAsync(DeviceInfo device)
        {
            try
            {
                _logger.LogInformation($"更新设备: {device.Id}");

                // 验证设备信息
                var validationResult = ValidateDeviceInfo(device);
                if (!validationResult.IsValid)
                {
                    return new DeviceOperationResult 
                    { 
                        Success = false, 
                        Message = validationResult.ErrorMessage 
                    };
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 检查设备是否存在
                            var existingDevice = await GetDeviceByIdAsync(connection, transaction, device.Id);
                            if (existingDevice == null)
                            {
                                return new DeviceOperationResult 
                                { 
                                    Success = false, 
                                    Message = "设备不存在" 
                                };
                            }

                            // 更新设备信息
                            var updateSql = @"
                                UPDATE Devices 
                                SET Name = @Name,
                                    Type = @Type,
                                    Model = @Model,
                                    IpAddress = @IpAddress,
                                    Port = @Port,
                                    Location = @Location,
                                    Description = @Description,
                                    Configuration = @Configuration,
                                    UpdateTime = @UpdateTime,
                                    UpdatedBy = @UpdatedBy
                                WHERE Id = @Id";

                            using (var command = new SqlCommand(updateSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", device.Id);
                                command.Parameters.AddWithValue("@Name", device.Name ?? "");
                                command.Parameters.AddWithValue("@Type", (int)device.Type);
                                command.Parameters.AddWithValue("@Model", device.Model ?? "");
                                command.Parameters.AddWithValue("@IpAddress", device.IpAddress ?? "");
                                command.Parameters.AddWithValue("@Port", device.Port);
                                command.Parameters.AddWithValue("@Location", device.Location ?? "");
                                command.Parameters.AddWithValue("@Description", device.Description ?? "");
                                command.Parameters.AddWithValue("@Configuration", 
                                    JsonConvert.SerializeObject(device.Configuration) ?? "{}");
                                command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                                command.Parameters.AddWithValue("@UpdatedBy", GetCurrentUserId());
                                
                                await command.ExecuteNonQueryAsync();
                            }

                            // 记录变更日志
                            await InsertDeviceChangeLogAsync(connection, transaction, 
                                existingDevice, device);

                            transaction.Commit();
                            _logger.LogInformation($"设备更新成功: {device.Id}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"更新设备时发生数据库错误: {device.Id}");
                            throw;
                        }
                    }
                }

                return new DeviceOperationResult 
                { 
                    Success = true, 
                    Message = "设备更新成功" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备时发生异常: {device.Id}");
                return new DeviceOperationResult 
                { 
                    Success = false, 
                    Message = $"更新设备失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="deleteReason">删除原因</param>
        /// <returns>删除结果</returns>
        public async Task<DeviceOperationResult> DeleteDeviceAsync(string deviceId, string deleteReason)
        {
            try
            {
                _logger.LogInformation($"删除设备: {deviceId}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 检查设备是否存在
                            var existingDevice = await GetDeviceByIdAsync(connection, transaction, deviceId);
                            if (existingDevice == null)
                            {
                                return new DeviceOperationResult 
                                { 
                                    Success = false, 
                                    Message = "设备不存在" 
                                };
                            }

                            // 软删除设备
                            var deleteSql = @"
                                UPDATE Devices 
                                SET IsDeleted = 1,
                                    DeleteTime = @DeleteTime,
                                    DeleteReason = @DeleteReason,
                                    DeletedBy = @DeletedBy
                                WHERE Id = @Id";

                            using (var command = new SqlCommand(deleteSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Id", deviceId);
                                command.Parameters.AddWithValue("@DeleteTime", DateTime.Now);
                                command.Parameters.AddWithValue("@DeleteReason", deleteReason ?? "");
                                command.Parameters.AddWithValue("@DeletedBy", GetCurrentUserId());
                                
                                await command.ExecuteNonQueryAsync();
                            }

                            // 记录删除日志
                            await InsertDeviceOperationLogAsync(connection, transaction, 
                                deviceId, DeviceOperation.Delete, deleteReason);

                            transaction.Commit();
                            _logger.LogInformation($"设备删除成功: {deviceId}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, $"删除设备时发生数据库错误: {deviceId}");
                            throw;
                        }
                    }
                }

                // 停止设备监控
                await _statusMonitor.StopMonitoringAsync(deviceId);

                return new DeviceOperationResult 
                { 
                    Success = true, 
                    Message = "设备删除成功" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除设备时发生异常: {deviceId}");
                return new DeviceOperationResult 
                { 
                    Success = false, 
                    Message = $"删除设备失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 执行设备命令
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="command">命令</param>
        /// <param name="parameters">参数</param>
        /// <returns>执行结果</returns>
        public async Task<DeviceCommandResult> ExecuteDeviceCommandAsync(
            string deviceId, DeviceCommand command, Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation($"执行设备命令: {deviceId} - {command}");

                // 检查设备是否存在且在线
                var device = await GetDeviceInfoAsync(deviceId);
                if (device == null)
                {
                    return new DeviceCommandResult 
                    { 
                        Success = false, 
                        Message = "设备不存在" 
                    };
                }

                if (device.Status != DeviceStatus.Online)
                {
                    return new DeviceCommandResult 
                    { 
                        Success = false, 
                        Message = "设备离线，无法执行命令" 
                    };
                }

                // 执行命令
                var result = await _commandExecutor.ExecuteAsync(deviceId, command, parameters);

                // 记录命令执行日志
                await RecordCommandExecutionAsync(deviceId, command, parameters, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行设备命令时发生异常: {deviceId} - {command}");
                return new DeviceCommandResult 
                { 
                    Success = false, 
                    Message = $"执行命令失败: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// 获取设备统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<DeviceStatistics> GetDeviceStatisticsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            COUNT(1) as TotalCount,
                            SUM(CASE WHEN ds.Status = 1 THEN 1 ELSE 0 END) as OnlineCount,
                            SUM(CASE WHEN ds.Status = 0 THEN 1 ELSE 0 END) as OfflineCount,
                            SUM(CASE WHEN ds.Status = 2 THEN 1 ELSE 0 END) as ErrorCount,
                            AVG(CAST(ds.CpuUsage as FLOAT)) as AvgCpuUsage,
                            AVG(CAST(ds.MemoryUsage as FLOAT)) as AvgMemoryUsage,
                            AVG(CAST(ds.Temperature as FLOAT)) as AvgTemperature
                        FROM Devices d
                        LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                        WHERE d.IsDeleted = 0";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new DeviceStatistics
                                {
                                    TotalCount = reader.GetInt32("TotalCount"),
                                    OnlineCount = reader.GetInt32("OnlineCount"),
                                    OfflineCount = reader.GetInt32("OfflineCount"),
                                    ErrorCount = reader.GetInt32("ErrorCount"),
                                    AvgCpuUsage = reader.IsDBNull("AvgCpuUsage") ? 0 : (float)reader.GetDouble("AvgCpuUsage"),
                                    AvgMemoryUsage = reader.IsDBNull("AvgMemoryUsage") ? 0 : (float)reader.GetDouble("AvgMemoryUsage"),
                                    AvgTemperature = reader.IsDBNull("AvgTemperature") ? 0 : (float)reader.GetDouble("AvgTemperature")
                                };
                            }
                        }
                    }
                }

                return new DeviceStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备统计信息时发生异常");
                return new DeviceStatistics();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 验证设备信息
        /// </summary>
        private ValidationResult ValidateDeviceInfo(DeviceInfo device)
        {
            if (device == null)
                return new ValidationResult { IsValid = false, ErrorMessage = "设备信息不能为空" };

            if (string.IsNullOrWhiteSpace(device.Name))
                return new ValidationResult { IsValid = false, ErrorMessage = "设备名称不能为空" };

            if (string.IsNullOrWhiteSpace(device.IpAddress))
                return new ValidationResult { IsValid = false, ErrorMessage = "设备IP地址不能为空" };

            if (!IsValidIpAddress(device.IpAddress))
                return new ValidationResult { IsValid = false, ErrorMessage = "设备IP地址格式不正确" };

            if (device.Port <= 0 || device.Port > 65535)
                return new ValidationResult { IsValid = false, ErrorMessage = "设备端口号必须在1-65535之间" };

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 验证IP地址格式
        /// </summary>
        private bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// 根据IP地址获取设备
        /// </summary>
        private async Task<DeviceInfo> GetDeviceByIpAsync(string ipAddress)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT d.*, ds.Status, ds.LastHeartbeat, ds.CpuUsage, ds.MemoryUsage, 
                           ds.NetworkStatus, ds.Temperature, ds.ErrorCount
                    FROM Devices d
                    LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                    WHERE d.IpAddress = @IpAddress AND d.IsDeleted = 0";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@IpAddress", ipAddress);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapDeviceInfoFromReader(reader);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        private async Task<DeviceInfo> GetDeviceByIdAsync(SqlConnection connection, 
            SqlTransaction transaction, string deviceId)
        {
            var sql = @"
                SELECT d.*, ds.Status, ds.LastHeartbeat, ds.CpuUsage, ds.MemoryUsage, 
                       ds.NetworkStatus, ds.Temperature, ds.ErrorCount
                FROM Devices d
                LEFT JOIN DeviceStatus ds ON d.Id = ds.DeviceId
                WHERE d.Id = @DeviceId AND d.IsDeleted = 0";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@DeviceId", deviceId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapDeviceInfoFromReader(reader);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 添加设备参数
        /// </summary>
        private void AddDeviceParameters(SqlCommand command, DeviceInfo device)
        {
            command.Parameters.AddWithValue("@Id", device.Id);
            command.Parameters.AddWithValue("@Name", device.Name ?? "");
            command.Parameters.AddWithValue("@Type", (int)device.Type);
            command.Parameters.AddWithValue("@Model", device.Model ?? "");
            command.Parameters.AddWithValue("@IpAddress", device.IpAddress ?? "");
            command.Parameters.AddWithValue("@Port", device.Port);
            command.Parameters.AddWithValue("@Location", device.Location ?? "");
            command.Parameters.AddWithValue("@Description", device.Description ?? "");
            command.Parameters.AddWithValue("@CreateTime", device.CreateTime);
            command.Parameters.AddWithValue("@CreatedBy", GetCurrentUserId());
            command.Parameters.AddWithValue("@IsDeleted", false);
            command.Parameters.AddWithValue("@Configuration", 
                JsonConvert.SerializeObject(device.Configuration) ?? "{}");
        }

        /// <summary>
        /// 从数据读取器映射设备信息
        /// </summary>
        private DeviceInfo MapDeviceInfoFromReader(SqlDataReader reader)
        {
            return new DeviceInfo
            {
                Id = reader.GetString("Id"),
                Name = reader.GetString("Name"),
                Type = (DeviceType)reader.GetInt32("Type"),
                Model = reader.IsDBNull("Model") ? "" : reader.GetString("Model"),
                IpAddress = reader.GetString("IpAddress"),
                Port = reader.GetInt32("Port"),
                Location = reader.IsDBNull("Location") ? "" : reader.GetString("Location"),
                Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                CreateTime = reader.GetDateTime("CreateTime"),
                Status = reader.IsDBNull("Status") ? DeviceStatus.Offline : (DeviceStatus)reader.GetInt32("Status"),
                LastHeartbeat = reader.IsDBNull("LastHeartbeat") ? null : reader.GetDateTime("LastHeartbeat"),
                CpuUsage = reader.IsDBNull("CpuUsage") ? 0 : reader.GetFloat("CpuUsage"),
                MemoryUsage = reader.IsDBNull("MemoryUsage") ? 0 : reader.GetFloat("MemoryUsage"),
                NetworkStatus = reader.IsDBNull("NetworkStatus") ? NetworkStatus.Disconnected : (NetworkStatus)reader.GetInt32("NetworkStatus"),
                Temperature = reader.IsDBNull("Temperature") ? 0 : reader.GetFloat("Temperature"),
                ErrorCount = reader.IsDBNull("ErrorCount") ? 0 : reader.GetInt32("ErrorCount"),
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    reader.IsDBNull("Configuration") ? "{}" : reader.GetString("Configuration"))
            };
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return "system";
        }

        #endregion
    }
}
