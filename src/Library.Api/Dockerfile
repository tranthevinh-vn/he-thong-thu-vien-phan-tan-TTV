FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Library.Api/Library.Api.csproj", "src/Library.Api/"]
COPY ["src/Library.Data/Library.Data.csproj", "src/Library.Data/"]
COPY ["src/Library.Contracts/Library.Contracts.csproj", "src/Library.Contracts/"]
RUN dotnet restore "src/Library.Api/Library.Api.csproj"

COPY . .
RUN dotnet publish "src/Library.Api/Library.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Library.Api.dll"]
