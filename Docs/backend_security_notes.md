# Backend Security Notes

## Password Hashing Strategy

The backend now uses adaptive PBKDF2 hashing for user passwords.

### Algorithm
- Scheme: PBKDF2-HMAC-SHA256
- Iterations: 120000
- Salt size: 16 bytes (random per password)
- Derived key size: 32 bytes
- Storage format: `pbkdf2$<iterations>$<saltBase64>$<hashBase64>`

Implementation reference:
- `backend/Database_Backend/Database_Backend/Services/PasswordHashService.cs`

### Backward Compatibility and Migration
Legacy stored values (plain or SHA256 forms) are still accepted during login for compatibility.
On successful login with a legacy value:
1. Password is verified against legacy format.
2. Stored value is immediately upgraded to PBKDF2 format.
3. Subsequent logins use PBKDF2 verification.

This provides non-breaking migration without requiring a global password reset.

### Secure Handling Notes
- Passwords are never returned via API DTOs.
- Verification uses constant-time comparison for PBKDF2 hashes.
- Unique per-password salts protect against rainbow-table reuse.
- Iteration count is encoded in hash payload, enabling future iteration upgrades.

### Future Hardening Options
- Introduce a server-side pepper stored in secret management.
- Add account lockout/rate limiting for repeated login failures.
- Enforce password complexity and rotation policies where required.
