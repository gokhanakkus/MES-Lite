# --- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching, then restore.
COPY MESLite.slnx ./
COPY backend/ ./backend/
RUN dotnet restore backend/MESLite.API/MESLite.API.csproj

RUN dotnet publish backend/MESLite.API/MESLite.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
ENV Database__Provider=SqlServer
EXPOSE 8080
ENTRYPOINT ["dotnet", "MESLite.API.dll"]
