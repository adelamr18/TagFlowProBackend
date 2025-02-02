# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory in the container
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["TagFlowApi/TagFlowApi.csproj", "TagFlowApi/"]
RUN dotnet restore "TagFlowApi/TagFlowApi.csproj"

# Copy the rest of the source code
COPY . .

# Set the working directory to the folder containing the .csproj file
WORKDIR /src/TagFlowApi

# Publish the app to the /app/publish directory
RUN dotnet publish "TagFlowApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Set the working directory for the runtime container
WORKDIR /app

# Expose port 8080 for the application
EXPOSE 8080

# Copy the published files from the build stage
COPY --from=build /app/publish .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "TagFlowApi.dll"]
