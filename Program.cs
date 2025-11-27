using SelfDdns.Services;

var builder = Host.CreateApplicationBuilder(args);

// 配置日志格式为单行
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.UseUtcTimestamp = false;
});

// 从配置文件或环境变量读取配置（环境变量优先）
var dnsConfig = new DnsConfig
{
    AccessKeyId = GetConfig("ALIYUN_ACCESS_KEY_ID", "Aliyun:AccessKeyId"),
    AccessKeySecret = GetConfig("ALIYUN_ACCESS_KEY_SECRET", "Aliyun:AccessKeySecret"),
    Domain = GetConfig("DNS_DOMAIN", "Dns:Domain"),
    SubDomain = GetConfig("DNS_SUBDOMAIN", "Dns:SubDomain"),
    CheckIntervalSeconds = int.TryParse(builder.Configuration["Dns:CheckIntervalSeconds"], out var interval) ? interval : 60
};

string GetConfig(string envVar, string configKey)
{
    var envValue = Environment.GetEnvironmentVariable(envVar);
    if (!string.IsNullOrEmpty(envValue)) return envValue;

    var configValue = builder.Configuration[configKey];
    return !string.IsNullOrEmpty(configValue) ? configValue : "";
}

// 验证配置
if (string.IsNullOrEmpty(dnsConfig.AccessKeyId) || string.IsNullOrEmpty(dnsConfig.AccessKeySecret))
{
    Console.WriteLine("错误: 请配置阿里云 AccessKeyId 和 AccessKeySecret");
    Console.WriteLine("可通过 appsettings.json 或环境变量 ALIYUN_ACCESS_KEY_ID / ALIYUN_ACCESS_KEY_SECRET 配置");
    return 1;
}

if (string.IsNullOrEmpty(dnsConfig.Domain) || string.IsNullOrEmpty(dnsConfig.SubDomain))
{
    Console.WriteLine("错误: 请配置域名信息");
    Console.WriteLine("可通过 appsettings.json 或环境变量 DNS_DOMAIN / DNS_SUBDOMAIN 配置");
    return 1;
}

// 注册服务
builder.Services.AddSingleton(dnsConfig);
builder.Services.AddHttpClient<IIpService, IpService>();
builder.Services.AddSingleton<IAliyunDnsService, AliyunDnsService>();
builder.Services.AddHostedService<DdnsWorker>();

var host = builder.Build();
host.Run();

return 0;
