# 3rd Semester Exam, Computer Science AP
## Client: Business Esbjerg

## Goal/Problem/Description:
* TBA

## Requirements:
* .NET 10 SDK
* Rest TBA

## Running backend locally

1. You'll need to set a JWT secret in server/API (Any random string of at least 32 chars). 
Use this command: `dotnet user-secrets set "Jwt:Secret" "your-secret-here"`


2. On the first application startup, the backend automatically creates an initial administrator account if it does not already exist. 
Use these commands: `dotnet user-secrets set "Admin:Username" "your-admin-username"` `dotnet user-secrets set "Admin:Password" "your-admin-password"`


3. Start the API. Use this command: `dotnet run`

The SQLite database (`db.db`) is created automatically in `server/API` on first run.
The API runs on localhost:5194

## Running with Docker

(The Dockerfile expects `Jwt__Secret`, `Admin_Username`, and `Admin__Password` as an environment variables.)

1. Build an image of the backend. 
Use this command: `docker build -t business-esb-backend .`

2. Run the image - remember your JWT secret, admin credentials, and CORS settings. 
Use this command: `docker run --rm --name business-esb-backend -p 127.0.0.1:4000:4000 \
  -e Jwt__Secret="your-secret-here" \
  -e Admin__Username="your-admin-username" \
  -e Admin__Password="your-admin-password" \
  -e Cors__Origins__0="https://your-client-domain.com" \
  business-esb-backend`
