FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Dinder.Api/Dinder.Api.csproj src/Dinder.Api/
COPY src/Dinder.Domain/Dinder.Domain.csproj src/Dinder.Domain/
COPY src/Dinder.Application/Dinder.Application.csproj src/Dinder.Application/
COPY src/Dinder.Infrastructure/Dinder.Infrastructure.csproj src/Dinder.Infrastructure/
COPY src/Dinder.Contracts/Dinder.Contracts.csproj src/Dinder.Contracts/
RUN dotnet restore src/Dinder.Api/Dinder.Api.csproj
COPY . .
WORKDIR /src/src/Dinder.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Dinder.Api.dll"]
