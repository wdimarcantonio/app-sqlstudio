# Security Notes for PR #7

## ⚠️ Known Security Issues

The following security issues were identified during code review and should be addressed in a future PR:

### 1. Plain Text Password Storage (HIGH PRIORITY)
**Issue**: Database connection passwords are currently stored in plain text in the SQLite database.

**Affected Files**:
- `SqlExcelBlazor.Server/Models/Connections/SqlServerConnection.cs`
- `SqlExcelBlazor.Server/Models/Connections/PostgreSqlConnection.cs`
- `SqlExcelBlazor.Server/Models/Connections/MySqlConnection.cs`
- `SqlExcelBlazor.Server/Models/Connections/WebServiceConnection.cs`

**Risk**: If the database is compromised, all stored credentials would be exposed.

**Recommended Solution**:
- Implement encryption for sensitive fields using ASP.NET Core Data Protection API
- Alternative: Use Azure Key Vault or similar credential management system
- Alternative: Use Windows Credential Manager or platform-specific secure storage

**Example Implementation**:
```csharp
public class SecureConnectionRepository
{
    private readonly IDataProtector _protector;
    
    public SecureConnectionRepository(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ConnectionPasswords");
    }
    
    public void EncryptPassword(Connection connection)
    {
        if (connection is SqlServerConnection sqlConn && !string.IsNullOrEmpty(sqlConn.Password))
        {
            sqlConn.Password = _protector.Protect(sqlConn.Password);
        }
        // ... similar for other connection types
    }
    
    public void DecryptPassword(Connection connection)
    {
        if (connection is SqlServerConnection sqlConn && !string.IsNullOrEmpty(sqlConn.Password))
        {
            sqlConn.Password = _protector.Unprotect(sqlConn.Password);
        }
        // ... similar for other connection types
    }
}
```

### 2. API Keys and Tokens in Plain Text (HIGH PRIORITY)
**Issue**: WebService authentication credentials (API keys, bearer tokens, basic auth) are stored in plain text in the `AuthConfigJson` field.

**Affected Files**:
- `SqlExcelBlazor.Server/Models/Connections/WebServiceConnection.cs`

**Risk**: Exposes third-party API credentials if database is compromised.

**Recommended Solution**: Same as password storage - implement encryption for `AuthConfig` dictionary values.

## 🔐 Recommended Action Plan

### Phase 1 (Next PR)
1. Add NuGet package: `Microsoft.AspNetCore.DataProtection`
2. Configure Data Protection in `Program.cs`
3. Create `ICredentialEncryptor` service
4. Update Repository to encrypt/decrypt on save/load
5. Add migration to re-encrypt existing credentials

### Phase 2 (Future Enhancement)
1. Evaluate external credential stores (Azure Key Vault, AWS Secrets Manager)
2. Implement credential rotation policies
3. Add audit logging for credential access
4. Implement connection string templates (separate credentials from connection info)

## 📋 Other Recommendations

### Medium Priority
- Add input validation for port numbers to prevent negative values
- Implement rate limiting on API endpoints
- Add connection timeout limits to prevent resource exhaustion
- Validate all user input before processing

### Low Priority
- Consider implementing connection string validation
- Add unit tests for authentication methods
- Document security best practices for deployment

## 🚫 What NOT to Do

- **DO NOT** commit credentials to source control
- **DO NOT** log passwords or sensitive credentials
- **DO NOT** expose credentials in error messages
- **DO NOT** transmit passwords over unencrypted connections in production

## 📚 References

- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [Secure Storage of App Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [OWASP Secure Coding Practices](https://owasp.org/www-project-secure-coding-practices-quick-reference-guide/)

---

**Note**: These issues are documented for transparency. The current implementation is suitable for development and demonstration purposes, but **MUST** be addressed before production deployment.

Last Updated: 2026-01-30  
Severity: HIGH  
Priority: Next PR
