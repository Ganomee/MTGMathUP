# Multi-stage build for MTG Mullagain Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY MtgMullagain.sln ./
COPY src/MtgMullagain.Api/MtgMullagain.Api.csproj ./src/MtgMullagain.Api/
COPY src/MtgMullagain.Core/MtgMullagain.Core.csproj ./src/MtgMullagain.Core/
COPY src/MtgMullagain.Infrastructure/MtgMullagain.Infrastructure.csproj ./src/MtgMullagain.Infrastructure/
COPY tests/MtgMullagain.Tests/MtgMullagain.Tests.csproj ./tests/MtgMullagain.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish ./src/MtgMullagain.Api -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /out .

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "MtgMullagain.Api.dll"]
