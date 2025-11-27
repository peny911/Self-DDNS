FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Self-DDNS.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish Self-DDNS.csproj -c Release -o /app/publish -p:PublishSingleFile=false -p:SelfContained=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV TZ=Asia/Shanghai
ENV ALIYUN_ACCESS_KEY_ID=""
ENV ALIYUN_ACCESS_KEY_SECRET=""
ENV DNS_DOMAIN=""
ENV DNS_SUBDOMAIN=""

ENTRYPOINT ["dotnet", "Self-DDNS.dll"]
