FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY API/Api.csproj API/
COPY Service/Service.csproj Service/
COPY Infra/Infrastructure.csproj Infra/

RUN dotnet restore API/Api.csproj

COPY . .

RUN dotnet publish API/Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out ./

RUN mkdir -p /app/data && chown -R app:app /app/data

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=4000


EXPOSE 4000

ENTRYPOINT ["dotnet", "Api.dll"]