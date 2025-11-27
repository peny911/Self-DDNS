# Self-DDNS

基于阿里云 DNS API 的动态域名解析服务，适用于家庭宽带动态 IP 环境。

## 功能特性

- 每 60 秒自动检测外网 IP 变化
- IP 变化时自动更新阿里云 DNS A 记录
- 支持多个国内 IP 查询服务，避免 VPN 分流问题
- 支持 Docker 部署，适配群晖 NAS

## 前置准备

### 1. 获取阿里云 AccessKey

1. 登录 [阿里云控制台](https://ram.console.aliyun.com/)
2. 创建 RAM 用户，勾选「OpenAPI 调用访问」
3. 为该用户授权 `AliyunDNSFullAccess` 权限
4. 创建并保存 AccessKey ID 和 AccessKey Secret

### 2. 确认域名已托管

确保你的域名已在阿里云 DNS 解析服务中托管。

## 部署方式

### 方式一：Docker 部署（推荐）

#### 构建镜像

```bash
docker build -t self-ddns .
```

#### 运行容器

```bash
docker run -d \
  --name self-ddns \
  --restart=always \
  -e TZ=Asia/Shanghai \
  -e ALIYUN_ACCESS_KEY_ID="你的AccessKeyId" \
  -e ALIYUN_ACCESS_KEY_SECRET="你的AccessKeySecret" \
  -e DNS_DOMAIN="mydomain.com" \
  -e DNS_SUBDOMAIN="home" \
  self-ddns
```

> **提示**：`TZ=Asia/Shanghai` 用于设置容器时区，确保日志时间与本地时间一致。

#### 查看日志

```bash
docker logs -f self-ddns
```

### 方式二：群晖 Docker 部署

#### 步骤 1：上传镜像

将项目文件上传到群晖，通过 SSH 执行：

```bash
cd /volume1/docker/self-ddns
docker build -t self-ddns .
```

或者在本地构建后导出：

```bash
# 本地构建（Apple Silicon Mac 需指定平台为 amd64）
docker build --platform linux/amd64 -t self-ddns .

# 导出镜像
docker save self-ddns -o self-ddns.tar

# 上传 self-ddns.tar 到群晖，然后导入
docker load -i self-ddns.tar
```

> **注意**：如果在 Apple Silicon Mac (M1/M2/M3) 上构建，必须添加 `--platform linux/amd64` 参数，否则镜像无法在 x86_64 架构的群晖 NAS 上运行（会报 `exec format error` 错误）。

#### 步骤 2：通过群晖 Docker 界面创建容器

1. 打开「Container Manager」（或 Docker 套件）
2. 选择「self-ddns」镜像，点击「启动」
3. 配置环境变量：

| 变量名 | 值 |
|--------|-----|
| TZ | Asia/Shanghai |
| ALIYUN_ACCESS_KEY_ID | 你的 AccessKey ID |
| ALIYUN_ACCESS_KEY_SECRET | 你的 AccessKey Secret |
| DNS_DOMAIN | mydomain.com |
| DNS_SUBDOMAIN | home |

4. 启用「自动重新启动」
5. 完成创建

### 方式三：直接运行

#### 编译发布

```bash
dotnet publish -c Release -o ./publish
```

#### 设置环境变量并运行

```bash
export ALIYUN_ACCESS_KEY_ID="你的AccessKeyId"
export ALIYUN_ACCESS_KEY_SECRET="你的AccessKeySecret"
export DNS_DOMAIN="mydomain.com"
export DNS_SUBDOMAIN="home"

./publish/self-ddns
```

## 配置说明

### 环境变量

| 变量名 | 必填 | 说明 |
|--------|------|------|
| TZ | 否 | 时区，如 `Asia/Shanghai`，默认已设置 |
| ALIYUN_ACCESS_KEY_ID | 是 | 阿里云 AccessKey ID |
| ALIYUN_ACCESS_KEY_SECRET | 是 | 阿里云 AccessKey Secret |
| DNS_DOMAIN | 是 | 主域名，如 `mydomain.com` |
| DNS_SUBDOMAIN | 是 | 子域名前缀，如 `home` |

### 配置文件

也可以通过 `appsettings.json` 配置：

```json
{
  "Aliyun": {
    "AccessKeyId": "你的AccessKeyId",
    "AccessKeySecret": "你的AccessKeySecret"
  },
  "Dns": {
    "Domain": "mydomain.com",
    "SubDomain": "home",
    "CheckIntervalSeconds": 60
  }
}
```

> 环境变量优先级高于配置文件

## 验证部署

1. 查看容器日志，确认服务正常启动：

```bash
docker logs self-ddns
```

正常输出示例：

```
info: Ddns.Services.DdnsWorker[0]
      DDNS 服务已启动，监控域名: home.mydomain.com，检查间隔: 60 秒
info: Ddns.Services.DdnsWorker[0]
      当前 DNS 记录值: 1.2.3.4
```

2. 在阿里云控制台查看 DNS 记录是否已更新

3. 使用 `nslookup` 或 `dig` 验证：

```bash
nslookup home.mydomain.com
```

## 常见问题

### Q: 获取的 IP 不正确？

如果 NAS 上运行了 VPN，请确保 VPN 配置为分流模式（国内 IP 直连）。本服务优先使用国内 IP 查询服务。

### Q: DNS 记录更新失败？

1. 检查 AccessKey 是否正确
2. 确认 RAM 用户有 DNS 操作权限
3. 确认域名已托管在阿里云

### Q: 如何修改检查间隔？

通过配置文件修改 `CheckIntervalSeconds`，单位为秒，默认 60 秒。
