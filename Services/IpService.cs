namespace SelfDdns.Services;

public record IpResult(string Ip, string Provider);

public interface IIpService
{
    Task<IpResult?> GetPublicIpAsync(CancellationToken cancellationToken = default);
}

public class IpService : IIpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IpService> _logger;

    // 优先使用国内服务，避免 VPN 分流问题
    private static readonly string[] IpProviders =
    [
        "https://myip.ipip.net",
        "https://ip.3322.net",
        "https://www.3322.org/dyndns/getip",
        "https://api.ipify.org"
    ];

    public IpService(HttpClient httpClient, ILogger<IpService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
    }

    public async Task<IpResult?> GetPublicIpAsync(CancellationToken cancellationToken = default)
    {
        foreach (var provider in IpProviders)
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
