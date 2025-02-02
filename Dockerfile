# Use the appropriate .NET SDK that supports .NET 9.0 (if available)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ["TagFlowApi/TagFlowApi.csproj", "TagFlowApi/"]
RUN dotnet restore "TagFlowApi/TagFlowApi.csproj"

COPY . .
WORKDIR "/src/TagFlowApi"
RUN dotnet publish "TagFlowApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TagFlowApi.dll"]
