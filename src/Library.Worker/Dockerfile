FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Library.Worker/Library.Worker.csproj", "src/Library.Worker/"]
COPY ["src/Library.Data/Library.Data.csproj", "src/Library.Data/"]
COPY ["src/Library.Contracts/Library.Contracts.csproj", "src/Library.Contracts/"]
RUN dotnet restore "src/Library.Worker/Library.Worker.csproj"

COPY . .
RUN dotnet publish "src/Library.Worker/Library.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Library.Worker.dll"]
