FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/RevolaAgent.Api -c Release -o /out/api
RUN dotnet publish src/RevolaAgent.Worker -c Release -o /out/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RevolaAgent.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /out/worker .
USER $APP_UID
ENTRYPOINT ["dotnet", "RevolaAgent.Worker.dll"]
