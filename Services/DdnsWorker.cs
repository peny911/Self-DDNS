namespace SelfDdns.Services;

public class DdnsWorker : BackgroundService
{
    private readonly IIpService _ipService;
    private readonly IAliyunDnsService _dnsService;
    private readonly DnsConfig _config;
    private readonly ILogger<DdnsWorker> _logger;
    private string? _lastKnownIp;

    public DdnsWorker(
        IIpService ipService,
        IAliyunDnsService dnsService,
        DnsConfig config,
        ILogger<DdnsWorker> logger)
    {
        _ipService = ipService;
        _dnsService = dnsService;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DDNS 服务已启动，监控域名: {SubDomain}.{Domain}，检查间隔: {Interval} 秒",
            _config.SubDomain, _config.Domain, _config.CheckIntervalSeconds);

        // 启动时获取当前 DNS 记录值
        _lastKnownIp = await _dnsService.GetCurrentRecordValueAsync(stoppingToken);
        if (_lastKnownIp != null)
        {
            _logger.LogInformation("当前 DNS 记录值: {Ip}", _lastKnownIp);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndUpdateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查/更新过程发生错误");
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.CheckIntervalSeconds), stoppingToken);
        }
    }

    private async Task CheckAndUpdateAsync(CancellationToken stoppingToken)
    {
        var result = await _ipService.GetPublicIpAsync(stoppingToken);

        if (result == null)
        {
            _logger.LogWarning("无法获取当前外网 IP");
            return;
        }

        var currentIp = result.Ip;

        if (currentIp == _lastKnownIp)
        {
            _logger.LogInformation("通过 {Provider} 查询，当前 IP: {Ip}，DNS 记录一致，无需更新",
                result.Provider, currentIp);
            return;
        }

        _logger.LogInformation("通过 {Provider} 查询，当前 IP: {Ip}，检测到 IP 变化: {OldIp} -> {NewIp}",
            result.Provider, currentIp, _lastKnownIp ?? "(无)", currentIp);

        var success = await _dnsService.UpdateRecordAsync(currentIp, stoppingToken);
        if (success)
        {
            _lastKnownIp = currentIp;
        }
    }
}
