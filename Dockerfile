FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AddressBook.slnx ./
COPY src/AddressBook.Api/AddressBook.Api.csproj src/AddressBook.Api/
RUN dotnet restore src/AddressBook.Api/AddressBook.Api.csproj
COPY src/ src/
RUN dotnet publish src/AddressBook.Api/AddressBook.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AddressBook.Api.dll"]
