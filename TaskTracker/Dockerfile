FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["TaskTracker/TaskTracker.Web.Api.csproj", "TaskTracker/"]
COPY ["TaskTracker.Core/TaskTracker.Core.csproj", "TaskTracker.Core/"]
COPY ["TaskTracker.Utils/TaskTracker.Utils.csproj", "TaskTracker.Utils/"]
RUN dotnet restore "TaskTracker/TaskTracker.Web.Api.csproj"

COPY . .
WORKDIR "/src/TaskTracker"
RUN dotnet publish "TaskTracker.Web.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN mkdir -p /app/logs && chown -R app:app /app
USER app
COPY --chown=app:app --from=build /app/publish .
ENTRYPOINT ["dotnet", "TaskTracker.Web.Api.dll"]
