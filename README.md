# Sermon Transcription API

A robust, multi-tenant .NET 8 API for managing sermon transcriptions with comprehensive user and organization management capabilities.

## 🚀 Features

- **Multi-tenant Architecture**: JWT-based authentication with organization context via headers
- **User Management**: Complete user registration, authentication, and profile management
- **Organization Management**: Multi-organization support with role-based access control
- **Sermon Transcription**: Core functionality for sermon transcription management
- **Comprehensive Testing**: 251 tests (183 unit + 68 integration) with 100% pass rate
- **Modern .NET 8**: Built with the latest .NET 8 features and best practices
- **Clean Architecture**: Domain-driven design with clear separation of concerns

## 🏗️ Architecture

The project follows Clean Architecture principles with the following layers:

```
SermonTranscription/
├── Api/                    # Presentation layer (Controllers, Middleware)
├── Application/           # Application services and DTOs
├── Domain/               # Domain entities and business logic
├── Infrastructure/       # Data access and external services
└── Tests/               # Unit and integration tests
```

### Key Components

- **JWT Multi-tenant Authentication**: User context in JWT tokens, organization context via `X-Organization-ID` header
- **Entity Framework Core**: PostgreSQL database with code-first migrations
- **AutoMapper**: Entity-to-DTO mapping
- **FluentValidation**: Request validation
- **Swagger/OpenAPI**: API documentation

## 🛠️ Technology Stack

- **.NET 8**: Latest .NET framework
- **Entity Framework Core**: ORM for data access
- **PostgreSQL**: Primary database
- **JWT**: Authentication and authorization
- **AutoMapper**: Object mapping
- **FluentValidation**: Input validation
- **xUnit**: Unit testing framework
- **Docker**: Containerization support

## 📋 Prerequisites

- .NET 8 SDK
- PostgreSQL
- Docker (optional, for containerized deployment)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd net-api
```

### 2. Configuration

This project uses a **hybrid approach** for managing configuration:

- **User Secrets** for sensitive data (passwords, API keys, connection strings)
- **appsettings.Local.json** for non-sensitive development overrides

#### Setup User Secrets (Sensitive Data)

```bash
# Navigate to the API project
cd SermonTranscription.Api

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Add sensitive configuration
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-jwt-key-32-characters-minimum"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SermonTranscriptionDb_Dev;Username=postgres;Password=your_password;Include Error Detail=true"
```

#### Setup Local Settings (Non-Sensitive Overrides)

Copy the example configuration files:

```bash
cp appsettings.Local.json.example SermonTranscription.Api/appsettings.Local.json
cp docker.env.example docker.env
```

Update `appsettings.Local.json` with non-sensitive development overrides:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SermonTranscription": "Debug"
    }
  },
  "JwtSettings": {
    "ExpirationInMinutes": 120
  },
  "Features": {
    "EnableDetailedErrors": true,
    "EnableSwagger": true
  }
}
```

#### Configuration Loading Order

.NET loads configuration in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **`appsettings.Local.json`** ← Non-sensitive dev overrides
4. **User Secrets** ← Sensitive data (Development only)
5. Environment Variables
6. Command-line arguments

### 3. Database Setup

Run the Entity Framework migrations:

```bash
cd SermonTranscription.Api
dotnet ef database update
```

### 4. Run the Application

#### Development Mode

```bash
dotnet run --project SermonTranscription.Api
```

#### Docker Mode

```bash
docker-compose up -d
```

The API will be available at:

- **Local**: http://localhost:5020
- **Swagger UI**: http://localhost:5020/swagger

## 🔐 Authentication

The API uses JWT-based authentication with multi-tenant support:

### JWT Token Structure

```json
{
  "aud": "SermonTranscriptionClients",
  "iss": "SermonTranscriptionApi",
  "exp": 1753246377,
  "nameidentifier": "user-id",
  "emailaddress": "user@example.com",
  "name": "User Name",
  "userId": "user-guid"
}
```

### Organization Context

Organization context is provided via the `X-Organization-ID` header:

```
X-Organization-ID: 00000000-0000-0000-0000-000000000001
```

### Endpoint Types

1. **Organization-Agnostic Endpoints**: User profile management

   - `GET /api/v1/users/profile`
   - `PUT /api/v1/users/profile`
   - `POST /api/v1/users/change-password`

2. **Organization-Specific Endpoints**: Organization management
   - `GET /api/v1/organizations`
   - `POST /api/v1/organizations`
   - `GET /api/v1/users/organizations/users`

## 📚 API Endpoints

### Authentication

- `POST /api/v1/auth/register` - User registration
- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/refresh` - Refresh JWT token
- `POST /api/v1/auth/logout` - User logout
- `POST /api/v1/auth/forgot-password` - Password reset request
- `POST /api/v1/auth/reset-password` - Password reset

### User Management

- `GET /api/v1/users/profile` - Get user profile
- `PUT /api/v1/users/profile` - Update user profile
- `POST /api/v1/users/change-password` - Change password
- `GET /api/v1/users/organizations/users` - List organization users
- `GET /api/v1/users/organizations/users/{userId}` - Get organization user
- `PUT /api/v1/users/organizations/users/{userId}/role` - Update user role
- `DELETE /api/v1/users/organizations/users/{userId}` - Remove user from organization
- `POST /api/v1/users/organizations/users/{userId}/activate` - Activate user
- `POST /api/v1/users/organizations/users/{userId}/deactivate` - Deactivate user

### Organization Management

- `GET /api/v1/organizations` - List organizations
- `POST /api/v1/organizations` - Create organization
- `GET /api/v1/organizations/{id}` - Get organization
- `PUT /api/v1/organizations/{id}` - Update organization
- `PUT /api/v1/organizations/{id}/settings` - Update organization settings
- `PUT /api/v1/organizations/{id}/logo` - Update organization logo
- `POST /api/v1/organizations/{id}/activate` - Activate organization
- `POST /api/v1/organizations/{id}/deactivate` - Deactivate organization

### User Invitations

- `POST /api/v1/organizations/{id}/invite` - Invite user to organization
- `POST /api/v1/auth/accept-invitation` - Accept invitation

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Run Unit Tests Only

```bash
dotnet test SermonTranscription.Tests.Unit
```

### Run Integration Tests Only

```bash
dotnet test SermonTranscription.Tests.Integration
```

### Test Coverage

- **Unit Tests**: 183 tests covering services, entities, and middleware
- **Integration Tests**: 68 tests covering API endpoints
- **Total**: 251 tests with 100% pass rate

### Test Categories

- **AuthService Tests**: Authentication, registration, login, password reset
- **InvitationService Tests**: User invitations and acceptance
- **JwtService Tests**: JWT token generation and validation
- **PasswordHasher Tests**: Password hashing and verification
- **Entity Tests**: Domain entity behavior and validation
- **Controller Tests**: API endpoint integration tests

## 🐳 Docker Deployment

### Development

```bash
docker-compose up -d
```

### Production

```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Environment Variables

In production, use environment variables for sensitive data:

```bash
# Example environment variables for production
export ConnectionStrings__DefaultConnection="Host=prod-server;Database=SermonTranscriptionDb;Username=appuser;Password=secure_password"
export JwtSettings__SecretKey="production-jwt-secret-key-32-characters-minimum"
export ExternalServices__Gladia__ApiKey="your-production-gladia-api-key"
```

#### Docker Example

```dockerfile
ENV ConnectionStrings__DefaultConnection="Host=postgres;Database=SermonTranscriptionDb;Username=appuser;Password=${DB_PASSWORD}"
ENV JwtSettings__SecretKey="${JWT_SECRET_KEY}"
```

#### Required Environment Variables

- `ConnectionStrings__DefaultConnection`: Database connection string
- `JwtSettings__SecretKey`: JWT signing key (32+ characters)
- `JwtSettings__Issuer`: JWT issuer
- `EmailSettings__SmtpServer`: SMTP server for emails
- `EmailSettings__SmtpPort`: SMTP port
- `EmailSettings__Username`: SMTP username
- `EmailSettings__Password`: SMTP password

## 🔧 Development

### Project Structure

```
SermonTranscription/
├── Api/                    # Web API layer
│   ├── Controllers/       # API controllers
│   ├── Middleware/        # Custom middleware
│   ├── Authorization/     # Authorization attributes
│   └── Swagger/          # API documentation
├── Application/           # Application layer
│   ├── Services/         # Application services
│   ├── DTOs/            # Data transfer objects
│   ├── Interfaces/      # Service interfaces
│   └── Mapping/         # AutoMapper profiles
├── Domain/               # Domain layer
│   ├── Entities/        # Domain entities
│   ├── Interfaces/      # Repository interfaces
│   ├── Enums/          # Domain enums
│   └── Exceptions/     # Domain exceptions
├── Infrastructure/       # Infrastructure layer
│   ├── Data/           # Entity Framework context
│   ├── Repositories/   # Repository implementations
│   ├── Services/       # External service implementations
│   └── Migrations/     # Database migrations
└── Tests/               # Test projects
    ├── Unit/           # Unit tests
    └── Integration/    # Integration tests
```

### Key Patterns

#### Multi-tenant Architecture

- JWT tokens contain user information only
- Organization context provided via `X-Organization-ID` header
- Middleware pipeline handles tenant resolution
- Repository pattern with tenant-aware queries

#### Authorization

- Custom authorization attributes for organization membership
- Role-based access control (Admin, Manager, Member)
- Organization-agnostic endpoints for user profile management

#### Testing

- Base test classes for common setup
- In-memory database for integration tests
- Comprehensive test data factories
- Direct database verification using `AsNoTracking()`

## 🔐 Secret Management

### Managing User Secrets

```bash
# List all secrets
dotnet user-secrets list

# Set a secret
dotnet user-secrets set "SectionName:KeyName" "SecretValue"

# Remove a secret
dotnet user-secrets remove "SectionName:KeyName"

# Clear all secrets
dotnet user-secrets clear
```

### What Goes Where

#### ✅ Use User Secrets For:

- Database passwords
- API keys and tokens
- JWT signing keys
- OAuth client secrets
- Third-party service credentials

#### ✅ Use appsettings.Local.json For:

- Logging levels
- Development URLs
- Feature toggles
- Timeout values
- Non-sensitive overrides

#### ❌ Never Put in Git:

- Passwords or API keys
- Connection strings with credentials
- JWT secrets
- Any sensitive configuration

### Verification

#### Check Current Configuration

```bash
# View merged configuration (without secrets displayed)
dotnet run --project SermonTranscription.Api --environment Development

# Check user secrets
dotnet user-secrets list --project SermonTranscription.Api
```

#### Test Database Connection

```bash
# This will show if the connection string is working
dotnet ef database update --project SermonTranscription.Infrastructure --startup-project SermonTranscription.Api
```

## 📝 API Documentation

Interactive API documentation is available via Swagger UI:

- **Development**: http://localhost:5020/swagger
- **Production**: https://your-domain.com/swagger

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow Clean Architecture principles
- Write comprehensive tests for new features
- Use meaningful commit messages
- Update documentation for API changes

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:

- Create an issue in the repository
- Check the API documentation at `/swagger`
- Review the test examples for usage patterns

## 🔄 Version History

- **v1.0.0**: Initial release with multi-tenant architecture
- **v1.1.0**: Added comprehensive integration tests
- **v1.2.0**: Fixed critical bugs and improved error handling

---

**Built with ❤️ using .NET 8 and Clean Architecture principles**
