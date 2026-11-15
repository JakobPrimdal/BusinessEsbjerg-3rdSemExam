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


## Testing

Testing in this project is separated into responsibilities, and type of tests - `/UnitTests`, `/IntegrationTests`, `/E2ETests`.

We have had the goal of catching most defects at the cheapest level. Business rules such as validation and error handling are tested as unit tests. 
Concerns that only exist when the pieces are connected (routing, authentication, authorization, cookies, database access and serialization) are tested as integration tests. 
Browser-based flows are reserved for Playwright.

All tests are run automatically by a CI pipeline (see 'Continuous Integration' below).

### What is tested
Beyond the happy path, the tests deliberately cover: 
* Boundary values: usernames and passwords exactly at, and one step outside, the minimum and maximum lengths. 
* Invalid input: whitespace-only usernames, unknown or numeric-but-undefined roles, and malformed password hashes. 
* Security properties: the same error message is returned for an unknown user and a wrong password, so attackers cannot discover which usernames exist. 
* Inactive users cannot log in even with the correct password. The plain-text password is never stored, and the same password gives different hashes because of the random salt. 
* Error handling: each custom exception type is mapped to the correct status code, and unexpected exceptions return a generic message, so internal details are not leaked.

#### Techniques
Tests follow the method_scenario_expectedResult naming convention, so a failing test name explains what broke. Data-driven tests ([Theory] with [InlineData] or [MemberData]) cover many input combinations without duplicating test code. 
TestPasswordHasher uses the real Argon2 implementation rather than a mock, because the point of those tests is the cryptographic behavior. 
Since each hash is deliberately slow, that class is kept small. 

On the client side, tests are written with Bun’s built-in test runner (bun:test).

#### Unit tests
The backend unit tests are in the UnitTests project and use xUnit as the test framework and NSubstitute for mocking.
Each test class targets one production class:

#### Integration tests
The integration tests are in the IntegrationTests project. They use xUnit together with Microsoft.AspNetCore.Mvc.Testing.
