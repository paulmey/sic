# Design

## Tech Stack

| Layer | Technology | Rationale |
|---|---|---|
| Frontend | React + TypeScript | Largest ecosystem, best calendar component libraries (FullCalendar, react-big-calendar). TypeScript for type safety. |
| Backend | C# / .NET 8 (Azure Functions, isolated worker) | Strong typing, first-class Azure support, familiar to the team. |
| Database | Azure Cosmos DB (NoSQL) | Permanent free tier (1000 RU/s, 25 GB). Document model fits the domain well. |
| Auth | Azure Static Web Apps built-in auth | Zero-code OAuth for Microsoft + Google. Provides `/.auth/me` endpoint and `x-ms-client-principal` header to the API. |
| Hosting | Azure Static Web Apps (free tier) | Serves React frontend via CDN, routes `/api/*` to Azure Functions. Free SSL, custom domains. |

### Portability to AWS

The architecture is designed to be portable. The equivalent AWS stack:

| Layer | Azure | AWS |
|---|---|---|
| Frontend hosting | Static Web Apps | S3 + CloudFront |
| API | Azure Functions (C#) | Lambda (C#) |
| Database | Cosmos DB | DynamoDB |
| Auth | SWA built-in auth | Cognito |

To enable this, the data layer uses a repository abstraction (see Data Access Layer below).

## Data Model

### Entities

#### User
| Field | Type | Notes |
|---|---|---|
| id | string | GUID |
| identityProvider | string | "microsoft" or "google" |
| identityId | string | Subject ID from the identity provider |
| displayName | string | User-chosen display name |
| appRoles | string[] | Zero or more of: `user-admin`, `category-admin`, `resource-admin` |
| createdAt | DateTimeOffset | |

Partition key: `id`

#### Category
| Field | Type | Notes |
|---|---|---|
| id | string | GUID |
| name | string | Display name |
| icon | string | Icon identifier (e.g. emoji or icon library name) |
| createdAt | DateTimeOffset | |

Partition key: `id`

#### Resource
| Field | Type | Notes |
|---|---|---|
| id | string | GUID |
| categoryId | string | FK to Category |
| name | string | Display name |
| description | string | Longer description |
| imageUrl | string | URL to resource image |
| createdAt | DateTimeOffset | |

Partition key: `id`

#### ResourceRole
| Field | Type | Notes |
|---|---|---|
| id | string | GUID |
| resourceId | string | FK to Resource |
| userId | string | FK to User |
| role | string | `user` or `manager` |

Partition key: `resourceId`

#### Booking
| Field | Type | Notes |
|---|---|---|
| id | string | GUID |
| resourceId | string | FK to Resource |
| userId | string | FK to User, the creator |
| title | string | Optional, max 30 chars |
| description | string | Optional, max ~1000 chars |
| startTime | DateTimeOffset | UTC |
| endTime | DateTimeOffset | UTC |
| createdAt | DateTimeOffset | |

Partition key: `resourceId`

Partitioning bookings by `resourceId` means overlap queries (`all bookings for resource X in time range`) hit a single partition — fast and cheap.

#### InviteLink
| Field | Type | Notes |
|---|---|---|
| id | string | GUID, used as the invite token |
| createdByUserId | string | FK to User |
| expiresAt | DateTimeOffset | |
| usedByUserId | string | Null until used |
| createdAt | DateTimeOffset | |

Partition key: `id`

### Cosmos DB Container Strategy

All entities share a single container using a `type` discriminator field and a hierarchical partition key:

- **Container**: `sic-data`
- **Partition key**: `/pk`

Each document gets a `type` field (`user`, `category`, `resource`, `resource-role`, `booking`, `invite`) and a `pk` field set to a sensible value:

| Type | pk value | Rationale |
|---|---|---|
| user | `user:{id}` | User lookups by ID |
| category | `category:{id}` | Category lookups by ID |
| resource | `resource:{id}` | Resource lookups by ID |
| resource-role | `resource:{resourceId}` | List roles per resource in one query |
| booking | `resource:{resourceId}` | All bookings for a resource in one partition |
| invite | `invite:{id}` | Invite lookups by token |

Listing all items of a type (e.g. all categories) uses a cross-partition query with a `type` filter — acceptable at this scale.

## Data Access Layer

Repository interfaces abstract storage. Business logic depends only on interfaces:

```
IUserRepository
ICategoryRepository
IResourceRepository
IResourceRoleRepository
IBookingRepository
IInviteLinkRepository
```

Each has a Cosmos DB implementation. Alternative implementations (DynamoDB, SQL) can be added without changing business logic.

### Overlap Prevention

The `IBookingRepository.CreateAsync` method must guarantee no overlapping bookings for the same resource. In Cosmos DB this is handled by:

1. Querying existing bookings for the resource where the time range overlaps.
2. If none found, inserting the new booking.
3. Using an optimistic concurrency approach: if a conflicting booking was inserted between steps 1 and 2, the operation is retried or rejected.

Since all bookings for a resource share a partition, this query is efficient.

## API Design

All endpoints are under `/api`. Authentication is enforced by Azure Static Web Apps — unauthenticated requests to `/api/*` are rejected.

### Auth
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/me` | Get current user profile + roles | Any authenticated |
| PUT | `/api/me` | Update display name | Any authenticated |
| POST | `/api/invite/redeem` | Redeem an invite link | Unauthenticated (becomes authenticated) |

### Categories
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/categories` | List all categories | Any user |
| POST | `/api/categories` | Create a category | `category-admin` |
| PUT | `/api/categories/{id}` | Update a category | `category-admin` |
| DELETE | `/api/categories/{id}` | Delete a category | `category-admin` |

### Resources
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/resources` | List all resources (with optional category filter) | Any user |
| GET | `/api/resources/{id}` | Get resource details | Any user |
| POST | `/api/resources` | Create a resource | `resource-admin` |
| PUT | `/api/resources/{id}` | Update a resource | `resource-admin` |
| DELETE | `/api/resources/{id}` | Delete a resource | `resource-admin` |

### Resource Roles
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/resources/{id}/roles` | List roles for a resource | `resource-admin` or `manager` of resource |
| POST | `/api/resources/{id}/roles` | Assign a role to a user | `resource-admin` |
| DELETE | `/api/resources/{id}/roles/{userId}` | Remove a role | `resource-admin` |

### Bookings
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/resources/{id}/bookings?from=&to=` | List bookings for a resource in a time range | Any user with resource role |
| POST | `/api/resources/{id}/bookings` | Create a booking | `user` or `manager` of resource |
| PUT | `/api/resources/{id}/bookings/{bookingId}` | Update a booking | Owner or `manager` of resource |
| DELETE | `/api/resources/{id}/bookings/{bookingId}` | Delete a booking | Owner or `manager` of resource |

### Users (Admin)
| Method | Path | Description | Required Role |
|---|---|---|---|
| GET | `/api/users` | List all users | `user-admin` |
| PUT | `/api/users/{id}/roles` | Update app-level roles for a user | `user-admin` |
| DELETE | `/api/users/{id}` | Remove a user | `user-admin` |

### Invite Links
| Method | Path | Description | Required Role |
|---|---|---|---|
| POST | `/api/invites` | Create an invite link | `user-admin` |
| GET | `/api/invites` | List active invites | `user-admin` |
| DELETE | `/api/invites/{id}` | Revoke an invite | `user-admin` |

## Project Structure

```
sic/
├── README.md
├── DESIGN.md
├── .gitignore
├── src/
│   ├── api/                          # C# Azure Functions project
│   │   ├── Sic.Api/                  # Functions (HTTP triggers)
│   │   ├── Sic.Core/                 # Domain models, interfaces, business logic
│   │   └── Sic.Data.Cosmos/          # Cosmos DB repository implementations
│   └── web/                          # React frontend
│       ├── src/
│       │   ├── components/           # Reusable UI components
│       │   ├── pages/                # Page-level components
│       │   ├── services/             # API client
│       │   └── hooks/                # Custom React hooks
│       └── package.json
├── infra/                            # Infrastructure as code (Bicep)
└── tests/
    ├── Sic.Core.Tests/               # Unit tests
    └── Sic.Api.Tests/                # Integration tests
```

## First-User Bootstrap Flow

1. The app is deployed with no data.
2. The first user authenticates via Microsoft or Google.
3. The API detects no users exist in the database.
4. The user is created with all admin roles (`user-admin`, `category-admin`, `resource-admin`).
5. From this point on, new users can only join via invite links created by a `user-admin`.
