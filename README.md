# Sharing is caring
Sharing is caring is reservation system for shared resources (apartments, spaces, equipment, etc.).

## Features
- Registered users can reserve/book a resource for an arbitrary amount of time, can be multi-day down to the minute.
- The system prevents overlap between reservations.
- Users can see eachothers reservations for a space
- The system uses existing identity providers (Microsoft, GitHub) instead of storing credentials.
- The system is web based
- Works great on mobile
- Costs very little to run on a cloud platform
- Resources have a name, description, image and category. Categories have a name and an icon. There is no other hierarchy
- Bookings have a short title (30 chars) and a slightly longer description (~1k), both optional
- There is a calendar view and an agenda view, with filters for the resources
- Scales well from 5-1000 users
- Cheaply deployable on Azure using free tiers. Uses well-known tech stack

## User authorization setup 
- The first user to log in will become the first administrator
- After the administrator is established, users can only join using an (expiring) invite link
- Users will have a 'profile' to set their display name
- There will always be at least one administrator
- There is role based access control at the app level, and at the resource level:
    - App level:
        - User administrator (user CRUD, including app RBAC)
        - Category administrator (category CRUD)
        - Resource administrator (resource CRUD and resource RBAC)
    - Per resource:
        - User (CRUD their own bookings)
        - Manager (CRUD everyone's bookings for this resource)

## Not in scope for this version
- recurring bookings
- resource hierarchy
- custom resource attributes for filters
- booking rules per resource/user, such as:
    - min/max duration
    - max book ahead time
    - only past/future
- booking approval workflow
- notifications
- auto purging/cleanup
- multi-tenancy (multiple resource pools / user groups using the same system)
- resource deactivation (temporarily unavailable or 'invisible')
- bulk operations
- multi-timezone (backend stores everything in UTC, frontend translates to browser timezone)

## Possible applications
- Apartment sharing
- (Meeting) room booking
- Tool/equipment sharing
- Boat booking for rowing clubs

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19 + TypeScript + Vite |
| Backend | C# / .NET 8 (Azure Functions, isolated worker) |
| Database | Azure Cosmos DB (serverless) |
| Auth | Azure Static Web Apps built-in OAuth (Microsoft + GitHub) |
| Hosting | Azure Static Web Apps (free tier) |
| IaC | Bicep |
| CI/CD | GitHub Actions |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure Static Web Apps CLI](https://github.com/Azure/static-web-apps-cli): `npm install -g @azure/static-web-apps-cli`
- [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator) (for local development)

## Local Development

1. **Start the Cosmos DB Emulator** (or use a cloud Cosmos DB instance).

2. **Create the database and container** in the emulator:
   - Database: `sic`
   - Container: `sic-data` with partition key `/pk`

3. **Restore and build:**
   ```bash
   dotnet restore Sic.sln
   dotnet build Sic.sln
   cd src/web && npm install
   ```

4. **Run locally with SWA CLI:**
   ```bash
   swa start
   ```
   This starts the Vite dev server, the Azure Functions API, and the SWA proxy (with auth emulation) wired together. The app will be available at `http://localhost:4280`.

5. **Run tests:**
   ```bash
   dotnet test Sic.sln
   ```

## Deploy to Azure

### 1. Create the infrastructure

```bash
az login
az group create --name sic-rg --location westeurope
az deployment group create \
  --resource-group sic-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam
```

This creates:
- A Cosmos DB serverless account with the `sic` database and `sic-data` container
- An Azure Static Web App (free tier)
- App settings linking the Cosmos DB connection string to the SWA

### 2. Get the SWA deployment token

```bash
az staticwebapp secrets list --name sic-swa --resource-group sic-rg --query "properties.apiKey" -o tsv
```

### 3. Configure GitHub Actions

Add the token as a repository secret named `AZURE_STATIC_WEB_APPS_API_TOKEN` in your GitHub repo settings (Settings > Secrets and variables > Actions).

### 4. Push to deploy

Pushing to `master` triggers the CI/CD pipeline which:
1. Builds and tests the .NET backend
2. Builds the React frontend
3. Deploys both to Azure Static Web Apps

Pull requests run the build and test steps without deploying.

### 5. Configure authentication providers

In the Azure portal, go to your Static Web App > Settings > Identity:
- Enable **Microsoft** (Azure AD) and/or **GitHub** as identity providers
- No additional configuration is needed — SWA handles the OAuth flow on the free tier
- Google and custom OIDC providers require the Standard plan ($9/month)

## Project Structure

```
├── src/
│   ├── api/
│   │   ├── Sic.Core/          # Domain models, services, repository interfaces
│   │   ├── Sic.Api/           # Azure Functions HTTP endpoints
│   │   └── Sic.Cosmos/        # Cosmos DB repository implementations
│   └── web/                   # React + Vite frontend
├── tests/
│   ├── Sic.Core.Tests/        # Service-layer unit tests
│   └── Sic.Api.Tests/         # API-layer unit tests
├── infra/                     # Bicep templates
├── .github/workflows/         # CI/CD pipeline
└── DESIGN.md                  # Detailed design document
```

## Cost Estimate

Running on Azure free/serverless tiers:
- **Static Web Apps**: Free tier (100 GB bandwidth, custom domains, SSL)
- **Cosmos DB**: Serverless (~$0.25 per million RU, pay only for requests)
- **Estimated monthly cost for <100 users**: Under $1/month