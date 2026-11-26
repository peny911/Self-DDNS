namespace SelfDdns.Services;

public record IpResult(string Ip, string Provider);

public class IpDetectionConfig
{
    public IReadOnlyList<string> Providers { get; init; } = [];
}

public interface IIpService
{
    Task<IpResult?> GetPublicIpAsync(CancellationToken cancellationToken = default);
}

public class IpService : IIpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IpService> _logger;
    private readonly IReadOnlyList<string> _providers;

    public IpService(
        HttpClient httpClient,
        IpDetectionConfig config,
        ILogger<IpService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _providers = config.Providers;
        _logger = logger;
    }

    public async Task<IpResult?> GetPublicIpAsync(CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(provider, cancellationToken);
                var ip = ExtractIp(response);

                if (!string.IsNullOrEmpty(ip) && IsValidIpv4(ip))
                {
                    return new IpResult(ip, provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("从 {Provider} 获取 IP 失败: {Error}", provider, ex.Message);
            }
        }

        _logger.LogError("所有 IP 提供商均获取失败");
        return null;
    }

    private static string? ExtractIp(string response)
    {
        var text = response.Trim();

        // 如果整个响应就是 IP，直接返回
        if (IsValidIpv4(text))
        {
            return text;
        }

        // 尝试用正则提取 IP（适用于 ipip.net 等返回文本描述的服务）
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool IsValidIpv4(string ip)
    {
        return System.Net.IPAddress.TryParse(ip, out var address)
               && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }
}
