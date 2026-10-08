The API should be designed around these principles:
- /api/v1/... for application APIs
- /s/{code} for public human-facing share URLs
- JWT access tokens for authenticated APIs
- Refresh-token rotation
- Resource/owner authorization for snippets
- Consistent error responses using Problem Details
- Cursor/page-based pagination for collections
- No sensitive data in URLs except the opaque share code
- Explicit 201/204/400/401/403/404/409/429 semantics
1. Complete endpoint map
TextShare API
│
├── /api/v1/auth
│   ├── POST   /register
│   ├── POST   /login
│   ├── POST   /refresh
│   ├── POST   /logout
│   └── POST   /logout-all
│
├── /api/v1/users
│   ├── GET    /me
│   ├── PATCH  /me
│   ├── PATCH  /me/password
│   └── DELETE /me
│
├── /api/v1/snippets
│   ├── POST   /
│   ├── GET    /
│   ├── GET    /{snippetId}
│   ├── PATCH  /{snippetId}
│   ├── DELETE /{snippetId}
│   ├── POST   /{snippetId}/links
│   └── GET    /{snippetId}/links
│
├── /api/v1/share-links
│   ├── GET    /{linkId}
│   ├── PATCH  /{linkId}
│   └── DELETE /{linkId}
│
├── /s
│   └── GET    /{code}
│
├── /api/v1/admin
│   ├── GET    /users
│   ├── GET    /users/{userId}
│   ├── PATCH  /users/{userId}/status
│   ├── PATCH  /users/{userId}/role
│   ├── GET    /access-logs
│   └── GET    /refresh-tokens
│
└── /health
    ├── GET    /
    └── GET    /ready

I would not expose every database operation as an endpoint. The API should represent business capabilities, not tables.
2. Authentication API
POST /api/v1/auth/register
Creates a user and establishes a session.
Request
{
  "email": "shashank@example.com",
  "password": "VeryStrongPassword123!",
  "displayName": "Shashank"
}

Response — 201 Created
{
  "user": {
    "id": "0d9b9c9c-2e84-4ca4-8f3e-7c9d08e5d3b2",
    "email": "shashank@example.com",
    "displayName": "Shashank",
    "role": "user"
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAtUtc": "2026-10-08T08:20:00Z",
  "refreshToken": "..."
}

Possible responses
201 Created
400 Bad Request
409 Conflict          -> email already exists
422 Unprocessable Entity -> validation failure
429 Too Many Requests

I would return a generic account-existence error rather than exposing internal user information where appropriate.
3. POST /api/v1/auth/login
Authenticates the user.
Request
{
  "email": "shashank@example.com",
  "password": "VeryStrongPassword123!"
}

Response — 200 OK
{
  "user": {
    "id": "0d9b9c9c-2e84-4ca4-8f3e-7c9d08e5d3b2",
    "email": "shashank@example.com",
    "displayName": "Shashank",
    "role": "user"
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAtUtc": "2026-10-08T08:20:00Z",
  "refreshToken": "..."
}

Errors
401 Unauthorized
429 Too Many Requests

Don't return:
{
  "error": "email exists but password is wrong"
}

Use the same generic credential error for failed authentication.
4. POST /api/v1/auth/refresh
Rotates the refresh token and issues a new access token.
Request
{
  "refreshToken": "..."
}

Response
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAtUtc": "2026-10-08T08:20:00Z",
  "refreshToken": "new-refresh-token"
}

Important behavior
Old RT-1
   ↓
revoked
   ↓
RT-2 created
   ↓
same token family

If previously-used RT-1 is submitted again:
detect reuse
       ↓
revoke token family
       ↓
401 Unauthorized

This is an important security feature for your project.
5. POST /api/v1/auth/logout
Revokes the supplied refresh token.
Request
{
  "refreshToken": "..."
}

Response
204 No Content

The endpoint can be anonymous because the refresh token itself identifies the session.
6. POST /api/v1/auth/logout-all
Requires authentication.
Authorization: Bearer <access-token>

Response
204 No Content

This revokes all active refresh tokens belonging to the current user.
Useful when:
User suspects account compromise
User changed password
User wants to sign out everywhere

7. User API
GET /api/v1/users/me
Returns current authenticated user.
Response
{
  "id": "0d9b9c9c-2e84-4ca4-8f3e-7c9d08e5d3b2",
  "email": "shashank@example.com",
  "displayName": "Shashank",
  "role": "user",
  "isActive": true,
  "createdAtUtc": "2026-10-01T09:30:00Z",
  "lastLoginAtUtc": "2026-10-08T08:00:00Z"
}

8. PATCH /api/v1/users/me
Updates profile information.
Request
{
  "displayName": "Shashank Kumar"
}

Response
200 OK

9. PATCH /api/v1/users/me/password
Password change should be a separate endpoint.
Request
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}

Server behavior
verify current password
        ↓
hash new password using BCrypt
        ↓
update user
        ↓
revoke all refresh tokens

Response
204 No Content

I strongly recommend revoking all sessions after a password change.
10. DELETE /api/v1/users/me
Deletes/deactivates the user's account.
I would actually implement soft deletion/deactivation initially rather than physically deleting the user immediately.
Response
204 No Content

Business rule:
User deactivated
      ↓
cannot login
      ↓
all refresh tokens revoked
      ↓
existing snippets become inaccessible to owner

Whether snippets are permanently deleted or retained is a product decision.
11. Snippet API
This is the main application API.
POST /api/v1/snippets
Creates a text snippet.
Request
{
  "title": "My ASP.NET Notes",
  "content": "Entity Framework Core uses DbContext for database access...",
  "expiresAtUtc": "2026-10-15T18:30:00Z",
  "maxViews": 100
}

expiresAtUtc and maxViews can be nullable.
Response — 201 Created
{
  "id": "d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d",
  "title": "My ASP.NET Notes",
  "content": "Entity Framework Core uses DbContext for database access...",
  "expiresAtUtc": "2026-10-15T18:30:00Z",
  "maxViews": 100,
  "viewCount": 0,
  "createdAtUtc": "2026-10-08T08:10:00Z",
  "updatedAtUtc": "2026-10-08T08:10:00Z"
}

Header:
Location: /api/v1/snippets/d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d

12. GET /api/v1/snippets
Returns snippets owned by the current user.
Query parameters
pageSize
cursor
search
sortBy
sortDirection
status

Example:
GET /api/v1/snippets?pageSize=20&search=aspnet&status=active

I prefer cursor pagination for a production API over relying exclusively on page numbers.
Example response:
{
  "items": [
    {
      "id": "d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d",
      "title": "ASP.NET Notes",
      "expiresAtUtc": "2026-10-15T18:30:00Z",
      "viewCount": 15,
      "createdAtUtc": "2026-10-08T08:10:00Z"
    }
  ],
  "nextCursor": "eyJjcmVhdGVkIjoi..."
}

Do not return the complete content for a large snippet-list endpoint.
13. GET /api/v1/snippets/{snippetId}
Returns one owned snippet.
Response
{
  "id": "d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d",
  "title": "ASP.NET Notes",
  "content": "Entity Framework Core uses DbContext...",
  "expiresAtUtc": "2026-10-15T18:30:00Z",
  "maxViews": 100,
  "viewCount": 15,
  "lastViewedAtUtc": "2026-10-08T08:05:00Z",
  "createdAtUtc": "2026-10-08T08:10:00Z",
  "updatedAtUtc": "2026-10-08T08:15:00Z"
}

Authorization:
authenticated
AND
owner OR admin

14. PATCH /api/v1/snippets/{snippetId}
I prefer PATCH because most updates are partial.
Request
{
  "title": "Updated ASP.NET Notes",
  "content": "Updated content...",
  "expiresAtUtc": "2026-10-20T18:30:00Z",
  "maxViews": 200
}

Response
200 OK

Authorization
owner OR admin

15. Optimistic concurrency
Since we added:
version UUID

you can implement conditional updates.
For example:
If-Match: "7d0f8d..."

Then:
request version == database version
        ↓
update

otherwise
        ↓
409 Conflict

Response:
{
  "type": "https://api.textshare.dev/problems/concurrency-conflict",
  "title": "Resource was modified",
  "status": 409,
  "detail": "The snippet was changed by another request."
}

This is a very nice production feature to demonstrate.
16. DELETE /api/v1/snippets/{snippetId}
Deletes the snippet.
Because ShareLink has cascade delete:
Snippet deleted
     ↓
all share links deleted

Response
204 No Content

Authorization:
owner OR admin

17. Share-link API
A very important design choice:
Creating a share link is an operation on a snippet, so the canonical endpoint is:
POST /api/v1/snippets/{snippetId}/links

rather than:
POST /api/v1/share-links

because the relationship is part of the command.
18. POST /api/v1/snippets/{snippetId}/links
Create a share link.
Request
{
  "expiresAtUtc": "2026-10-10T18:30:00Z",
  "maxUses": 25,
  "password": null
}

Or password-protected:
{
  "expiresAtUtc": "2026-10-10T18:30:00Z",
  "maxUses": 25,
  "password": "MySecret123!"
}

Response — 201 Created
{
  "id": "38dc68e2-59dc-4dca-b0b1-c7f3e13f5c6c",
  "url": "https://textshare.example/s/K8xP2nQ7mL9vR4sT",
  "expiresAtUtc": "2026-10-10T18:30:00Z",
  "maxUses": 25,
  "useCount": 0,
  "isPasswordProtected": true,
  "createdAtUtc": "2026-10-08T08:20:00Z"
}

Important
The plaintext share code:
K8xP2nQ7mL9vR4sT

is returned only at creation time.
The database stores:
HMAC-SHA256(code)

not the code itself.
19. GET /api/v1/snippets/{snippetId}/links
Returns the owner's links.
{
  "items": [
    {
      "id": "38dc68e2-59dc-4dca-b0b1-c7f3e13f5c6c",
      "expiresAtUtc": "2026-10-10T18:30:00Z",
      "maxUses": 25,
      "useCount": 10,
      "isPasswordProtected": true,
      "isRevoked": false,
      "lastAccessedAtUtc": "2026-10-08T08:25:00Z",
      "createdAtUtc": "2026-10-08T08:20:00Z"
    }
  ],
  "nextCursor": null
}

Notice:
url

doesn't need to be returned again because you deliberately don't store the raw token.
That is a security advantage.
20. GET /api/v1/share-links/{linkId}
Returns metadata for a specific link.
{
  "id": "38dc68e2-59dc-4dca-b0b1-c7f3e13f5c6c",
  "snippetId": "d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d",
  "expiresAtUtc": "2026-10-10T18:30:00Z",
  "maxUses": 25,
  "useCount": 10,
  "isPasswordProtected": true,
  "isRevoked": false,
  "lastAccessedAtUtc": "2026-10-08T08:25:00Z"
}

Authorization:
owner OR admin

21. PATCH /api/v1/share-links/{linkId}
Update link policy.
Example:
{
  "expiresAtUtc": "2026-10-20T18:30:00Z",
  "maxUses": 100
}

I would not allow changing the actual token.
Changing the share credential should instead be:
revoke old link
+
create new link

This makes security semantics very clear.
22. DELETE /api/v1/share-links/{linkId}
You can treat deletion as revocation.
DELETE /api/v1/share-links/38dc68e2-59dc-4dca-b0b1-c7f3e13f5c6c

Response:
204 No Content

Internally:
RevokedAtUtc = now

rather than immediately deleting the row.
This is better for auditability.
23. Public share endpoint
This is the heart of TextShare.
GET /s/{code}
Example:
GET /s/K8xP2nQ7mL9vR4sT

No JWT required.
Successful response
{
  "id": "d7d8c2d9-a9d7-4325-99c8-48fdd8ec4f3d",
  "title": "ASP.NET Notes",
  "content": "Entity Framework Core uses DbContext...",
  "expiresAtUtc": "2026-10-10T18:30:00Z"
}

I would deliberately not return:
ownerUserId
viewCount
maxViews
internal link ID
token hash
database IDs

The public API should expose only what the consumer needs.
24. Password-protected public share
There is an important API question here.
A request to:
GET /s/K8xP2nQ7mL9vR4sT

might determine that the link is password protected.
A simple implementation could use:
GET /s/{code}

and return:
401 Unauthorized

with:
{
  "type": "https://api.textshare.dev/problems/password-required",
  "title": "Password required",
  "status": 401
}

Then:
POST /s/{code}/unlock

Request:
{
  "password": "MySecret123!"
}

Response:
{
  "accessToken": "short-lived-share-access-token"
}

Then:
GET /s/{code}
Authorization: Bearer <share-access-token>

This is the design I prefer for password-protected links.
25. Share-access token
Don't reuse your normal user JWT for a public share password.
Use a purpose-specific short-lived token:
ShareAccessToken
    ├── shareLinkId
    ├── purpose = "share-access"
    └── expiration = 5–15 minutes

That limits the blast radius if somebody obtains it.
The flow becomes:
GET /s/{code}
      ↓
password protected
      ↓
401 PasswordRequired

POST /s/{code}/unlock
      ↓
BCrypt.Verify()
      ↓
short-lived share access token
      ↓
GET /s/{code}
      ↓
content

26. POST /s/{code}/unlock
Request:
{
  "password": "MySecret123!"
}

Success:
{
  "accessToken": "eyJhbGciOi..."
  ,
  "expiresAtUtc": "2026-10-08T08:40:00Z"
}

Errors:
401 Unauthorized
404 Not Found
429 Too Many Requests

I would rate-limit this endpoint aggressively.
For example:
5 failed attempts / minute / IP + link

with a sensible distributed rate-limit strategy in production.
27. Share access logging
Every public access attempt should result in an audit record.
For example:
GET /s/K8xP2nQ7mL9vR4sT

creates:
{
  "shareLinkId": "...",
  "userId": null,
  "accessedAtUtc": "2026-10-08T08:40:00Z",
  "wasSuccessful": true,
  "ipAddress": "203.0.113.10",
  "userAgent": "Mozilla/5.0..."
}

Failed access:
{
  "wasSuccessful": false,
  "failureReason": "ExpiredLink"
}

28. Admin API
Administrative APIs should use:
Authorization: Bearer <admin-token>

and:
[Authorize(Policy = "AdminOnly")]

GET /api/v1/admin/users
Query:
pageSize
cursor
search
status
role

Example:
GET /api/v1/admin/users?role=user&status=active

Response:
{
  "items": [
    {
      "id": "...",
      "email": "user@example.com",
      "displayName": "User",
      "role": "user",
      "isActive": true,
      "createdAtUtc": "2026-09-01T10:00:00Z"
    }
  ],
  "nextCursor": null
}

Do not return passwordHash.
29. GET /api/v1/admin/users/{userId}
Returns administrative user information.
Could include:
{
  "id": "...",
  "email": "user@example.com",
  "displayName": "User",
  "role": "user",
  "isActive": true,
  "createdAtUtc": "...",
  "lastLoginAtUtc": "...",
  "snippetCount": 15,
  "activeRefreshTokenCount": 2
}

This is an example where aggregated information is useful without exposing sensitive storage details.
30. PATCH /api/v1/admin/users/{userId}/status
Request:
{
  "isActive": false
}

Behavior when deactivating:
User inactive
      ↓
revoke all refresh tokens
      ↓
future authentication denied

31. PATCH /api/v1/admin/users/{userId}/role
Request:
{
  "role": "admin"
}

Possible values:
user
admin

This should itself be audited.
32. GET /api/v1/admin/access-logs
Admin audit/analytics endpoint.
Example:
GET /api/v1/admin/access-logs?
    success=false&
    reason=InvalidPassword&
    from=2026-10-01T00:00:00Z&
    to=2026-10-08T00:00:00Z

Response:
{
  "items": [
    {
      "id": "...",
      "shareLinkId": "...",
      "userId": null,
      "accessedAtUtc": "2026-10-08T08:40:00Z",
      "wasSuccessful": false,
      "ipAddress": "203.0.113.10",
      "failureReason": "InvalidPassword"
    }
  ],
  "nextCursor": "..."
}

33. GET /api/v1/admin/refresh-tokens
This should be an operational/security endpoint rather than exposing raw token values.
Example:
{
  "items": [
    {
      "id": "...",
      "userId": "...",
      "familyId": "...",
      "createdAtUtc": "...",
      "expiresAtUtc": "...",
      "revokedAtUtc": null,
      "revocationReason": null,
      "createdByIp": "..."
    }
  ]
}

Never return:
token
tokenHash

to the administrator API.
34. Health endpoints
For deployment:
GET /health
Basic liveness check:
{
  "status": "Healthy"
}

GET /health/ready
Readiness should check dependencies:
Application
   │
   ├── PostgreSQL
   └── required infrastructure

Example result:
{
  "status": "Healthy",
  "checks": {
    "postgresql": "Healthy"
  }
}

Use:
/health
/health/ready

for container/orchestrator probes rather than exposing database diagnostic endpoints.
35. HTTP status code strategy
I would standardize these across the entire API.
Status	Meaning
200	Successful read/update
201	Resource created
204	Successful delete/action with no body
400	Malformed request
401	Not authenticated / invalid credentials
403	Authenticated but not authorized
404	Resource does not exist / deliberately hidden
409	Conflict / concurrency / duplicate business state
422	Semantic validation failure, if you choose to use it
429	Rate limit exceeded
500	Unexpected server error


Don't turn domain failures into 500.
For example:
snippet not owned
      → 403

snippet doesn't exist
      → 404

duplicate email
      → 409

invalid password
      → 401

36. Standard error format
Use RFC 9457 Problem Details rather than inventing different error JSON for each endpoint.
Example:
{
  "type": "https://api.textshare.dev/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/snippets",
  "traceId": "00-7d9c..."
}

For field validation:
{
  "type": "https://api.textshare.dev/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "content": [
      "Content is required.",
      "Content must be less than 1 MB."
    ],
    "expiresAtUtc": [
      "Expiration must be in the future."
    ]
  }
}

ASP.NET Core has built-in Problem Details support that fits this pattern.
37. DTO separation
Do not expose EF entities directly from controllers.
Use:
Application
│
├── Auth
│   ├── RegisterRequest
│   ├── LoginRequest
│   ├── RefreshRequest
│   └── AuthResponse
│
├── Users
│   ├── UserResponse
│   ├── UpdateProfileRequest
│   └── ChangePasswordRequest
│
├── Snippets
│   ├── CreateSnippetRequest
│   ├── UpdateSnippetRequest
│   ├── SnippetResponse
│   └── SnippetListItem
│
└── ShareLinks
    ├── CreateShareLinkRequest
    ├── UpdateShareLinkRequest
    └── ShareLinkResponse

So:
Controller
   ↓
DTO
   ↓
Application Service
   ↓
Domain
   ↓
EF Core

not:
Controller
   ↓
DbContext
   ↓
Entity exposed directly

38. Recommended controllers
I'd keep controllers like this:
Controllers/
│
├── AuthController.cs
├── UsersController.cs
├── SnippetsController.cs
├── ShareLinksController.cs
├── PublicShareController.cs
├── AdminUsersController.cs
├── AdminAuditController.cs
└── HealthController.cs

Or, if you adopt vertical slices, an even stronger portfolio architecture is:
Features/
├── Auth/
│   ├── Register/
│   ├── Login/
│   ├── Refresh/
│   └── Logout/
│
├── Snippets/
│   ├── Create/
│   ├── Get/
│   ├── List/
│   ├── Update/
│   └── Delete/
│
└── ShareLinks/
    ├── Create/
    ├── Revoke/
    └── Access/

For a student portfolio project, either is defensible; vertical slices become especially attractive if the codebase gets large.
39. End-to-end authorization matrix
This is the matrix I would implement.
Endpoint	Anonymous	User	Admin
POST /auth/register	✅	✅	✅
POST /auth/login	✅	✅	✅
POST /auth/refresh	✅	✅	✅
POST /auth/logout	✅	✅	✅
POST /auth/logout-all	❌	✅	✅
GET /users/me	❌	✅	✅
PATCH /users/me	❌	✅	✅
PATCH /users/me/password	❌	✅	✅
DELETE /users/me	❌	✅	✅
POST /snippets	❌	✅	✅
GET /snippets	❌	✅ own	✅ all if desired
GET /snippets/{id}	❌	✅ own	✅
PATCH /snippets/{id}	❌	✅ own	✅
DELETE /snippets/{id}	❌	✅ own	✅
POST /snippets/{id}/links	❌	✅ own	✅
GET /snippets/{id}/links	❌	✅ own	✅
GET /share-links/{id}	❌	✅ own	✅
PATCH /share-links/{id}	❌	✅ own	✅
DELETE /share-links/{id}	❌	✅ own	✅
GET /s/{code}	✅	✅	✅
POST /s/{code}/unlock	✅	✅	✅
/admin/*	❌	❌	✅


The critical distinction is:
GET /s/{code}

is public, but:
/api/v1/snippets/{id}

is owner-authorized.
Those should never be conflated.
40. Recommended request flow for a normal snippet
POST /api/v1/snippets
Authorization: Bearer JWT
        │
        ▼
JWT authentication
        │
        ▼
ClaimsPrincipal
        │
        ▼
Get sub → UserId
        │
        ▼
Validate request
        │
        ▼
Create TextSnippet
        │
        ▼
PostgreSQL
        │
        ▼
201 Created

41. Recommended request flow for accessing a share
GET /s/K8xP2nQ7mL9vR4sT
        │
        ▼
Hash/HMAC code
        │
        ▼
Find ShareLink
        │
        ├── not found ───────► 404
        │
        ▼
Check revoked
        │
        ├── revoked ─────────► 404
        │
        ▼
Check link expiration
        │
        ├── expired ─────────► 404
        │
        ▼
Check snippet expiration
        │
        ├── expired ─────────► 404
        │
        ▼
Check max uses/views
        │
        ├── exceeded ────────► 404
        │
        ▼
Password protected?
        │
        ├── yes ─────────────► 401
        │
        ▼
Atomic increment
        │
        ▼
Write access log
        │
        ▼
200 OK + content

42. Idempotency
For creation endpoints, especially:
POST /api/v1/snippets
POST /api/v1/snippets/{id}/links

I would support an optional:
Idempotency-Key: 3e7b8c5f-...

This becomes useful if a client times out after the server successfully creates the resource.
Without idempotency:
Client
  POST
    ↓
Server creates snippet
    ↓
Network timeout
    ↓
Client retries POST
    ↓
duplicate snippet

With idempotency:
Client → same Idempotency-Key
              ↓
Server detects previous request
              ↓
returns original result

This is not mandatory for v1, but it is a good production-grade extension.
43. Rate limiting
At minimum, rate-limit:
POST /auth/login
POST /auth/register
POST /auth/refresh
POST /s/{code}/unlock
GET  /s/{code}

Especially:
/s/{code}/unlock

because it can become a password-guessing endpoint.
For example, conceptually:
Authentication
    10 requests/min/IP

Share password unlock
    5 failed attempts/min/link+IP

Public share
    higher limit

The actual values should be tuned based on deployment and threat model rather than treated as universal security constants.
44. API versioning
I'd start with:
/api/v1

rather than:
/api

because your project demonstrates API lifecycle awareness.
So:
/api/v1/snippets

not:
/api/snippets

Then later:
/api/v2/snippets

can introduce breaking changes without silently changing existing clients.
The public share URL is different:
/s/{code}

because it is intended to be short and permanent-looking.
45. Swagger/OpenAPI grouping
Your Swagger UI should ideally show:
Authentication
   Register
   Login
   Refresh
   Logout
   Logout All

Users
   Get Me
   Update Me
   Change Password
   Delete Me

Snippets
   Create
   List
   Get
   Update
   Delete

Share Links
   Create
   List
   Get
   Update
   Revoke

Public Sharing
   Get Shared Text
   Unlock Shared Text

Administration
   Users
   User Status
   User Roles
   Access Logs
   Refresh Tokens

Health
   Liveness
   Readiness

And configure Swagger's bearer authentication:
Authorize
Bearer eyJ...

so you can test protected endpoints directly from Swagger.
46. Final API architecture
The resulting project architecture becomes:
                         ┌────────────────────┐
                         │      Client        │
                         └─────────┬──────────┘
                                   │
                 ┌─────────────────┴──────────────────┐
                 │                                    │
          /api/v1/*                              /s/{code}
                 │                                    │
                 ▼                                    ▼
        ┌──────────────────┐                ┌──────────────────┐
        │ Authentication   │                │ Public Sharing   │
        │ + Authorization  │                │ + Access Control │
        └────────┬─────────┘                └────────┬─────────┘
                 │                                   │
                 └────────────────┬──────────────────┘
                                  ▼
                      ┌──────────────────────┐
                      │ Application Layer    │
                      │                      │
                      │ Auth                 │
                      │ Snippets             │
                      │ Share Links          │
                      │ Users                │
                      │ Audit                │
                      └──────────┬───────────┘
                                 ▼
                      ┌──────────────────────┐
                      │ Domain               │
                      │                      │
                      │ User                 │
                      │ TextSnippet          │
                      │ ShareLink            │
                      │ RefreshToken         │
                      │ ShareAccessLog       │
                      └──────────┬───────────┘
                                 ▼
                      ┌──────────────────────┐
                      │ Infrastructure       │
                      │                      │
                      │ EF Core              │
                      │ PostgreSQL           │
                      │ BCrypt               │
                      │ JWT                  │
                      │ Background Cleanup   │
                      └──────────────────────┘

The endpoint set I would actually implement for v1
AUTH
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout
POST   /api/v1/auth/logout-all

USER
GET    /api/v1/users/me
PATCH  /api/v1/users/me
PATCH  /api/v1/users/me/password
DELETE /api/v1/users/me

SNIPPETS
POST   /api/v1/snippets
GET    /api/v1/snippets
GET    /api/v1/snippets/{snippetId}
PATCH  /api/v1/snippets/{snippetId}
DELETE /api/v1/snippets/{snippetId}

SHARE LINKS
POST   /api/v1/snippets/{snippetId}/links
GET    /api/v1/snippets/{snippetId}/links
GET    /api/v1/share-links/{linkId}
PATCH  /api/v1/share-links/{linkId}
DELETE /api/v1/share-links/{linkId}

PUBLIC
GET    /s/{code}
POST   /s/{code}/unlock

ADMIN
GET    /api/v1/admin/users
GET    /api/v1/admin/users/{userId}
PATCH  /api/v1/admin/users/{userId}/status
PATCH  /api/v1/admin/users/{userId}/role
GET    /api/v1/admin/access-logs
GET    /api/v1/admin/refresh-tokens

OPS
GET    /health
GET    /health/ready
