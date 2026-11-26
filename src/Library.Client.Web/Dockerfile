FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Library.Client.Web/Library.Client.Web.csproj", "src/Library.Client.Web/"]
COPY ["src/Library.Contracts/Library.Contracts.csproj", "src/Library.Contracts/"]
RUN dotnet restore "src/Library.Client.Web/Library.Client.Web.csproj"

COPY . .
RUN dotnet publish "src/Library.Client.Web/Library.Client.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Library.Client.Web.dll"]
