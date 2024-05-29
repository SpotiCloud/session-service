# Use the official ASP.NET runtime image as a base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the .csproj file and restore dependencies
COPY ["SessionService/SessionService.csproj", "SessionService/"]
RUN dotnet restore "SessionService/SessionService.csproj"

# Copy the rest of the application code
COPY . .
WORKDIR "/src/SessionService"
RUN dotnet build "SessionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SessionService.csproj" -c Release -o /app/publish

# Use the base image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SessionService.dll"]