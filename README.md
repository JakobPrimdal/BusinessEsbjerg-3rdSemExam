# 3rd Semester Exam, Computer Science AP
## Client: Business Esbjerg

## Goal/Problem/Description:
* TBA

## Requirements:
* .NET 10 SDK
* Rest TBA

## Security: Auth

### How login works

1. The client sends `POST /Login` with username and password.
2. The server verifies the password (hashed) and creates a signed JWT token containing the user id, username and role. - Which expires after 2 hours.
3. The server sends the JWT back in a `Set-Cookie` header named `jwt`. The response body only contains the user info, **never the token**.
4. The browser automatically sends the cookie with every later request. The server reads and validates it (`JwtBearer` in `Program.cs`).
5. `POST /Logout` makes the server delete the cookie. This must be done server-side, since JavaScript cannot touch an HttpOnly cookie.
6. On page load the client calls `GET /GetMe` to find out whether the user is logged in.

### HttpOnly, Secure, and SameSite

| Measure           | Benefit                                                                              |
|-------------------|--------------------------------------------------------------------------------------|
| `HttpOnly`        | JavaScript cannot read the cookie, so an XSS attack cannot steal the token.          |
| `Secure`          | The cookie is only sent over HTTPS, so it cannot be intercepted on the network.      |
| `SameSite=Strict` | The cookie is only sent for requests from our own site, which protects against CSRF. |


### Additional measures

- **CORS** only allows the origins listed in `Cors:Origins`, with credentials enabled. The client must send requests with `credentials: "include"`.
- **Passwords** are hashed, never stored in plain text.
- **Role-based authorization** is enforced on the server with `[Authorize(Roles = "...")]`. The client-side route guards are only for user experience.


## Running backend locally

1. You'll need to set a JWT secret in server/API (Any random string of at least 32 chars). 
Use this command:
```bash
dotnet user-secrets set "Jwt:Secret" "your-secret-here"
```


2. On the first application startup, the backend automatically creates an initial administrator account if it does not already exist. 
Use these commands:
```bash
dotnet user-secrets set "Admin:Username" "your-admin-username"` `dotnet user-secrets set "Admin:Password" "your-admin-password"
```

3. Start the API. Use this command: `dotnet run`

The SQLite database (`db.db`) is created automatically in `server/API` on first run.
The API runs on localhost:5194

### Running the backend with Docker

(The Dockerfile expects `Jwt__Secret`, `Admin_Username`, `Admin__Password`, and `Cors__Origins__0` as an environment variables.)

1. Build an image of the backend. 
Use this command:
```bash
docker build -t business-esb-backend .
```

2. Run the image - remember your JWT secret, admin credentials, and CORS settings. 
Use this command: 
```bash
docker run --rm --name business-esb-backend -p 127.0.0.1:4000:4000 \
  -e Jwt__Secret="your-secret-here" \
  -e Admin__Username="your-admin-username" \
  -e Admin__Password="your-admin-password" \
  -e Cors__Origins__0="https://your-client-domain.com" \
  business-esb-backend
```


## Running frontend locally

0. To the frontend of this application in production you will need to create a `client/.env.production` file to contain you backend api call.
Use this: `BUN_PUBLIC_API_URL=your-backend-api`

1. Install dependencies:

```bash
bun install
```

2. Start a development server:

```bash
bun run dev
```

3. Start a production server:

```bash
bun run start
```
