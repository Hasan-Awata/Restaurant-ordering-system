# 1. Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 2. Copy the solution and project files first (for caching)
COPY ["Backend/OrderingSystem/OrderingSystem.csproj", "Backend/OrderingSystem/"]
COPY ["Backend/OrderingSystem.Application/OrderingSystem.Application.csproj", "Backend/OrderingSystem.Application/"]
COPY ["Backend/OrderingSystem.Domain/OrderingSystem.Domain.csproj", "Backend/OrderingSystem.Domain/"]
COPY ["Backend/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj", "Backend/OrderingSystem.Infrastructure/"]

# 3. Restore dependencies
RUN dotnet restore "Backend/OrderingSystem/OrderingSystem.csproj"

# 4. Copy the rest of the code and build
COPY . .
WORKDIR "/src/Backend/OrderingSystem"
RUN dotnet publish "OrderingSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 5. Use the lightweight runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 6. Expose the standard port and run it
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OrderingSystem.dll"]