# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy các file csproj để restore dependencies
COPY src/HotelBookingApp.API/*.csproj ./src/HotelBookingApp.API/
COPY src/HotelBookingApp.Application/*.csproj ./src/HotelBookingApp.Application/
COPY src/HotelBookingApp.Domain/*.csproj ./src/HotelBookingApp.Domain/
COPY src/HotelBookingApp.Infrastructure/*.csproj ./src/HotelBookingApp.Infrastructure/
COPY src/HotelBookingApp.Shared/*.csproj ./src/HotelBookingApp.Shared/

RUN dotnet restore src/HotelBookingApp.API/HotelBookingApp.API.csproj

# Copy toàn bộ mã nguồn và build
COPY src/ ./src/
WORKDIR /app/src/HotelBookingApp.API
RUN dotnet publish -c Release -o /app/out

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Mở cổng mặc định của ASP.NET Core 8/9
EXPOSE 8080

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "HotelBookingApp.API.dll"]
