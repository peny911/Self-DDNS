FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Self-DDNS.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ALIYUN_ACCESS_KEY_ID=""
ENV ALIYUN_ACCESS_KEY_SECRET=""
ENV DNS_DOMAIN=""
ENV DNS_SUBDOMAIN=""

ENTRYPOINT ["dotnet", "Self-DDNS.dll"]
