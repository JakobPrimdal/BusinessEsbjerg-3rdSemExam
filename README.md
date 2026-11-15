# 3rd Semester Exam, Computer Science AP
## Client: Business Esbjerg

## Goal/Problem/Description:
* TBA

## Requirements:
* .NET 10 SDK
* Rest TBA


## Project Architecture

### Backend Architecture
The backend follows a layered architecture divided into three main layers: API, Service, and Infrastructure (Infra). 
Each layer has a distinct responsibility, helping separate concerns and keep the codebase maintainable and testable.

#### Layers and Responsibilities 
1. API Layer 
The API layer is the entry point to the application. It handles incoming HTTP requests and communicates with the Service layer. 
Responsibilities: 
Exposes REST endpoints through controllers. Handles HTTP-specific concerns such as routing, authentication and authorization. 
Configures dependency injection and application services in Program.cs. Handles exceptions globally and returns appropriate HTTP responses. 
The API layer should contain as little business logic as possible. Its primary responsibility is to handle communication between the client and the application. 


2. Service Layer 
The Service layer contains the application's business logic and rules. It processes requests received from the API layer and coordinates the operations required to fulfil them. 
Responsibilities: 
Implements business logic and validation. Handles authentication and user-related operations. Manages password hashing and JWT token generation. 
Converts database entities into DTOs before returning data to the API layer. 
Defines service contracts and other interfaces. For example, AuthService handles registration, login and retrieving users without needing to know how HTTP requests or database connections work. 


3. Infrastructure Layer (Infra) 
The Infrastructure layer handles communication with external resources, primarily the database. 
Responsibilities: 
Defines database entities and their mappings. Manages database connections through LinqToDB. Implements database operations such as creating, retrieving and inserting (CRUD) users. 
Handles database initialization and role seeding. For example, LoginDb implements the database operations required by the authentication functionality.

#### Loose Coupling through Interfaces
To reduce coupling between components and layers, the backend uses interfaces to define contracts between different parts of the application. 
Instead of depending directly on concrete implementations, services depend on interfaces describing the functionality they require. 

For example, AuthService depends on ILoginDb rather than directly depending on LoginDb.
The ILoginDb interface defines the database operations available to the Service layer, while LoginDb provides the actual implementation using LinqToDB. 
This separation means that the business logic does not need to know how the database operations are implemented.

#### Benefits of using interfaces:
* Reduces coupling between components. 
* Makes implementations easier to replace without changing the consuming service. 
* Improves testability by allowing dependencies to be mocked or substituted. 
* Makes responsibilities and dependencies more explicit.

#### Dependency Injection
* TBA

### Frontend Architecture


## Branching strategies

* TBA


## Running backend locally

1. You'll need to set a JWT secret in server/API (Any random string of at least 32 chars). 
Use this command: `dotnet user-secrets set "Jwt:Secret" "your-secret-here"`


2. On the first application startup, the backend automatically creates an initial administrator account if it does not already exist. 
Use these commands: `dotnet user-secrets set "Admin:Username" "your-admin-username"` `dotnet user-secrets set "Admin:Password" "your-admin-password"`


3. Start the API. Use this command: `dotnet run`

The SQLite database (`db.db`) is created automatically in `server/API` on first run.
The API runs on localhost:5194

### Running with Docker

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

### Techniques
Tests follow the method_scenario_expectedResult naming convention, so a failing test name explains what broke. Data-driven tests ([Theory] with [InlineData] or [MemberData]) cover many input combinations without duplicating test code. 
TestPasswordHasher uses the real Argon2 implementation rather than a mock, because the point of those tests is the cryptographic behavior. 
Since each hash is deliberately slow, that class is kept small. 

On the client side, tests are written with Bun’s built-in test runner (bun:test).

#### Unit tests
The backend unit tests are in the UnitTests project and use xUnit as the test framework and NSubstitute for mocking.
Each test class targets one production class:

#### Integration tests
The integration tests are in the IntegrationTests project. They use xUnit together with Microsoft.AspNetCore.Mvc.Testing.


## Continuous Integration

A GitHub Actions workflow (pipeline-tests.yml) runs on every push and pull request to main.

It has two independent jobs:
* Frontend tests: installs dependencies with a frozen lockfile (bun install --frozen-lockfile) and runs bun test. 
* Backend tests: restores, builds and runs dotnet test for the whole solution, which executes both the unit and integration tests.

Because the pipeline runs on pull requests, a change that breaks an existing test is detected before it is merged.


## Continuous Deployment

* TBA


## AI Disclosure

* TBA