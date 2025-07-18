# Secret Management Setup

This project uses a **hybrid approach** for managing configuration:

- **User Secrets** for sensitive data (passwords, API keys, connection strings)
- **appsettings.Local.json** for non-sensitive development overrides

## 🔐 User Secrets (Sensitive Data)

### Setup User Secrets

```bash
# Navigate to the API project
cd SermonTranscription.Api

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Add sensitive configuration
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-jwt-key-32-characters-minimum"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SermonTranscriptionDb_Dev;Username=postgres;Password=your_password;Include Error Detail=true"
```

### What Goes in User Secrets

Use user secrets for:

- Database connection strings (contain passwords)
- JWT signing keys
- API keys (Gladia, email services, etc.)
- OAuth client secrets
- Redis connection strings (if they contain passwords)
- Any other sensitive credentials

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

## 🛠️ Local Settings (Non-Sensitive Overrides)

### appsettings.Local.json

This file is ignored by git and perfect for development overrides that aren't sensitive:

- Logging levels
- Development URLs and ports
- Feature flags
- Timeout values
- Non-sensitive service configurations
- Additional CORS origins

### Example Local Settings

The `appsettings.Local.json` file can override any non-sensitive settings:

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

## 📋 Configuration Loading Order

.NET loads configuration in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **`appsettings.Local.json`** ← Non-sensitive dev overrides
4. **User Secrets** ← Sensitive data (Development only)
5. Environment Variables
6. Command-line arguments

## 🚀 Production Setup

### Environment Variables

In production, use environment variables for sensitive data:

```bash
# Example environment variables for production
export ConnectionStrings__DefaultConnection="Host=prod-server;Database=SermonTranscriptionDb;Username=appuser;Password=secure_password"
export JwtSettings__SecretKey="production-jwt-secret-key-32-characters-minimum"
export ExternalServices__Gladia__ApiKey="your-production-gladia-api-key"
```

### Docker Example

```dockerfile
ENV ConnectionStrings__DefaultConnection="Host=postgres;Database=SermonTranscriptionDb;Username=appuser;Password=${DB_PASSWORD}"
ENV JwtSettings__SecretKey="${JWT_SECRET_KEY}"
```

## 🔍 Verification

### Check Current Configuration

```bash
# View merged configuration (without secrets displayed)
dotnet run --project SermonTranscription.Api --environment Development

# Check user secrets
dotnet user-secrets list --project SermonTranscription.Api
```

### Test Database Connection

```bash
# This will show if the connection string is working
dotnet ef database update --project SermonTranscription.Infrastructure --startup-project SermonTranscription.Api
```

## 📝 Best Practices

### ✅ Do Use User Secrets For:

- Database passwords
- API keys and tokens
- JWT signing keys
- OAuth client secrets
- Third-party service credentials

### ✅ Do Use appsettings.Local.json For:

- Logging levels
- Development URLs
- Feature toggles
- Timeout values
- Non-sensitive overrides

### ❌ Never Put in Git:

- Passwords or API keys
- Connection strings with credentials
- JWT secrets
- Any sensitive configuration

### 🔄 Team Sharing:

- **Sensitive data**: Each developer manages their own user secrets
- **Non-sensitive config**: Share via `appsettings.Local.json.example` file
- **Base config**: Commit `appsettings.json` with safe defaults

## 🚨 Security Notes

1. **User secrets are stored outside the project directory** in your user profile
2. **appsettings.Local.json is git-ignored** but still stored in the project folder
3. **Never commit sensitive data** even in ignored files
4. **Use environment variables in production** rather than files
5. **Rotate secrets regularly** especially for production environments

## 📞 Troubleshooting

### User Secrets Not Working?

```bash
# Check if user secrets are initialized
dotnet user-secrets list --project SermonTranscription.Api

# Re-initialize if needed
dotnet user-secrets init --project SermonTranscription.Api
```

### Configuration Not Loading?

1. Check file naming: `appsettings.Local.json` (case-sensitive)
2. Verify JSON syntax is valid
3. Ensure file is in the same directory as `appsettings.json`
4. Check that the file is not corrupted or empty
