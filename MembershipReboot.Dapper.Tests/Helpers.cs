using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;

namespace MembershipReboot.Dapper.Tests {
    public static class Helpers {
        public static SqlConnection CreateConnection(string cs) {
            var conn = new SqlConnection(cs);
            conn.Open();
            return conn;
        }

        public static SqlConnection CreateClosedConnection(string cs) {
            var conn = new SqlConnection(cs);
            return conn;
        }

        public static void ResetDatabase(SqlConnection connection) {
            connection.Execute(SqlQueries.DefaultInit);
        }

        public static void ResetDatabaseExtendedGroup(SqlConnection connection) {
            connection.Execute(SqlQueries.ExtendedGroupInit);
        }

        public static void ResetDatabaseExtendedUser(SqlConnection connection) {
            connection.Execute(SqlQueries.ExtendedUserInit);
        }

        public static TObj SetField<TObj, TProp>(TObj obj, TProp value, Expression<Func<TObj, TProp>> propExpr) {
            var type = typeof(TObj);
            var prop = type.GetProperty(propExpr.GetName(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop != null && prop.CanWrite) {
                prop.SetValue(obj, value);
            } else {
                throw new Exception($"Cannot get (or write) prop {propExpr.GetName()})");
            }
            return obj;
        }

        public static object CallMethod<TObj>(TObj obj, string methodName, object[] parameters) {
            var type = typeof(TObj);
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null) {
                return method.Invoke(obj, parameters);
            } else {
                throw new Exception($"Cannot call method {methodName})");
            }
        }

        public static LimitedPrecisionDateTimeComparer DateTimeComparer = new LimitedPrecisionDateTimeComparer();
        public static LimitedPrecisionNullableDateTimeComparer NullableDateTimeComparer = new LimitedPrecisionNullableDateTimeComparer();
    }

    public static class Extensions {
        public static T Exec<BO, T>(this Expression<Func<BO, T>> expr, BO obj) {
            return expr.Compile()(obj);
        }

        public static string GetName(this LambdaExpression expr) {
            if (expr.Body is MemberExpression) {
                return ((MemberExpression)expr.Body).Member.Name;
            }

            if (!(expr.Body is UnaryExpression)) {
                throw new ArgumentException(string.Format("Cannot get name from expression: {0}", expr), "expr");
            }

            UnaryExpression ue = (UnaryExpression)expr.Body;

            int maxIters = 4;
            while (maxIters-- >= 0 && ue != null) {
                if (ue.Operand is MemberExpression) {
                    return ((MemberExpression)ue.Operand).Member.Name;
                } else if (!(ue.Operand is UnaryExpression)) {
                    break;
                }

                ue = ue.Operand as UnaryExpression;
            }

            throw new ArgumentException(string.Format("Cannot get name from expression: {0}", expr), "expr");
        }

        public static TObj SetField<TObj, TProp>(this TObj obj, TProp value, Expression<Func<TObj, TProp>> propExpr) {
            return Helpers.SetField(obj, value, propExpr);
        }

        public static object CallMethod<TObj>(this TObj obj, string methodName, object[] parameters) {
            return Helpers.CallMethod(obj, methodName, parameters);
        }
    }

    public class LimitedPrecisionDateTimeComparer : IEqualityComparer<DateTime> {
        private ulong _millisecondPrecision;

        public LimitedPrecisionDateTimeComparer(ulong millisecondPrecision = 100) {
            _millisecondPrecision = millisecondPrecision;
        }

        public bool Equals(DateTime x, DateTime y) {
            return (ulong)Math.Abs(Math.Round((x - y).TotalMilliseconds)) < _millisecondPrecision;
        }

        public int GetHashCode(DateTime obj) {
            // NOTE(tim): This isn't really correct, because equality in this case
            // depends on the relation between the two datetimes.
            var milli = (ulong)Math.Round((obj - DateTime.MinValue).TotalMilliseconds);
            var result = milli - (milli % _millisecondPrecision);
            return result.GetHashCode();
        }
    }

    public class LimitedPrecisionNullableDateTimeComparer : IEqualityComparer<DateTime?> {
        private ulong _millisecondPrecision;

        public LimitedPrecisionNullableDateTimeComparer(ulong millisecondPrecision = 100) {
            _millisecondPrecision = millisecondPrecision;
        }

        public bool Equals(DateTime? x, DateTime? y) {
            if (!x.HasValue && !y.HasValue)
                return true;
            if (!x.HasValue || !y.HasValue)
                return false;

            return (ulong)Math.Abs(Math.Round((x.Value - y.Value).TotalMilliseconds)) < _millisecondPrecision;
        }

        public int GetHashCode(DateTime? obj) {
            if (!obj.HasValue)
                return 0;

            // NOTE(tim): This isn't really correct, because equality in this case
            // depends on the relation between the two datetimes.
            var milli = (ulong)Math.Round((obj.Value - DateTime.MinValue).TotalMilliseconds);
            var result = milli - (milli % _millisecondPrecision);
            return result.GetHashCode();
        }
    }

    public class FuncEqualityComparer<T> : IEqualityComparer<T> {
        private Func<T, T, bool> _comparer;

        public FuncEqualityComparer(Func<T, T, bool> comparer) {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            _comparer = comparer;
        }

        public bool Equals(T x, T y) {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;

            return _comparer(x, y);
        }

        public int GetHashCode(T obj) {
            return obj?.GetHashCode() ?? 0;
        }
    }

    #region Queries

    public static class SqlQueries {
        public static readonly string CheckDb =
@"IF NOT EXISTS(select * from master.sys.databases where Name = 'MembershipReboot')
	CREATE DATABASE MembershipReboot";

        private static readonly string DropTables = $@"
IF OBJECT_ID('GroupChilds', 'U') IS NOT NULL DROP TABLE GroupChilds
IF OBJECT_ID('Groups', 'U') IS NOT NULL DROP TABLE Groups
IF OBJECT_ID('ExtendedGroups', 'U') IS NOT NULL DROP TABLE ExtendedGroups
IF OBJECT_ID('LinkedAccountClaims', 'U') IS NOT NULL DROP TABLE LinkedAccountClaims
IF OBJECT_ID('LinkedAccounts', 'U') IS NOT NULL DROP TABLE LinkedAccounts
IF OBJECT_ID('PasswordResetSecrets', 'U') IS NOT NULL DROP TABLE PasswordResetSecrets
IF OBJECT_ID('TwoFactorAuthTokens', 'U') IS NOT NULL DROP TABLE TwoFactorAuthTokens
IF OBJECT_ID('UserCertificates', 'U') IS NOT NULL DROP TABLE UserCertificates
IF OBJECT_ID('UserClaims', 'U') IS NOT NULL DROP TABLE UserClaims
IF OBJECT_ID('UserAccounts', 'U') IS NOT NULL DROP TABLE UserAccounts
IF OBJECT_ID('ExtendedUserAccounts', 'U') IS NOT NULL DROP TABLE ExtendedUserAccounts";

        public static readonly string DefaultInit = $@"
{DropTables}

CREATE TABLE [dbo].[Groups] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.Groups] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[GroupChilds] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ChildGroupID] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_dbo.GroupChilds] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[GroupChilds]([ParentKey])
CREATE TABLE [dbo].[UserAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Username] [nvarchar](254) NOT NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    [AccountApproved] [datetime],
    [AccountRejected] [datetime],
    [IsAccountClosed] [bit] NOT NULL,
    [AccountClosed] [datetime],
    [IsLoginAllowed] [bit] NOT NULL,
    [LastLogin] [datetime],
    [LastFailedLogin] [datetime],
    [FailedLoginCount] [int] NOT NULL,
    [PasswordChanged] [datetime],
    [RequiresPasswordReset] [bit] NOT NULL,
    [Email] [nvarchar](254),
    [IsAccountVerified] [bit] NOT NULL,
    [LastFailedPasswordReset] [datetime],
    [FailedPasswordResetCount] [int] NOT NULL,
    [MobileCode] [nvarchar](100),
    [MobileCodeSent] [datetime],
    [MobilePhoneNumber] [nvarchar](20),
    [MobilePhoneNumberChanged] [datetime],
    [AccountTwoFactorAuthMode] [int] NOT NULL,
    [CurrentTwoFactorAuthStatus] [int] NOT NULL,
    [VerificationKey] [nvarchar](100),
    [VerificationPurpose] [int],
    [VerificationKeySent] [datetime],
    [VerificationStorage] [nvarchar](100),
    [HashedPassword] [nvarchar](200),
    CONSTRAINT [PK_dbo.UserAccounts] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[UserClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.UserClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccountClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccountClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccountClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [LastLogin] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccounts] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccounts]([ParentKey])
CREATE TABLE [dbo].[PasswordResetSecrets] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [PasswordResetSecretID] [uniqueidentifier] NOT NULL,
    [Question] [nvarchar](150) NOT NULL,
    [Answer] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.PasswordResetSecrets] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[PasswordResetSecrets]([ParentKey])
CREATE TABLE [dbo].[TwoFactorAuthTokens] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Token] [nvarchar](100) NOT NULL,
    [Issued] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.TwoFactorAuthTokens] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[TwoFactorAuthTokens]([ParentKey])
CREATE TABLE [dbo].[UserCertificates] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Thumbprint] [nvarchar](150) NOT NULL,
    [Subject] [nvarchar](250),
    CONSTRAINT [PK_dbo.UserCertificates] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserCertificates]([ParentKey])
ALTER TABLE [dbo].[GroupChilds] ADD CONSTRAINT [FK_dbo.GroupChilds_dbo.Groups_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[Groups] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserClaims] ADD CONSTRAINT [FK_dbo.UserClaims_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccountClaims] ADD CONSTRAINT [FK_dbo.LinkedAccountClaims_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccounts] ADD CONSTRAINT [FK_dbo.LinkedAccounts_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[PasswordResetSecrets] ADD CONSTRAINT [FK_dbo.PasswordResetSecrets_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[TwoFactorAuthTokens] ADD CONSTRAINT [FK_dbo.TwoFactorAuthTokens_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserCertificates] ADD CONSTRAINT [FK_dbo.UserCertificates_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE";

        public static readonly string ExtendedGroupInit = $@"
{DropTables}

CREATE TABLE [dbo].[ExtendedGroups] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
	[Details] [nvarchar](255) NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.ExtendedGroups] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[GroupChilds] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ChildGroupID] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_dbo.GroupChilds] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[GroupChilds]([ParentKey])
CREATE TABLE [dbo].[UserAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Username] [nvarchar](254) NOT NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    [AccountApproved] [datetime],
    [AccountRejected] [datetime],
    [IsAccountClosed] [bit] NOT NULL,
    [AccountClosed] [datetime],
    [IsLoginAllowed] [bit] NOT NULL,
    [LastLogin] [datetime],
    [LastFailedLogin] [datetime],
    [FailedLoginCount] [int] NOT NULL,
    [PasswordChanged] [datetime],
    [RequiresPasswordReset] [bit] NOT NULL,
    [Email] [nvarchar](254),
    [IsAccountVerified] [bit] NOT NULL,
    [LastFailedPasswordReset] [datetime],
    [FailedPasswordResetCount] [int] NOT NULL,
    [MobileCode] [nvarchar](100),
    [MobileCodeSent] [datetime],
    [MobilePhoneNumber] [nvarchar](20),
    [MobilePhoneNumberChanged] [datetime],
    [AccountTwoFactorAuthMode] [int] NOT NULL,
    [CurrentTwoFactorAuthStatus] [int] NOT NULL,
    [VerificationKey] [nvarchar](100),
    [VerificationPurpose] [int],
    [VerificationKeySent] [datetime],
    [VerificationStorage] [nvarchar](100),
    [HashedPassword] [nvarchar](200),
    CONSTRAINT [PK_dbo.UserAccounts] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[UserClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.UserClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccountClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccountClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccountClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [LastLogin] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccounts] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccounts]([ParentKey])
CREATE TABLE [dbo].[PasswordResetSecrets] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [PasswordResetSecretID] [uniqueidentifier] NOT NULL,
    [Question] [nvarchar](150) NOT NULL,
    [Answer] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.PasswordResetSecrets] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[PasswordResetSecrets]([ParentKey])
CREATE TABLE [dbo].[TwoFactorAuthTokens] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Token] [nvarchar](100) NOT NULL,
    [Issued] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.TwoFactorAuthTokens] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[TwoFactorAuthTokens]([ParentKey])
CREATE TABLE [dbo].[UserCertificates] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Thumbprint] [nvarchar](150) NOT NULL,
    [Subject] [nvarchar](250),
    CONSTRAINT [PK_dbo.UserCertificates] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserCertificates]([ParentKey])
ALTER TABLE [dbo].[GroupChilds] ADD CONSTRAINT [FK_dbo.GroupChilds_dbo.ExtendedGroups_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedGroups] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserClaims] ADD CONSTRAINT [FK_dbo.UserClaims_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccountClaims] ADD CONSTRAINT [FK_dbo.LinkedAccountClaims_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccounts] ADD CONSTRAINT [FK_dbo.LinkedAccounts_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[PasswordResetSecrets] ADD CONSTRAINT [FK_dbo.PasswordResetSecrets_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[TwoFactorAuthTokens] ADD CONSTRAINT [FK_dbo.TwoFactorAuthTokens_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserCertificates] ADD CONSTRAINT [FK_dbo.UserCertificates_dbo.UserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[UserAccounts] ([Key]) ON DELETE CASCADE";

        public static readonly string ExtendedUserInit = $@"
{DropTables}

CREATE TABLE [dbo].[Groups] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Name] [nvarchar](100) NOT NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.Groups] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[GroupChilds] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ChildGroupID] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_dbo.GroupChilds] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[GroupChilds]([ParentKey])
CREATE TABLE [dbo].[ExtendedUserAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ID] [uniqueidentifier] NOT NULL,
    [Tenant] [nvarchar](50) NOT NULL,
    [Username] [nvarchar](254) NOT NULL,
    [OtherField] [nvarchar](254) NOT NULL,
    [Created] [datetime] NOT NULL,
    [LastUpdated] [datetime] NOT NULL,
    [AccountApproved] [datetime],
    [AccountRejected] [datetime],
    [IsAccountClosed] [bit] NOT NULL,
    [AccountClosed] [datetime],
    [IsLoginAllowed] [bit] NOT NULL,
    [LastLogin] [datetime],
    [LastFailedLogin] [datetime],
    [FailedLoginCount] [int] NOT NULL,
    [PasswordChanged] [datetime],
    [RequiresPasswordReset] [bit] NOT NULL,
    [Email] [nvarchar](254),
    [IsAccountVerified] [bit] NOT NULL,
    [LastFailedPasswordReset] [datetime],
    [FailedPasswordResetCount] [int] NOT NULL,
    [MobileCode] [nvarchar](100),
    [MobileCodeSent] [datetime],
    [MobilePhoneNumber] [nvarchar](20),
    [MobilePhoneNumberChanged] [datetime],
    [AccountTwoFactorAuthMode] [int] NOT NULL,
    [CurrentTwoFactorAuthStatus] [int] NOT NULL,
    [VerificationKey] [nvarchar](100),
    [VerificationPurpose] [int],
    [VerificationKeySent] [datetime],
    [VerificationStorage] [nvarchar](100),
    [HashedPassword] [nvarchar](200),
    CONSTRAINT [PK_dbo.ExtendedUserAccounts] PRIMARY KEY ([Key])
)
CREATE TABLE [dbo].[UserClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.UserClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccountClaims] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [Type] [nvarchar](150) NOT NULL,
    [Value] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccountClaims] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccountClaims]([ParentKey])
CREATE TABLE [dbo].[LinkedAccounts] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [ProviderName] [nvarchar](200) NOT NULL,
    [ProviderAccountID] [nvarchar](100) NOT NULL,
    [LastLogin] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.LinkedAccounts] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[LinkedAccounts]([ParentKey])
CREATE TABLE [dbo].[PasswordResetSecrets] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [PasswordResetSecretID] [uniqueidentifier] NOT NULL,
    [Question] [nvarchar](150) NOT NULL,
    [Answer] [nvarchar](150) NOT NULL,
    CONSTRAINT [PK_dbo.PasswordResetSecrets] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[PasswordResetSecrets]([ParentKey])
CREATE TABLE [dbo].[TwoFactorAuthTokens] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Token] [nvarchar](100) NOT NULL,
    [Issued] [datetime] NOT NULL,
    CONSTRAINT [PK_dbo.TwoFactorAuthTokens] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[TwoFactorAuthTokens]([ParentKey])
CREATE TABLE [dbo].[UserCertificates] (
    [Key] [int] NOT NULL IDENTITY,
    [ParentKey] [int] NOT NULL,
    [Thumbprint] [nvarchar](150) NOT NULL,
    [Subject] [nvarchar](250),
    CONSTRAINT [PK_dbo.UserCertificates] PRIMARY KEY ([Key])
)
CREATE INDEX [IX_ParentKey] ON [dbo].[UserCertificates]([ParentKey])
ALTER TABLE [dbo].[GroupChilds] ADD CONSTRAINT [FK_dbo.GroupChilds_dbo.Groups_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[Groups] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserClaims] ADD CONSTRAINT [FK_dbo.UserClaims_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccountClaims] ADD CONSTRAINT [FK_dbo.LinkedAccountClaims_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[LinkedAccounts] ADD CONSTRAINT [FK_dbo.LinkedAccounts_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[PasswordResetSecrets] ADD CONSTRAINT [FK_dbo.PasswordResetSecrets_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[TwoFactorAuthTokens] ADD CONSTRAINT [FK_dbo.TwoFactorAuthTokens_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE
ALTER TABLE [dbo].[UserCertificates] ADD CONSTRAINT [FK_dbo.UserCertificates_dbo.ExtendedUserAccounts_ParentKey] FOREIGN KEY ([ParentKey]) REFERENCES [dbo].[ExtendedUserAccounts] ([Key]) ON DELETE CASCADE";
    }

    #endregion Queries
}
