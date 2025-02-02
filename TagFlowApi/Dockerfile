# Use the .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY ["TagFlowBackend/TagFlowBackend.csproj", "TagFlowBackend/"]
RUN dotnet restore "TagFlowBackend/TagFlowBackend.csproj"
COPY . .
WORKDIR "/src/TagFlowBackend"
RUN dotnet build "TagFlowBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TagFlowBackend.csproj" -c Release -o /app/publish

# Use the runtime image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80  # This line should have no comment in the same line, just the port exposed

# Copy the published application from the build stage
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TagFlowBackend.dll"]
