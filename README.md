# Invoice Automation System - Module 1 Complete

## Overview
This is the implementation of **Module 1: User Registration & Authentication** for the Invoice Automation System. The first user to register automatically becomes the Super Admin.

## Features Implemented

### Module 1: User Registration & Authentication ✅
- ✅ User signup with email & password
- ✅ Email verification (required before login)
- ✅ Login / Logout with cookie authentication
- ✅ Forgot password / Reset password
- ✅ First user automatically becomes Super Admin
- ✅ Account lockout after 5 failed login attempts
- ✅ Session management with sliding expiration
- ✅ Remember me functionality
- ✅ Password hashing with BCrypt
- ✅ Secure token generation for email verification and password reset

## Technology Stack
- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: MySQL 8.0
- **ORM**: Entity Framework Core
- **Authentication**: Cookie-based authentication
- **Password Hashing**: BCrypt.Net
- **Email**: MailKit

## Project Structure
```
Invoice-automation-V1/
├── Core/
│   ├── Entities/           # User, UserToken entities
│   ├── Interfaces/         # Service interfaces
│   ├── Services/           # Business logic (AuthService)
│   └── DTOs/               # Data transfer objects
├── Infrastructure/
│   ├── Data/               # ApplicationDbContext
│   ├── Repositories/       # Data access layer
│   └── Services/           # External services (EmailService)
├── Controllers/            # AccountController
├── Views/
│   ├── Account/            # Authentication views
│   └── Shared/             # _Layout
├── ViewModels/             # View models for forms
└── wwwroot/                # Static files
```

## Setup Instructions

### Prerequisites
1. .NET 8.0 SDK
2. MySQL 8.0 Server
3. Visual Studio 2022 or VS Code

### Database Setup

1. **Create MySQL Database**:
```sql
CREATE DATABASE invoice_automation;
```

2. **Update Connection String** in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=invoice_automation;User=root;Password=YOUR_PASSWORD;"
}
```

3. **Configure Email Settings** in `appsettings.json`:
```json
"Email": {
  "FromName": "Invoice Automation System",
  "FromAddress": "your_email@gmail.com",
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "Username": "your_email@gmail.com",
  "Password": "your_app_password"
}
```

For Gmail, you need to:
- Enable 2-factor authentication
- Generate an App Password: https://myaccount.google.com/apppasswords
- Use the app password in the configuration

### Running the Application

1. **Install Dependencies** (if using CLI):
```bash
cd Invoice-automation-V1/Invoice-automation-V1
dotnet restore
```

2. **Create Database Tables**:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

3. **Run the Application**:
```bash
dotnet run
```

4. **Access the Application**:
   - Open browser and navigate to: `https://localhost:5001` or `http://localhost:5000`

## Usage

### First User Registration (Super Admin)
1. Click "Register" in the navigation bar
2. Fill in the registration form:
   - Full Name
   - Email Address
   - Phone Number (optional)
   - Password (minimum 6 characters)
3. Click "Create Account"
4. Check your email for the verification link
5. Click the verification link in your email
6. You can now log in as Super Admin

### Login
1. Click "Login" in the navigation bar
2. Enter your email and password
3. Optionally check "Remember me" for persistent login
4. Click "Login"

### Password Reset
1. Click "Forgot password?" on the login page
2. Enter your email address
3. Check your email for the reset link
4. Click the link and enter your new password
5. You can now log in with the new password

## Database Schema

### Users Table
```sql
CREATE TABLE users (
    id CHAR(36) PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    normalized_email VARCHAR(255) NOT NULL,
    email_confirmed TINYINT(1) DEFAULT 0,
    password_hash VARCHAR(255) NOT NULL,
    security_stamp VARCHAR(255),
    concurrency_stamp VARCHAR(255),
    full_name VARCHAR(200) NOT NULL,
    phone VARCHAR(20),
    avatar_url VARCHAR(500),
    is_active TINYINT(1) DEFAULT 1,
    is_super_admin TINYINT(1) DEFAULT 0,
    lockout_end DATETIME,
    lockout_enabled TINYINT(1) DEFAULT 1,
    access_failed_count INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at DATETIME
);
```

### User Tokens Table
```sql
CREATE TABLE user_tokens (
    id CHAR(36) PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    token_type ENUM('EmailVerification', 'PasswordReset', 'RefreshToken') NOT NULL,
    token_hash VARCHAR(255) NOT NULL,
    expires_at DATETIME NOT NULL,
    used_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
```

## Security Features
- Password hashing with BCrypt (work factor: 10)
- Secure token generation using cryptographically strong random numbers
- Token hashing with SHA256
- Account lockout after 5 failed login attempts (30-minute lockout)
- Email verification required before login
- Secure cookies (HttpOnly, Secure, SameSite)
- HTTPS enforcement
- CSRF protection with anti-forgery tokens
- SQL injection prevention with parameterized queries

## API Endpoints

### Account Controller
- `GET /Account/Register` - Show registration form
- `POST /Account/Register` - Submit registration
- `GET /Account/Login` - Show login form
- `POST /Account/Login` - Submit login
- `POST /Account/Logout` - Log out user
- `GET /Account/VerifyEmail` - Verify email address
- `GET /Account/ForgotPassword` - Show forgot password form
- `POST /Account/ForgotPassword` - Submit forgot password request
- `GET /Account/ResetPassword` - Show reset password form
- `POST /Account/ResetPassword` - Submit new password
- `GET /Account/AccessDenied` - Show access denied page

## Next Steps - Module 2

The next module will implement:
- Company registration with NTN
- Connect to Indraaj API
- Default company logic
- Company settings and management

## Troubleshooting

### Email Not Sending
- Check your SMTP credentials in `appsettings.json`
- For Gmail, ensure you're using an App Password, not your regular password
- Check spam/junk folder

### Database Connection Issues
- Verify MySQL is running
- Check connection string in `appsettings.json`
- Ensure database exists

### Migration Issues
```bash
# Remove existing migrations
dotnet ef migrations remove

# Create new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## License
This project is part of the Invoice Automation System development.

## Support
For issues and questions, please refer to the project documentation or create an issue in the repository.
