using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using AlibabaCloud.OpenApiClient.Models;

namespace SelfDdns.Services;

public class DnsConfig
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string AccessKeySecret { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string SubDomain { get; set; } = string.Empty;
    public int CheckIntervalSeconds { get; set; } = 60;
}

public interface IAliyunDnsService
{
    Task<string?> GetCurrentRecordValueAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateRecordAsync(string ip, CancellationToken cancellationToken = default);
}

public class AliyunDnsService : IAliyunDnsService
{
    private readonly Client _client;
    private readonly DnsConfig _config;
    private readonly ILogger<AliyunDnsService> _logger;
    private string? _recordId;

    public AliyunDnsService(DnsConfig config, ILogger<AliyunDnsService> logger)
    {
        _config = config;
        _logger = logger;

        var clientConfig = new Config
        {
            AccessKeyId = config.AccessKeyId,
            AccessKeySecret = config.AccessKeySecret,
            Endpoint = "alidns.cn-hangzhou.aliyuncs.com"
        };

        _client = new Client(clientConfig);
    }

    public async Task<string?> GetCurrentRecordValueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DescribeSubDomainRecordsRequest
            {
                SubDomain = $"{_config.SubDomain}.{_config.Domain}",
                Type = "A"
            };

            var response = await _client.DescribeSubDomainRecordsAsync(request);

            if (response.Body.TotalCount > 0)
            {
                var record = response.Body.DomainRecords.Record[0];
                _recordId = record.RecordId;
                _logger.LogDebug("获取到现有记录: {RecordId}, 值: {Value}", _recordId, record.Value);
                return record.Value;
            }

            _logger.LogInformation("未找到现有 A 记录，将创建新记录");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询 DNS 记录失败");
            return null;
        }
    }

    public async Task<bool> UpdateRecordAsync(string ip, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_recordId))
            {
                return await AddRecordAsync(ip);
            }

            var request = new UpdateDomainRecordRequest
            {
                RecordId = _recordId,
                RR = _config.SubDomain,
                Type = "A",
                Value = ip,
                TTL = 600
            };

            await _client.UpdateDomainRecordAsync(request);
            _logger.LogInformation("DNS 记录已更新: {SubDomain}.{Domain} -> {Ip}",
                _config.SubDomain, _config.Domain, ip);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 DNS 记录失败");
            return false;
        }
    }

    private async Task<bool> AddRecordAsync(string ip)
    {
        try
        {
            var request = new AddDomainRecordRequest
            {
                DomainName = _config.Domain,
                RR = _config.SubDomain,
                Type = "A",
                Value = ip,
                TTL = 600
            };

            var response = await _client.AddDomainRecordAsync(request);
            _recordId = response.Body.RecordId;
            _logger.LogInformation("DNS 记录已创建: {SubDomain}.{Domain} -> {Ip}",
                _config.SubDomain, _config.Domain, ip);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 DNS 记录失败");
            return false;
        }
    }
}
