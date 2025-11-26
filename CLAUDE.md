# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Self-DDNS 是一个基于阿里云 DNS API 的动态域名解析服务，用于家庭宽带动态 IP 环境。每 60 秒检测外网 IP 变化，自动更新阿里云 DNS A 记录。

## 常用命令

```bash
# 构建项目
dotnet build

# 发布项目
dotnet publish -c Release -o ./publish

# 运行项目（需先设置环境变量）
dotnet run

# Docker 构建
docker build -t ddns .

# Docker 运行
docker run -d --name ddns --restart=always \
  -e ALIYUN_ACCESS_KEY_ID="xxx" \
  -e ALIYUN_ACCESS_KEY_SECRET="xxx" \
  -e DNS_DOMAIN="example.com" \
  -e DNS_SUBDOMAIN="home" \
  ddns
```

## 代码架构

项目基于 .NET 8 Worker Service 模板，使用 `Microsoft.Extensions.Hosting` 作为后台服务框架。

### 核心服务

- **DdnsWorker** (`Services/DdnsWorker.cs`): 后台服务主循环，定时检测 IP 变化并触发 DNS 更新
- **IpService** (`Services/IpService.cs`): 通过多个国内 IP 查询服务获取外网 IP，支持故障转移
- **AliyunDnsService** (`Services/AliyunDnsService.cs`): 封装阿里云 DNS API，处理记录查询、创建和更新

### 配置

通过环境变量或 `appsettings.json` 配置，环境变量优先级更高：
- `ALIYUN_ACCESS_KEY_ID` / `Aliyun:AccessKeyId`
- `ALIYUN_ACCESS_KEY_SECRET` / `Aliyun:AccessKeySecret`
- `DNS_DOMAIN` / `Dns:Domain`
- `DNS_SUBDOMAIN` / `Dns:SubDomain`
- `Dns:CheckIntervalSeconds` (默认 60 秒)

### 依赖

- `AlibabaCloud.SDK.Alidns20150109`: 阿里云 DNS API SDK
- `Microsoft.Extensions.Hosting`: Worker Service 框架
- `Microsoft.Extensions.Http`: HttpClient 工厂
