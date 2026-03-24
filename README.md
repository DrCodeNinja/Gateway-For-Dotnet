# Gateway-For-Dotnet

A production-ready API Gateway built with **ASP.NET Core** and **YARP (Yet Another Reverse Proxy)** that demonstrates the API Gateway pattern for microservices architecture. This project includes JWT authentication, rate limiting, CORS, health checks, load balancing, and request logging — all configured through a clean, environment-based configuration system.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Create Configuration Files](#2-create-configuration-files)
  - [3. Build the Solution](#3-build-the-solution)
  - [4. Run the Services](#4-run-the-services)
- [API Reference](#api-reference)
  - [Gateway Endpoints](#gateway-endpoints)
  - [CatalogService Endpoints](#catalogservice-endpoints)
  - [OrdersService Endpoints](#ordersservice-endpoints)
  - [TokenGenerator Endpoints](#tokengenerator-endpoints)
  - [Health Check Endpoints](#health-check-endpoints)
- [Testing the Gateway](#testing-the-gateway)
  - [Public Endpoints (No Auth Required)](#public-endpoints-no-auth-required)
  - [Protected Endpoints (JWT Required)](#protected-endpoints-jwt-required)
  - [Health Checks](#health-checks)
- [Configuration Guide](#configuration-guide)
  - [Configuration Pattern](#configuration-pattern)
  - [ApiGateway Configuration](#apigateway-configuration)
  - [TokenGenerator Configuration](#tokengenerator-configuration)
  - [Adding a New Microservice](#adding-a-new-microservice)
- [Key Concepts](#key-concepts)
  - [What Is an API Gateway?](#what-is-an-api-gateway)
  - [Why YARP?](#why-yarp)
  - [JWT Authentication Flow](#jwt-authentication-flow)
  - [Rate Limiting](#rate-limiting)
  - [Load Balancing](#load-balancing)
  - [Health Checks](#health-checks-1)
  - [CORS](#cors)
  - [Request Logging](#request-logging)
  - [Middleware Pipeline Order](#middleware-pipeline-order)
- [NuGet Packages](#nuget-packages)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Architecture Overview

```
                          +---------------------+
                          |     Client Apps      |
                          | (Browser, Mobile,    |
                          |  Postman, curl)      |
                          +----------+----------+
                                     |
                                     | HTTP Requests
                                     v
                          +---------------------+
                          |    API Gateway       |
                          |  (http://localhost:  |
                          |       5000)          |
                          |                     |
                          | - Request Logging   |
                          | - CORS              |
                          | - JWT Auth          |
                          | - Rate Limiting     |
                          | - Load Balancing    |
                          | - Health Checks     |
                          | - Reverse Proxy     |
                          +----+----------+-----+
                               |          |
              /api/products/*  |          |  /api/orders/*
                               v          v
                  +-----------+--+  +----+-----------+
                  | CatalogService|  | OrdersService  |
                  | (localhost:   |  | (localhost:    |
                  |  5001)        |  |  5002)         |
                  |               |  |                |
                  | - Products    |  | - Orders       |
                  |   CRUD        |  |   CRUD         |
                  | - Health      |  | - Status Mgmt  |
                  |   Check       |  | - Health Check |
                  +---------------+  +----------------+

                  +-----------------------------------+
                  |        TokenGenerator              |
                  |     (http://localhost:5010)         |
                  |                                    |
                  | - JWT Token Generation API         |
                  | - For development/testing only     |
                  +-----------------------------------+
```

## Project Structure

```
Gateway-For-Dotnet/
|
+-- Gateway-For-Dotnet.sln        # Solution file
+-- .gitignore
+-- README.md
|
+-- ApiGateway/                    # YARP Reverse Proxy Gateway
|   +-- Program.cs                # Gateway setup (auth, CORS, rate limiting, logging)
|   +-- ApiGateway.csproj         # Project file with NuGet dependencies
|   +-- appsettings.json          # Production config with placeholders
|   +-- Properties/
|       +-- launchSettings.json   # Dev server config (port 5000)
|
+-- CatalogService/                # Product microservice
|   +-- Program.cs                # Product CRUD API endpoints
|   +-- CatalogService.csproj
|   +-- appsettings.json
|   +-- Properties/
|       +-- launchSettings.json   # Dev server config (port 5001)
|
+-- OrdersService/                 # Order microservice
|   +-- Program.cs                # Order CRUD API + status management
|   +-- OrdersService.csproj
|   +-- appsettings.json
|   +-- Properties/
|       +-- launchSettings.json   # Dev server config (port 5002)
|
+-- TokenGenerator/                # JWT token generator (dev/test utility)
    +-- Program.cs                # Token generation API endpoints
    +-- TokenGenerator.csproj
    +-- appsettings.json
    +-- Properties/
        +-- launchSettings.json   # Dev server config (port 5010)
```

## Features

| Feature | Description |
|---------|-------------|
| **Reverse Proxy** | YARP-based routing from gateway to downstream microservices |
| **JWT Authentication** | Bearer token validation with configurable issuer, audience, and signing key |
| **Authorization** | Per-route policies — public (`anonymous`) and protected (`authenticated`) routes |
| **Rate Limiting** | Fixed window policies — `fixed` (100 req/min) and `strict` (20 req/min) per route |
| **Load Balancing** | `RoundRobin` and `LeastRequests` policies across cluster destinations |
| **Health Checks** | Gateway-level and YARP cluster-level (active + passive) health monitoring |
| **CORS** | Configurable allowed origins with credentials support |
| **Request Logging** | Logs method, full URL, and client IP for every incoming request |
| **Hot Reload** | YARP configuration can be updated without restarting the gateway |
| **Environment Config** | Placeholder-based `appsettings.json` with environment-specific overrides |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- A terminal (PowerShell, Bash, or Command Prompt)
- (Optional) [curl](https://curl.se/) or [Postman](https://www.postman.com/) for testing

Verify your .NET installation:

```bash
dotnet --version
# Expected: 10.0.x or later
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/DrCodeNinja/Gateway-For-Dotnet.git
cd Gateway-For-Dotnet
```

### 2. Create Configuration Files

The repository only contains `appsettings.json` files with placeholders. You need to create `appsettings.Development.json` files with actual values for local development.

#### ApiGateway/appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.AspNetCore.RateLimiting": "Debug",
      "Yarp": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200"
    ]
  },
  "Jwt": {
    "Issuer": "https://your-auth-server.com",
    "Audience": "api-gateway",
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
  },
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "AuthorizationPolicy": "anonymous",
        "CorsPolicy": "AllowSpecificOrigins",
        "RateLimiterPolicy": "fixed",
        "Match": {
          "Path": "/api/products/{**catch-all}"
        }
      },
      "orders-route": {
        "ClusterId": "orders-cluster",
        "AuthorizationPolicy": "authenticated",
        "CorsPolicy": "AllowSpecificOrigins",
        "RateLimiterPolicy": "strict",
        "Match": {
          "Path": "/api/orders/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": "true",
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          },
          "Passive": {
            "Enabled": "true",
            "Policy": "TransportFailureRate",
            "ReactivationPeriod": "00:01:00"
          }
        },
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001/"
          }
        }
      },
      "orders-cluster": {
        "LoadBalancingPolicy": "LeastRequests",
        "HealthCheck": {
          "Active": {
            "Enabled": "true",
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          },
          "Passive": {
            "Enabled": "true",
            "Policy": "TransportFailureRate",
            "ReactivationPeriod": "00:01:00"
          }
        },
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5002/"
          }
        }
      }
    }
  }
}
```

#### TokenGenerator/appsettings.Development.json

> **Important:** The `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` values must match the ApiGateway configuration exactly, otherwise generated tokens will be rejected by the gateway.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "https://your-auth-server.com",
    "Audience": "api-gateway",
    "DefaultExpiresInHours": 1,
    "DefaultUsername": "testuser"
  }
}
```

#### CatalogService/appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

#### OrdersService/appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### 3. Build the Solution

```bash
dotnet build Gateway-For-Dotnet.sln
```

### 4. Run the Services

Open **four separate terminals** and run each service:

**Terminal 1 — CatalogService (port 5001):**
```bash
dotnet run --project CatalogService
```

**Terminal 2 — OrdersService (port 5002):**
```bash
dotnet run --project OrdersService
```

**Terminal 3 — TokenGenerator (port 5010):**
```bash
dotnet run --project TokenGenerator
```

**Terminal 4 — ApiGateway (port 5000):**
```bash
dotnet run --project ApiGateway
```

> **Note:** Start the downstream services (CatalogService, OrdersService) before the ApiGateway so that health checks pass on startup.

## API Reference

### Gateway Endpoints

All client requests go through the gateway at `http://localhost:5000`.

| Method | Gateway URL | Downstream Service | Auth Required |
|--------|------------|-------------------|---------------|
| GET | `/api/products` | CatalogService | No |
| GET | `/api/products/{id}` | CatalogService | No |
| POST | `/api/products` | CatalogService | No |
| GET | `/api/orders` | OrdersService | **Yes** |
| GET | `/api/orders/{id}` | OrdersService | **Yes** |
| POST | `/api/orders` | OrdersService | **Yes** |
| PUT | `/api/orders/{id}/status` | OrdersService | **Yes** |
| GET | `/health` | Gateway | No |
| GET | `/health/live` | Gateway | No |

### CatalogService Endpoints

Direct access at `http://localhost:5001` (bypasses gateway).

| Method | URL | Description | Request Body |
|--------|-----|-------------|-------------|
| GET | `/api/products` | List all products | — |
| GET | `/api/products/{id}` | Get product by ID | — |
| POST | `/api/products` | Create a product | `{ "name": "", "description": "", "price": 0, "stock": 0 }` |
| GET | `/health` | Health check | — |

### OrdersService Endpoints

Direct access at `http://localhost:5002` (bypasses gateway).

| Method | URL | Description | Request Body |
|--------|-----|-------------|-------------|
| GET | `/api/orders` | List all orders | — |
| GET | `/api/orders/{id}` | Get order by ID | — |
| POST | `/api/orders` | Create an order | `{ "customerId": "", "items": [{ "productId": 0, "productName": "", "quantity": 0, "unitPrice": 0 }] }` |
| PUT | `/api/orders/{id}/status` | Update order status | `{ "status": 0 }` |
| GET | `/health` | Health check | — |

**Order Status Values:** `0` = Pending, `1` = Processing, `2` = Completed, `3` = Cancelled

### TokenGenerator Endpoints

Access at `http://localhost:5010`.

| Method | URL | Description | Request Body |
|--------|-----|-------------|-------------|
| GET | `/api/token/quick` | Generate token with defaults | — |
| POST | `/api/token` | Generate custom token | `{ "username": "", "email": "", "expiresInHours": 1 }` |

**Response format:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-03-24T06:33:59Z",
  "username": "testuser"
}
```

### Health Check Endpoints

| URL | Description | Response |
|-----|-------------|----------|
| `GET /health` | Full health check — pings all downstream services | JSON with status per service |
| `GET /health/live` | Liveness probe — confirms gateway process is running | `Healthy` (no downstream checks) |

**Example `/health` response:**
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "catalog-service", "status": "Healthy", "description": null, "duration": 45.2 },
    { "name": "orders-service", "status": "Healthy", "description": null, "duration": 32.1 }
  ],
  "totalDuration": 47.8
}
```

## Testing the Gateway

### Public Endpoints (No Auth Required)

```bash
# List all products
curl http://localhost:5000/api/products

# Get a single product
curl http://localhost:5000/api/products/1

# Create a product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Webcam", "description": "HD webcam", "price": 59.99, "stock": 80}'
```

### Protected Endpoints (JWT Required)

```bash
# Step 1: Generate a token
TOKEN=$(curl -s http://localhost:5010/api/token/quick | jq -r '.token')

# Or generate a custom token
TOKEN=$(curl -s -X POST http://localhost:5010/api/token \
  -H "Content-Type: application/json" \
  -d '{"username": "john", "email": "john@example.com", "expiresInHours": 2}' | jq -r '.token')

# Step 2: Use the token with protected endpoints
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/orders

curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/orders/1

# Create an order
curl -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"customerId": "CUST-003", "items": [{"productId": 2, "productName": "Mouse", "quantity": 3, "unitPrice": 29.99}]}'

# Update order status
curl -X PUT http://localhost:5000/api/orders/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": 2}'

# Without a token, protected endpoints return 401
curl http://localhost:5000/api/orders
# Response: 401 Unauthorized
```

> **Note for Windows users:** If you don't have `jq`, you can use the `GET /api/token/quick` endpoint in a browser, copy the token value, and use it directly:
> ```bash
> curl -H "Authorization: Bearer eyJhbGciOi..." http://localhost:5000/api/orders
> ```

### Health Checks

```bash
# Full health check (pings downstream services)
curl http://localhost:5000/health

# Liveness probe (just checks if gateway is running)
curl http://localhost:5000/health/live
```

## Configuration Guide

### Configuration Pattern

This project follows a two-file configuration pattern:

| File | Purpose | Committed to Git |
|------|---------|:---------------:|
| `appsettings.json` | Defines structure with `{{PLACEHOLDER}}` values | Yes |
| `appsettings.Development.json` | Contains actual development values | **No** |

ASP.NET Core automatically merges `appsettings.Development.json` on top of `appsettings.json` when running in the Development environment. The placeholder file shows exactly what configuration is required without exposing secrets.

### ApiGateway Configuration

**Placeholder values to replace in `appsettings.Development.json`:**

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{JWT_ISSUER}}` | Token issuer URL | `https://your-auth-server.com` |
| `{{JWT_AUDIENCE}}` | Expected token audience | `api-gateway` |
| `{{JWT_SIGNING_KEY}}` | HMAC-SHA256 signing key (min 32 chars) | `YourSuperSecretKey...` |
| `{{CORS_ORIGIN_1}}` | Allowed CORS origin | `http://localhost:3000` |
| `{{CORS_ORIGIN_2}}` | Additional CORS origin | `http://localhost:4200` |
| `{{CATALOG_SERVICE_URL}}` | CatalogService base URL | `http://localhost:5001/` |
| `{{ORDERS_SERVICE_URL}}` | OrdersService base URL | `http://localhost:5002/` |

### TokenGenerator Configuration

| Placeholder | Description | Must Match |
|-------------|-------------|------------|
| `{{JWT_SIGNING_KEY}}` | Same signing key as ApiGateway | ApiGateway `Jwt:Key` |
| `{{JWT_ISSUER}}` | Same issuer as ApiGateway | ApiGateway `Jwt:Issuer` |
| `{{JWT_AUDIENCE}}` | Same audience as ApiGateway | ApiGateway `Jwt:Audience` |
| `{{DEFAULT_USERNAME}}` | Default username for quick token | Any string |

### Adding a New Microservice

To add a new downstream service to the gateway:

**1. Create the service:**
```bash
dotnet new webapi -n YourService --no-openapi
dotnet sln add YourService/YourService.csproj
```

**2. Add health check support in the new service's `Program.cs`:**
```csharp
builder.Services.AddHealthChecks();
// ... your endpoints ...
app.MapHealthChecks("/health");
```

**3. Set the port in `YourService/Properties/launchSettings.json`:**
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5003"
    }
  }
}
```

**4. Add a route and cluster to both `appsettings.json` (placeholder) and `appsettings.Development.json` (actual value):**

In `ApiGateway/appsettings.json` — add inside `ReverseProxy.Routes`:
```json
"your-route": {
  "ClusterId": "your-cluster",
  "AuthorizationPolicy": "anonymous",
  "CorsPolicy": "AllowSpecificOrigins",
  "RateLimiterPolicy": "fixed",
  "Match": {
    "Path": "/api/your-resource/{**catch-all}"
  }
}
```

Add inside `ReverseProxy.Clusters`:
```json
"your-cluster": {
  "LoadBalancingPolicy": "RoundRobin",
  "HealthCheck": {
    "Active": {
      "Enabled": "true",
      "Interval": "00:00:30",
      "Timeout": "00:00:10",
      "Policy": "ConsecutiveFailures",
      "Path": "/health"
    },
    "Passive": {
      "Enabled": "true",
      "Policy": "TransportFailureRate",
      "ReactivationPeriod": "00:01:00"
    }
  },
  "Destinations": {
    "destination1": {
      "Address": "{{YOUR_SERVICE_URL}}"
    }
  }
}
```

**5. Add the health check in `ApiGateway/Program.cs`:**
```csharp
var yourServiceUrl = builder.Configuration["ReverseProxy:Clusters:your-cluster:Destinations:destination1:Address"]!;

builder.Services.AddHealthChecks()
    // ... existing checks ...
    .AddUrlGroup(new Uri($"{yourServiceUrl}health"), name: "your-service", tags: ["service"]);
```

**6. Provide the actual URL in `appsettings.Development.json`** and restart the gateway.

## Key Concepts

### What Is an API Gateway?

An API Gateway is a single entry point that sits between clients and backend microservices. Instead of clients calling each microservice directly, they send all requests to the gateway, which handles routing, security, and cross-cutting concerns.

**Without a gateway:**
```
Client --> CatalogService (auth + rate limit + CORS)
Client --> OrdersService  (auth + rate limit + CORS)
Client --> UserService     (auth + rate limit + CORS)
```

**With a gateway:**
```
Client --> API Gateway (auth + rate limit + CORS) --> CatalogService
                                                  --> OrdersService
                                                  --> UserService
```

### Why YARP?

[YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/) is Microsoft's official reverse proxy library for .NET. Compared to alternatives:

| Feature | YARP | Ocelot |
|---------|------|--------|
| Maintained by | Microsoft | Community |
| Performance | Excellent (Kestrel-native) | Good |
| HTTP/2 & HTTP/3 | Yes | No |
| Config style | JSON + Programmatic | JSON only |
| ASP.NET Core integration | Native middleware | Separate middleware |
| Hot reload | Yes | Limited |

### JWT Authentication Flow

```
1. Client requests a token from TokenGenerator
   POST http://localhost:5010/api/token
   --> Returns: { "token": "eyJhbG..." }

2. Client sends request to gateway with token
   GET http://localhost:5000/api/orders
   Header: Authorization: Bearer eyJhbG...

3. Gateway validates the token:
   - Checks signature (using Jwt:Key)
   - Checks issuer (matches Jwt:Issuer)
   - Checks audience (matches Jwt:Audience)
   - Checks expiration (not expired)

4. If valid: request is proxied to OrdersService
   If invalid: 401 Unauthorized is returned
```

### Rate Limiting

Two fixed-window policies are configured:

| Policy | Limit | Window | Queue | Applied To |
|--------|-------|--------|-------|-----------|
| `fixed` | 100 requests | 1 minute | 10 queued | Product routes (public) |
| `strict` | 20 requests | 1 minute | 5 queued | Order routes (protected) |

When the limit is exceeded, the gateway returns `429 Too Many Requests`.

### Load Balancing

YARP supports multiple load balancing strategies per cluster:

| Policy | Behavior | Used By |
|--------|----------|---------|
| `RoundRobin` | Distributes requests evenly across destinations | catalog-cluster |
| `LeastRequests` | Sends to the destination with fewest active requests | orders-cluster |

To add multiple destinations for load balancing, add more entries under `Destinations`:
```json
"Destinations": {
  "destination1": { "Address": "http://localhost:5001/" },
  "destination2": { "Address": "http://localhost:5003/" }
}
```

### Health Checks

**Gateway-level** (`/health` and `/health/live`):
- `/health` — Pings all downstream services and returns a JSON report
- `/health/live` — Liveness probe that confirms the gateway process is running (useful for Kubernetes)

**YARP cluster-level** (configured per cluster):
- **Active checks** — YARP pings each destination's `/health` endpoint every 30 seconds. Unhealthy destinations are automatically removed from load balancing.
- **Passive checks** — YARP monitors transport failures in real-time. Failed destinations are deactivated for 1 minute before retrying.

### CORS

Two CORS policies are available:

| Policy | Behavior | Configuration |
|--------|----------|---------------|
| `AllowSpecificOrigins` | Allows only configured origins with credentials | Origins from `Cors:AllowedOrigins` in config |
| `AllowAll` | Allows any origin (no credentials) | Available but not applied by default |

CORS is applied globally via `app.UseCors()` and per-route via `CorsPolicy` in YARP route configuration.

### Request Logging

Every incoming request is logged with:
- HTTP method (GET, POST, etc.)
- Full URL (scheme + host + path + query string)
- Client IP address

Example log output:
```
info: Program[0]
      Request: GET http://localhost:5000/api/products from ::1
info: Program[0]
      Request: POST http://localhost:5000/api/orders from 192.168.1.50
```

### Middleware Pipeline Order

The gateway processes each request through this pipeline in order:

```
Incoming Request
    |
    v
[Request Logging]     --> Logs method, URL, client IP
    |
    v
[CORS]                --> Validates origin headers
    |
    v
[Authentication]      --> Validates JWT token
    |
    v
[Authorization]       --> Checks route authorization policy
    |
    v
[Rate Limiter]        --> Enforces request limits
    |
    v
[Health Checks]       --> Responds to /health endpoints
    |
    v
[YARP Reverse Proxy]  --> Routes to downstream service
```

## NuGet Packages

### ApiGateway

| Package | Version | Purpose |
|---------|---------|---------|
| [Yarp.ReverseProxy](https://www.nuget.org/packages/Yarp.ReverseProxy) | 2.3.0 | Reverse proxy and load balancing |
| [Microsoft.AspNetCore.Authentication.JwtBearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 10.0.5 | JWT Bearer token authentication |
| [AspNetCore.HealthChecks.Uris](https://www.nuget.org/packages/AspNetCore.HealthChecks.Uris) | 9.0.0 | URI-based health checks for downstream services |

### TokenGenerator

| Package | Version | Purpose |
|---------|---------|---------|
| [System.IdentityModel.Tokens.Jwt](https://www.nuget.org/packages/System.IdentityModel.Tokens.Jwt) | 8.16.0 | JWT token creation and signing |

### CatalogService & OrdersService

No additional NuGet packages — uses only the built-in ASP.NET Core framework.

## Troubleshooting

| Problem | Cause | Solution |
|---------|-------|---------|
| `401 Unauthorized` on orders endpoints | Missing or invalid JWT token | Generate a token using TokenGenerator and include it in the `Authorization: Bearer` header |
| `429 Too Many Requests` | Rate limit exceeded | Wait for the rate limit window to reset (1 minute) |
| `502 Bad Gateway` | Downstream service is not running | Start the CatalogService or OrdersService first |
| Health check shows `Unhealthy` | Downstream service is down or unreachable | Verify the service is running on the correct port |
| JWT token rejected | Mismatched signing key, issuer, or audience | Ensure `Jwt` settings in TokenGenerator and ApiGateway `appsettings.Development.json` are identical |
| Build fails with file locked error | Previous service instance still running | Stop all running services (`taskkill` or `Ctrl+C`) and rebuild |
| CORS error in browser | Origin not in allowed list | Add your frontend URL to `Cors:AllowedOrigins` in the gateway config |

## Service Ports Summary

| Service | Port | URL |
|---------|------|-----|
| ApiGateway | 5000 | `http://localhost:5000` |
| CatalogService | 5001 | `http://localhost:5001` |
| OrdersService | 5002 | `http://localhost:5002` |
| TokenGenerator | 5010 | `http://localhost:5010` |

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -m 'Add your feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Open a Pull Request

## License

This project is open source and available under the [MIT License](LICENSE).
