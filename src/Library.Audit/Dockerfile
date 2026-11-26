FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Library.Audit/Library.Audit.csproj", "src/Library.Audit/"]
COPY ["src/Library.Data/Library.Data.csproj", "src/Library.Data/"]
COPY ["src/Library.Contracts/Library.Contracts.csproj", "src/Library.Contracts/"]
RUN dotnet restore "src/Library.Audit/Library.Audit.csproj"

COPY . .
RUN dotnet publish "src/Library.Audit/Library.Audit.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Library.Audit.dll"]
