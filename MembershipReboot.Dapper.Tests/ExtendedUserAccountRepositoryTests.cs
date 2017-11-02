using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Relational;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace MembershipReboot.Dapper.Tests {
    public class ExtendedUserAccountRepositoryTests : IDisposable, IClassFixture<CheckDatabase> {
        #region Fields

        public static string ConnectionString => @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=MembershipReboot;Integrated Security=True";

        #endregion Fields

        #region Constructor and Disposable

        public ExtendedUserAccountRepositoryTests() { }

        public void Dispose() { }

        #endregion Constructor and Disposable

        #region Static

        private SqlConnection CreateConnection() => Helpers.CreateConnection(ConnectionString);
        private SqlConnection CreateClosedConnection() => Helpers.CreateClosedConnection(ConnectionString);
        private void ResetDatabase(SqlConnection connection) => Helpers.ResetDatabaseExtendedUser(connection);
        private TObj SetField<TObj, TProp>(TObj obj, TProp value, Expression<Func<TObj, TProp>> propExpr) => Helpers.SetField(obj, value, propExpr);
        private void CallMethod<TObj>(TObj obj, string methodName, object[] parameters) => Helpers.CallMethod(obj, methodName, parameters);
        private LimitedPrecisionDateTimeComparer DateTimeComparer => Helpers.DateTimeComparer;
        private LimitedPrecisionNullableDateTimeComparer NullableDateTimeComparer => Helpers.NullableDateTimeComparer;

        private static DapperUserAccountRepository<ExtendedUserAccount> CreateRepository(IDbConnection connection) {
            var repo = new DapperUserAccountRepository<ExtendedUserAccount>(connection, userAccountTable: "ExtendedUserAccounts");
            return repo;
        }

        private static DapperUserAccountRepository<ExtendedUserAccount> CreateRepository(IDbConnection connection, string userAccountTable,
            string userCertificateTable, string userClaimTable, string linkedAccountTable, string linkedAccountClaimTable,
            string twoFactorAuthTokenTable, string passwordResetSecretTable) {

            var repo = new DapperUserAccountRepository<ExtendedUserAccount>(connection, userAccountTable: userAccountTable,
                tableNameMap: new Dictionary<Type, string> {
                    [typeof(RelationalUserCertificate)] = userCertificateTable,
                    [typeof(RelationalUserClaim)] = userClaimTable,
                    [typeof(RelationalLinkedAccount)] = linkedAccountTable,
                    [typeof(RelationalLinkedAccountClaim)] = linkedAccountClaimTable,
                    [typeof(RelationalTwoFactorAuthToken)] = twoFactorAuthTokenTable,
                    [typeof(RelationalPasswordResetSecret)] = passwordResetSecretTable,
                });
            return repo;
        }

        #endregion Static

        #region Helpers

        private Guid GetFirstID(SqlConnection conn) {
            var id = conn.Query<Guid>("select ID from ExtendedUserAccounts").FirstOrDefault();
            Assert.True(id != Guid.Empty);
            return id;
        }

        private int GetFirstKey(SqlConnection conn) {
            var key = conn.Query<int>("select [Key] from ExtendedUserAccounts").FirstOrDefault();
            Assert.True(key > 0);
            return key;
        }

        private void AssertUserCount(SqlConnection conn, int count) {
            Assert.Equal(count, conn.Query<int>("select count(*) from ExtendedUserAccounts").FirstOrDefault());
        }

        private void AssertCertCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from UserCertificates {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertClaimCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from UserClaims {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertLinkedCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from LinkedAccounts {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertLinkedClaimsCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from LinkedAccountClaims {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertTwoFactorCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from TwoFactorAuthTokens {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertResetSecretCount(SqlConnection conn, int count, int parentKey = -1) {
            Assert.Equal(count, conn.Query<int>($"select count(*) from PasswordResetSecrets {(parentKey <= 0 ? "" : "where [ParentKey] = @parentKey")}", new { parentKey }).FirstOrDefault());
        }

        private void AssertUserIsValid(ExtendedUserAccount user, bool checkKey = true) {
            Assert.NotNull(user);
            if (checkKey) {
                Assert.True(user.Key > 0);
            }
            Assert.NotNull(user.UserCertificateCollection);
            Assert.NotNull(user.ClaimCollection);
            Assert.NotNull(user.LinkedAccountCollection);
            Assert.NotNull(user.LinkedAccountClaimCollection);
            Assert.NotNull(user.TwoFactorAuthTokenCollection);
            Assert.NotNull(user.PasswordResetSecretCollection);
        }

        private void AssertUsersEqual(ExtendedUserAccount user1, ExtendedUserAccount user2, bool compareKey = true, bool compareCollections = false) {
            AssertUserIsValid(user1, compareKey);
            AssertUserIsValid(user2, compareKey);

            if (compareKey) {
                Assert.Equal(user1.Key, user2.Key);
            }
            Assert.Equal(user1.ID, user2.ID);
            Assert.Equal(user1.Tenant, user2.Tenant);
            Assert.Equal(user1.Username, user2.Username);
            Assert.Equal(user1.OtherField, user2.OtherField);
            Assert.Equal(user1.Created, user2.Created, DateTimeComparer);
            Assert.Equal(user1.LastUpdated, user2.LastUpdated, DateTimeComparer);
            Assert.Equal(user1.IsAccountClosed, user2.IsAccountClosed);
            Assert.Equal(user1.AccountClosed, user2.AccountClosed, NullableDateTimeComparer);
            Assert.Equal(user1.IsLoginAllowed, user2.IsLoginAllowed);
            Assert.Equal(user1.LastLogin, user2.LastLogin, NullableDateTimeComparer);
            Assert.Equal(user1.LastFailedLogin, user2.LastFailedLogin, NullableDateTimeComparer);
            Assert.Equal(user1.FailedLoginCount, user2.FailedLoginCount);
            Assert.Equal(user1.PasswordChanged, user2.PasswordChanged, NullableDateTimeComparer);
            Assert.Equal(user1.RequiresPasswordReset, user2.RequiresPasswordReset);
            Assert.Equal(user1.Email, user2.Email);
            Assert.Equal(user1.IsAccountVerified, user2.IsAccountVerified);
            Assert.Equal(user1.LastFailedPasswordReset, user2.LastFailedPasswordReset, NullableDateTimeComparer);
            Assert.Equal(user1.MobileCode, user2.MobileCode);
            Assert.Equal(user1.MobileCodeSent, user2.MobileCodeSent, NullableDateTimeComparer);
            Assert.Equal(user1.MobilePhoneNumber, user2.MobilePhoneNumber);
            Assert.Equal(user1.MobilePhoneNumberChanged, user2.MobilePhoneNumberChanged, NullableDateTimeComparer);
            Assert.Equal(user1.AccountTwoFactorAuthMode, user2.AccountTwoFactorAuthMode);
            Assert.Equal(user1.CurrentTwoFactorAuthStatus, user2.CurrentTwoFactorAuthStatus);
            Assert.Equal(user1.VerificationKey, user2.VerificationKey);
            Assert.Equal(user1.VerificationPurpose, user2.VerificationPurpose);
            Assert.Equal(user1.VerificationKeySent, user2.VerificationKeySent, NullableDateTimeComparer);
            Assert.Equal(user1.VerificationStorage, user2.VerificationStorage);
            Assert.Equal(user1.HashedPassword, user2.HashedPassword);

            if (compareCollections) {
                AssertCollectionEqual(user1.UserCertificateCollection, user2.UserCertificateCollection, (a, b) => a.Thumbprint == b.Thumbprint, m => Tuple.Create(m.Key, m.Thumbprint));
                AssertCollectionEqual(user1.ClaimCollection, user2.ClaimCollection, (a, b) => a.Type == b.Type && a.Value == b.Value, m => Tuple.Create(m.Key, m.Value));
                AssertCollectionEqual(user1.LinkedAccountCollection, user2.LinkedAccountCollection, (a, b) => a.ProviderAccountID == b.ProviderAccountID && a.ProviderName == b.ProviderName, m => Tuple.Create(m.Key, m.ProviderAccountID));
                AssertCollectionEqual(user1.LinkedAccountClaimCollection, user2.LinkedAccountClaimCollection, (a, b) => a.ProviderAccountID == b.ProviderAccountID && a.ProviderName == b.ProviderName && a.Type == b.Type && a.Value == b.Value, m => Tuple.Create(m.Key, m.Value));
                AssertCollectionEqual(user1.TwoFactorAuthTokenCollection, user2.TwoFactorAuthTokenCollection, (a, b) => a.Token == b.Token, m => Tuple.Create(m.Key, m.Token));
                AssertCollectionEqual(user1.PasswordResetSecretCollection, user2.PasswordResetSecretCollection, (a, b) => a.PasswordResetSecretID == b.PasswordResetSecretID, m => Tuple.Create(m.Key, m.Question));
            }
        }

        private void AssertCollectionEqual<T, U>(ICollection<T> col1, ICollection<T> col2, Func<T, T, bool> comparer, Func<T, Tuple<int, U>> orderer) {
            Assert.NotNull(col1);
            Assert.NotNull(col2);
            Assert.Equal(col1.Count, col2.Count);
            var funcComparer = new FuncEqualityComparer<T>(comparer);
            Assert.Equal(col1.OrderBy(orderer), col2.OrderBy(orderer), funcComparer);
        }

        private ExtendedUserAccount CreateUser(DapperUserAccountRepository<ExtendedUserAccount> repo, bool includeChildren = true) {
            var user = repo.Create()
                    .SetField(Guid.NewGuid(), m => m.ID)
                    .SetField("default", m => m.Tenant)
                    .SetField("username", m => m.Username)
                    .SetField("other_data", m => m.OtherField)
                    .SetField("testemail@example.com", m => m.Email)
                    .SetField("123456789", m => m.MobilePhoneNumber)
                    .SetField("__verification_key__", m => m.VerificationKey)
                    .SetField(DateTime.Now, m => m.Created)
                    .SetField(DateTime.Now, m => m.LastUpdated);

            if (includeChildren) {
                {
                    var child1 = new UserCertificate()
                        .SetField("subject1", m => m.Subject)
                        .SetField("thumbprint1", m => m.Thumbprint);
                    var child2 = new UserCertificate()
                        .SetField("subject2", m => m.Subject)
                        .SetField("thumbprint2", m => m.Thumbprint);
                    user.CallMethod("AddCertificate", new object[] { child1 });
                    user.CallMethod("AddCertificate", new object[] { child2 });
                }

                {
                    var child1 = new UserClaim("type1", "value1");
                    var child2 = new UserClaim("type2", "value2");
                    user.CallMethod("AddClaim", new object[] { child1 });
                    user.CallMethod("AddClaim", new object[] { child2 });
                }

                {
                    var child1 = new LinkedAccount()
                        .SetField("name1", m => m.ProviderName)
                        .SetField("id1", m => m.ProviderAccountID)
                        .SetField(DateTime.Now, m => m.LastLogin);
                    var child2 = new LinkedAccount()
                        .SetField("name2", m => m.ProviderName)
                        .SetField("id2", m => m.ProviderAccountID)
                        .SetField(DateTime.Now, m => m.LastLogin);
                    user.CallMethod("AddLinkedAccount", new object[] { child1 });
                    user.CallMethod("AddLinkedAccount", new object[] { child2 });
                }

                {
                    var child1 = new LinkedAccountClaim()
                        .SetField("name1", m => m.ProviderName)
                        .SetField("id1", m => m.ProviderAccountID)
                        .SetField("type1", m => m.Type)
                        .SetField("value1", m => m.Value);
                    var child2 = new LinkedAccountClaim()
                        .SetField("name2", m => m.ProviderName)
                        .SetField("id2", m => m.ProviderAccountID)
                        .SetField("type2", m => m.Type)
                        .SetField("value2", m => m.Value);
                    user.CallMethod("AddLinkedAccountClaim", new object[] { child1 });
                    user.CallMethod("AddLinkedAccountClaim", new object[] { child2 });
                }

                {
                    var child1 = new TwoFactorAuthToken()
                        .SetField("token1", m => m.Token)
                        .SetField(DateTime.Now, m => m.Issued);
                    var child2 = new TwoFactorAuthToken()
                        .SetField("token2", m => m.Token)
                        .SetField(DateTime.Now, m => m.Issued);
                    user.CallMethod("AddTwoFactorAuthToken", new object[] { child1 });
                    user.CallMethod("AddTwoFactorAuthToken", new object[] { child2 });
                }

                {
                    var child1 = new PasswordResetSecret()
                        .SetField(Guid.NewGuid(), m => m.PasswordResetSecretID)
                        .SetField("question1", m => m.Question)
                        .SetField("answer1", m => m.Answer);
                    var child2 = new PasswordResetSecret()
                        .SetField(Guid.NewGuid(), m => m.PasswordResetSecretID)
                        .SetField("question2", m => m.Question)
                        .SetField("answer2", m => m.Answer);
                    user.CallMethod("AddPasswordResetSecret", new object[] { child1 });
                    user.CallMethod("AddPasswordResetSecret", new object[] { child2 });
                }
            }

            return user;
        }

        private void AssertExceptionMessage(Exception e, string message) {
            Assert.Equal(message, e.Message);
        }

        #endregion Helpers

        #region Tests

        [Fact]
        public void NullConnectionThrows() {
            Assert.Throws<ArgumentNullException>("connection", () => CreateRepository(null));
        }

        [Fact]
        public void CanCreateRepo() {
            using (var conn = CreateConnection()) {
                Assert.True(conn.State == ConnectionState.Open);
                var repo = CreateRepository(conn);
                Assert.NotNull(repo);
                Assert.True(conn.State == ConnectionState.Open);
                Assert.Equal(conn, repo.Connection);
            }
        }

        [Fact]
        public void ClosedConnectionIsOpened() {
            using (var conn = CreateClosedConnection()) {
                Assert.True(conn.State == ConnectionState.Closed);
                var repo = CreateRepository(conn);
                Assert.NotNull(repo);
                Assert.True(conn.State == ConnectionState.Open);
                Assert.Equal(conn, repo.Connection);
            }
        }

        [Fact]
        public void DefaultTableNamesAreSet() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Equal("ExtendedUserAccounts", repo.UserAccountTable);
                Assert.Equal("UserCertificates", repo.TableNameMap[typeof(RelationalUserCertificate)]);
                Assert.Equal("UserClaims", repo.TableNameMap[typeof(RelationalUserClaim)]);
                Assert.Equal("LinkedAccounts", repo.TableNameMap[typeof(RelationalLinkedAccount)]);
                Assert.Equal("LinkedAccountClaims", repo.TableNameMap[typeof(RelationalLinkedAccountClaim)]);
                Assert.Equal("TwoFactorAuthTokens", repo.TableNameMap[typeof(RelationalTwoFactorAuthToken)]);
                Assert.Equal("PasswordResetSecrets", repo.TableNameMap[typeof(RelationalPasswordResetSecret)]);
            }
        }

        [Fact]
        public void AlternativeTableNamesAreSet() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets");
                Assert.Equal("user_accounts", repo.UserAccountTable);
                Assert.Equal("user_certificates", repo.TableNameMap[typeof(RelationalUserCertificate)]);
                Assert.Equal("user_claims", repo.TableNameMap[typeof(RelationalUserClaim)]);
                Assert.Equal("linked_accounts", repo.TableNameMap[typeof(RelationalLinkedAccount)]);
                Assert.Equal("linked_account_claims", repo.TableNameMap[typeof(RelationalLinkedAccountClaim)]);
                Assert.Equal("two_factor_auth_tokens", repo.TableNameMap[typeof(RelationalTwoFactorAuthToken)]);
                Assert.Equal("password_reset_secrets", repo.TableNameMap[typeof(RelationalPasswordResetSecret)]);
            }
        }

        [Fact]
        public void AlternativeTableNamesAreEscaped() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn, "user[_acc]ounts", "user_c][][ertificates", "use[[]r_claims",
                    "linked_acco][][unts", "linked_acc]][]]ount_claims", "two_factor_au][]th_tokens", "][][]password_reset_secrets");
                Assert.Equal("user[[_acc]]ounts", repo.UserAccountTable);
                Assert.Equal("user_c]][[]][[ertificates", repo.TableNameMap[typeof(RelationalUserCertificate)]);
                Assert.Equal("use[[[[]]r_claims", repo.TableNameMap[typeof(RelationalUserClaim)]);
                Assert.Equal("linked_acco]][[]][[unts", repo.TableNameMap[typeof(RelationalLinkedAccount)]);
                Assert.Equal("linked_acc]]]][[]]]]ount_claims", repo.TableNameMap[typeof(RelationalLinkedAccountClaim)]);
                Assert.Equal("two_factor_au]][[]]th_tokens", repo.TableNameMap[typeof(RelationalTwoFactorAuthToken)]);
                Assert.Equal("]][[]][[]]password_reset_secrets", repo.TableNameMap[typeof(RelationalPasswordResetSecret)]);
            }
        }

        [Fact]
        public void NullEmptyOrWhitespaceTableNameThrows() {
            using (var conn = CreateConnection()) {
                Assert.Throws<ArgumentNullException>("userAccountTable", () => CreateRepository(conn, null, "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets"));
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", null, "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserCertificate is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", null,
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    null, "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccount is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", null, "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccountClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", null, "password_reset_secrets")), $"The table name specified for RelationalTwoFactorAuthToken is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", null)), $"The table name specified for RelationalPasswordResetSecret is invalid.");

                Assert.Throws<ArgumentNullException>("userAccountTable", () => CreateRepository(conn, "", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets"));
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserCertificate is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccount is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccountClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "", "password_reset_secrets")), $"The table name specified for RelationalTwoFactorAuthToken is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "")), $"The table name specified for RelationalPasswordResetSecret is invalid.");

                Assert.Throws<ArgumentNullException>("userAccountTable", () => CreateRepository(conn, "    ", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets"));
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "    ", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserCertificate is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "    ",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalUserClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "    ", "linked_account_claims", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccount is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "    ", "two_factor_auth_tokens", "password_reset_secrets")), $"The table name specified for RelationalLinkedAccountClaim is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "    ", "password_reset_secrets")), $"The table name specified for RelationalTwoFactorAuthToken is invalid.");
                AssertExceptionMessage(Assert.Throws<Exception>(() => CreateRepository(conn, "user_accounts", "user_certificates", "user_claims",
                    "linked_accounts", "linked_account_claims", "two_factor_auth_tokens", "    ")), $"The table name specified for RelationalPasswordResetSecret is invalid.");
            }
        }

        [Fact]
        public void CreateReturnsNonNullRelationalGroup() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);

                var newUser = repo.Create();
                Assert.NotNull(newUser);
                Assert.IsAssignableFrom<RelationalUserAccount>(newUser);
                Assert.NotNull(newUser.ClaimCollection);
                Assert.NotNull(newUser.LinkedAccountClaimCollection);
                Assert.NotNull(newUser.LinkedAccountCollection);
                Assert.NotNull(newUser.PasswordResetSecretCollection);
                Assert.NotNull(newUser.TwoFactorAuthTokenCollection);
                Assert.NotNull(newUser.UserCertificateCollection);
            }
        }

        [Fact]
        public void AddingNullRecordThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("item", () => repo.Add(null));
            }
        }

        [Fact]
        public void CanAddNewUser() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);

                repo.Add(user);

                AssertUserCount(conn, 1);

                var userFromDb = conn.Query<RelationalUserAccount>("select * from ExtendedUserAccounts").FirstOrDefault();
                Assert.NotNull(userFromDb);
                Assert.Equal(user.ID, userFromDb.ID);
                Assert.Equal(user.Tenant, userFromDb.Tenant);
                Assert.Equal(user.Username, userFromDb.Username);
                Assert.Equal(user.Email, userFromDb.Email);
                Assert.Equal(user.Created, userFromDb.Created, DateTimeComparer);
                Assert.Equal(user.LastUpdated, userFromDb.LastUpdated, DateTimeComparer);
            }
        }

        [Fact]
        public void CanAddNewUserWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);

                AssertUserCount(conn, 1);

                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);
            }
        }

        [Fact]
        public void ChildrenOfUserHaveCorrectParentID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);

                AssertUserCount(conn, 1);
                var userKey = GetFirstKey(conn);

                AssertCertCount(conn, 2, userKey);
                AssertClaimCount(conn, 2, userKey);
                AssertLinkedCount(conn, 2, userKey);
                AssertLinkedClaimsCount(conn, 2, userKey);
                AssertTwoFactorCount(conn, 2, userKey);
                AssertResetSecretCount(conn, 2, userKey);

                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from UserCertificates"));
                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from UserClaims"));
                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from LinkedAccounts"));
                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from LinkedAccountClaims"));
                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from TwoFactorAuthTokens"));
                Assert.Equal(new int[] { userKey, userKey }, conn.Query<int>("select [ParentKey] from PasswordResetSecrets"));
            }
        }

        [Fact]
        public void NonExistantIDReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = repo.GetByID(Guid.NewGuid());
                Assert.Null(user);
            }
        }

        [Fact]
        public void CanGetUserByID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByID(id);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByIDWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByID(id);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void CanGetCorrectUserByIDWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                repo.Add(CreateUser(repo));
                repo.Add(CreateUser(repo));
                repo.Add(CreateUser(repo));

                AssertUserCount(conn, 4);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 8);
                AssertClaimCount(conn, 8);
                AssertLinkedCount(conn, 8);
                AssertLinkedClaimsCount(conn, 8);
                AssertTwoFactorCount(conn, 8);
                AssertResetSecretCount(conn, 8);

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByID(id);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void GetByUsernameNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername(null));
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByUsername(null, "username"));
                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername("tenant", null));

                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername(""));
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByUsername("", "username"));
                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername("tenant", ""));

                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername("    "));
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByUsername("    ", "username"));
                Assert.Throws<ArgumentNullException>("username", () => repo.GetByUsername("tenant", "    "));
            }
        }

        [Fact]
        public void GetByWrongUsernameReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByUsername("wrong"));
                Assert.Null(repo.GetByUsername("wrong", "username"));
                Assert.Null(repo.GetByUsername("default", "wrong"));
            }
        }

        [Fact]
        public void CanGetUserByName() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb1 = repo.GetByUsername("username");
                AssertUserIsValid(userFromDb1);
                AssertUsersEqual(user, userFromDb1, false);

                var userFromDb2 = repo.GetByUsername("default", "username");
                AssertUserIsValid(userFromDb2);
                AssertUsersEqual(user, userFromDb2, false);
            }
        }

        [Fact]
        public void CanGetUserByNameWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb1 = repo.GetByUsername("username");
                AssertUserIsValid(userFromDb1);
                AssertUsersEqual(user, userFromDb1, true, true);

                var userFromDb2 = repo.GetByUsername("default", "username");
                AssertUserIsValid(userFromDb2);
                AssertUsersEqual(user, userFromDb2, true, true);
            }
        }

        [Fact]
        public void GetByEmailNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByEmail(null, "email"));
                Assert.Throws<ArgumentNullException>("email", () => repo.GetByEmail("tenant", null));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByEmail("", "email"));
                Assert.Throws<ArgumentNullException>("email", () => repo.GetByEmail("tenant", ""));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByEmail("    ", "email"));
                Assert.Throws<ArgumentNullException>("email", () => repo.GetByEmail("tenant", "    "));
            }
        }

        [Fact]
        public void GetByWrongEmailReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByEmail("wrong", "testemail@example.com"));
                Assert.Null(repo.GetByEmail("default", "wrong"));
            }
        }

        [Fact]
        public void CanGetUserByEmail() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByEmail("default", "testemail@example.com");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByEmailWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByEmail("default", "testemail@example.com");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void GetByVerificationKeyNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("key", () => repo.GetByVerificationKey(null));

                Assert.Throws<ArgumentNullException>("key", () => repo.GetByVerificationKey(""));

                Assert.Throws<ArgumentNullException>("key", () => repo.GetByVerificationKey("    "));
            }
        }

        [Fact]
        public void GetByWrongVerificationKeyReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByVerificationKey("wrong"));
            }
        }

        [Fact]
        public void CanGetUserByVerificationKey() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByVerificationKey("__verification_key__");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByVerificationKeyWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByVerificationKey("__verification_key__");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void GetByCertificateNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByCertificate(null, "thumbprint"));
                Assert.Throws<ArgumentNullException>("thumbprint", () => repo.GetByCertificate("tenant", null));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByCertificate("", "thumbprint"));
                Assert.Throws<ArgumentNullException>("thumbprint", () => repo.GetByCertificate("tenant", ""));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByCertificate("    ", "thumbprint"));
                Assert.Throws<ArgumentNullException>("thumbprint", () => repo.GetByCertificate("tenant", "    "));
            }
        }

        [Fact]
        public void GetByWrongCertificateReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByCertificate("wrong", "thumbprint1"));
                Assert.Null(repo.GetByCertificate("default", "wrong"));
            }
        }

        [Fact]
        public void CanGetUserByCertificate() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByCertificate("default", "thumbprint1");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByCertificateWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByCertificate("default", "thumbprint2");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void GetByLinkedAccountNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByLinkedAccount(null, "provider", "id"));
                Assert.Throws<ArgumentNullException>("provider", () => repo.GetByLinkedAccount("tenant", null, "id"));
                Assert.Throws<ArgumentNullException>("id", () => repo.GetByLinkedAccount("tenant", "provider", null));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByLinkedAccount("", "provider", "id"));
                Assert.Throws<ArgumentNullException>("provider", () => repo.GetByLinkedAccount("tenant", "", "id"));
                Assert.Throws<ArgumentNullException>("id", () => repo.GetByLinkedAccount("tenant", "provider", ""));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByLinkedAccount("  ", "provider", "id"));
                Assert.Throws<ArgumentNullException>("provider", () => repo.GetByLinkedAccount("tenant", "  ", "id"));
                Assert.Throws<ArgumentNullException>("id", () => repo.GetByLinkedAccount("tenant", "provider", "  "));
            }
        }

        [Fact]
        public void GetByWrongLinkedAccountReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByLinkedAccount("wrong", "name1", "id1"));
                Assert.Null(repo.GetByLinkedAccount("tenant", "wrong", "id1"));
                Assert.Null(repo.GetByLinkedAccount("tenant", "name1", "wrong"));
            }
        }

        [Fact]
        public void CanGetLinkedAccountByCertificate() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByLinkedAccount("default", "name1", "id1");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByLinkedAccountWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByLinkedAccount("default", "name2", "id2");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void GetByMobilePhoneNullEmptyOrWhitespaceThrows() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByMobilePhone(null, "phone"));
                Assert.Throws<ArgumentNullException>("phone", () => repo.GetByMobilePhone("tenant", null));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByMobilePhone("", "phone"));
                Assert.Throws<ArgumentNullException>("phone", () => repo.GetByMobilePhone("tenant", ""));

                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByMobilePhone("    ", "phone"));
                Assert.Throws<ArgumentNullException>("phone", () => repo.GetByMobilePhone("tenant", "    "));
            }
        }

        [Fact]
        public void GetByWrongMobilePhoneReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                repo.Add(CreateUser(repo, false));
                AssertUserCount(conn, 1);

                Assert.Null(repo.GetByMobilePhone("wrong", "123456789"));
                Assert.Null(repo.GetByMobilePhone("default", "wrong"));
            }
        }

        [Fact]
        public void CanGetUserByMobilePhone() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);

                var userFromDb = repo.GetByMobilePhone("default", "123456789");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);
            }
        }

        [Fact]
        public void CanGetUserByMobilePhoneWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);
                var userID = user.ID;

                repo.Add(user);
                AssertUserCount(conn, 1);
                var id = GetFirstID(conn);
                Assert.Equal(userID, id);
                var key = GetFirstKey(conn);
                user.Key = key;

                AssertCertCount(conn, 2, key);
                AssertClaimCount(conn, 2, key);
                AssertLinkedCount(conn, 2, key);
                AssertLinkedClaimsCount(conn, 2, key);
                AssertTwoFactorCount(conn, 2, key);
                AssertResetSecretCount(conn, 2, key);

                var userFromDb = repo.GetByMobilePhone("default", "123456789");
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, true, true);
            }
        }

        [Fact]
        public void UpdatingNullRecordThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("item", () => repo.Update(null));
            }
        }

        [Fact]
        public void CanUpdateUser() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);

                repo.Add(user);
                AssertUserCount(conn, 1);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);

                userFromDb.SetField("new_username", m => m.Username);
                userFromDb.SetField("1999999999", m => m.MobilePhoneNumber);
                userFromDb.SetField("changed_other_data", m => m.OtherField);

                repo.Update(userFromDb);
                AssertUserCount(conn, 1);

                userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                Assert.Equal(user.ID, userFromDb.ID);
                Assert.NotEqual(user.Username, userFromDb.Username);
                Assert.NotEqual(user.MobilePhoneNumber, userFromDb.MobilePhoneNumber);
                Assert.NotEqual(user.OtherField, userFromDb.OtherField);
            }
        }

        [Fact]
        public void CanUpdateUserWithNewChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false, true);

                {
                    var child3 = new UserCertificate()
                        .SetField("subject3", m => m.Subject)
                        .SetField("thumbprint3", m => m.Thumbprint);
                    var child4 = new UserCertificate()
                        .SetField("subject4", m => m.Subject)
                        .SetField("thumbprint4", m => m.Thumbprint);
                    userFromDb.CallMethod("AddCertificate", new object[] { child3 });
                    userFromDb.CallMethod("AddCertificate", new object[] { child4 });
                }

                {
                    var child3 = new UserClaim("type3", "value3");
                    var child4 = new UserClaim("type4", "value4");
                    userFromDb.CallMethod("AddClaim", new object[] { child3 });
                    userFromDb.CallMethod("AddClaim", new object[] { child4 });
                }

                {
                    var child3 = new LinkedAccount()
                        .SetField("name3", m => m.ProviderName)
                        .SetField("id3", m => m.ProviderAccountID)
                        .SetField(DateTime.Now, m => m.LastLogin);
                    var child4 = new LinkedAccount()
                        .SetField("name4", m => m.ProviderName)
                        .SetField("id4", m => m.ProviderAccountID)
                        .SetField(DateTime.Now, m => m.LastLogin);
                    userFromDb.CallMethod("AddLinkedAccount", new object[] { child3 });
                    userFromDb.CallMethod("AddLinkedAccount", new object[] { child4 });
                }

                {
                    var child3 = new LinkedAccountClaim()
                        .SetField("name3", m => m.ProviderName)
                        .SetField("id3", m => m.ProviderAccountID)
                        .SetField("type3", m => m.Type)
                        .SetField("value3", m => m.Value);
                    var child4 = new LinkedAccountClaim()
                        .SetField("name4", m => m.ProviderName)
                        .SetField("id4", m => m.ProviderAccountID)
                        .SetField("type4", m => m.Type)
                        .SetField("value4", m => m.Value);
                    userFromDb.CallMethod("AddLinkedAccountClaim", new object[] { child3 });
                    userFromDb.CallMethod("AddLinkedAccountClaim", new object[] { child4 });
                }

                {
                    var child3 = new TwoFactorAuthToken()
                        .SetField("token3", m => m.Token)
                        .SetField(DateTime.Now, m => m.Issued);
                    var child4 = new TwoFactorAuthToken()
                        .SetField("token4", m => m.Token)
                        .SetField(DateTime.Now, m => m.Issued);
                    userFromDb.CallMethod("AddTwoFactorAuthToken", new object[] { child3 });
                    userFromDb.CallMethod("AddTwoFactorAuthToken", new object[] { child4 });
                }

                {
                    var child3 = new PasswordResetSecret()
                        .SetField(Guid.NewGuid(), m => m.PasswordResetSecretID)
                        .SetField("question3", m => m.Question)
                        .SetField("answer3", m => m.Answer);
                    var child4 = new PasswordResetSecret()
                        .SetField(Guid.NewGuid(), m => m.PasswordResetSecretID)
                        .SetField("question4", m => m.Question)
                        .SetField("answer4", m => m.Answer);
                    userFromDb.CallMethod("AddPasswordResetSecret", new object[] { child3 });
                    userFromDb.CallMethod("AddPasswordResetSecret", new object[] { child4 });
                }

                repo.Update(userFromDb);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 4);
                AssertClaimCount(conn, 4);
                AssertLinkedCount(conn, 4);
                AssertLinkedClaimsCount(conn, 4);
                AssertTwoFactorCount(conn, 4);
                AssertResetSecretCount(conn, 4);

                userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                Assert.Equal(4, userFromDb.UserCertificateCollection.Count);
                Assert.Equal(4, userFromDb.ClaimCollection.Count);
                Assert.Equal(4, userFromDb.LinkedAccountCollection.Count);
                Assert.Equal(4, userFromDb.LinkedAccountClaimCollection.Count);
                Assert.Equal(4, userFromDb.TwoFactorAuthTokenCollection.Count);
                Assert.Equal(4, userFromDb.PasswordResetSecretCollection.Count);
            }
        }

        [Fact]
        public void CanUpdateUserChild() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false, true);

                Assert.Equal("thumbprint1", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("type1", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("name1", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("value1", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("token1", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("answer1", userFromDb.PasswordResetSecretCollection.First().Answer);

                userFromDb.UserCertificateCollection.First().SetField("changed_thumbprint", m => m.Thumbprint);
                userFromDb.ClaimCollection.First().SetField("changed_type", m => m.Type);
                userFromDb.LinkedAccountCollection.First().SetField("changed_name", m => m.ProviderName);
                userFromDb.LinkedAccountClaimCollection.First().SetField("changed_value", m => m.Value);
                userFromDb.TwoFactorAuthTokenCollection.First().SetField("changed_token", m => m.Token);
                userFromDb.PasswordResetSecretCollection.First().SetField("changed_answer", m => m.Answer);

                repo.Update(userFromDb);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);

                Assert.Equal("changed_thumbprint", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("changed_type", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("changed_name", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("changed_value", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("changed_token", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("changed_answer", userFromDb.PasswordResetSecretCollection.First().Answer);
            }
        }

        [Fact]
        public void CanRemoveUserChild() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false, true);

                Assert.Equal("thumbprint1", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("type1", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("name1", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("value1", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("token1", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("answer1", userFromDb.PasswordResetSecretCollection.First().Answer);

                userFromDb.CallMethod("RemoveCertificate", new object[] { userFromDb.UserCertificateCollection.First() });
                userFromDb.CallMethod("RemoveClaim", new object[] { userFromDb.ClaimCollection.First() });
                userFromDb.CallMethod("RemoveLinkedAccount", new object[] { userFromDb.LinkedAccountCollection.First() });
                userFromDb.CallMethod("RemoveLinkedAccountClaim", new object[] { userFromDb.LinkedAccountClaimCollection.First() });
                userFromDb.CallMethod("RemoveTwoFactorAuthToken", new object[] { userFromDb.TwoFactorAuthTokenCollection.First() });
                userFromDb.CallMethod("RemovePasswordResetSecret", new object[] { userFromDb.PasswordResetSecretCollection.First() });

                repo.Update(userFromDb);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 1);
                AssertClaimCount(conn, 1);
                AssertLinkedCount(conn, 1);
                AssertLinkedClaimsCount(conn, 1);
                AssertTwoFactorCount(conn, 1);
                AssertResetSecretCount(conn, 1);

                userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);

                Assert.Equal("thumbprint2", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("type2", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("name2", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("value2", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("token2", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("answer2", userFromDb.PasswordResetSecretCollection.First().Answer);
            }
        }

        [Fact]
        public void CanUpdateAndRemoveUserChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false, true);

                Assert.Equal("thumbprint1", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("type1", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("name1", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("value1", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("token1", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("answer1", userFromDb.PasswordResetSecretCollection.First().Answer);

                userFromDb.UserCertificateCollection.First().SetField("__________", m => m.Thumbprint);
                userFromDb.ClaimCollection.First().SetField("__________", m => m.Type);
                userFromDb.LinkedAccountCollection.First().SetField("__________", m => m.ProviderName);
                userFromDb.LinkedAccountClaimCollection.First().SetField("__________", m => m.Value);
                userFromDb.TwoFactorAuthTokenCollection.First().SetField("__________", m => m.Token);
                userFromDb.PasswordResetSecretCollection.First().SetField("__________", m => m.Answer);

                Assert.Equal("__________", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("__________", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("__________", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("__________", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("__________", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("__________", userFromDb.PasswordResetSecretCollection.First().Answer);

                userFromDb.UserCertificateCollection.ElementAt(1).SetField("changed_thumbprint", m => m.Thumbprint);
                userFromDb.ClaimCollection.ElementAt(1).SetField("changed_type", m => m.Type);
                userFromDb.LinkedAccountCollection.ElementAt(1).SetField("changed_name", m => m.ProviderName);
                userFromDb.LinkedAccountClaimCollection.ElementAt(1).SetField("changed_value", m => m.Value);
                userFromDb.TwoFactorAuthTokenCollection.ElementAt(1).SetField("changed_token", m => m.Token);
                userFromDb.PasswordResetSecretCollection.ElementAt(1).SetField("changed_answer", m => m.Answer);

                var certKey = userFromDb.UserCertificateCollection.ElementAt(1).Key;
                var claimKey = userFromDb.ClaimCollection.ElementAt(1).Key;
                var linkedKey = userFromDb.LinkedAccountCollection.ElementAt(1).Key;
                var linkedClaimKey = userFromDb.LinkedAccountClaimCollection.ElementAt(1).Key;
                var twoFactorKey = userFromDb.TwoFactorAuthTokenCollection.ElementAt(1).Key;
                var resetKey = userFromDb.PasswordResetSecretCollection.ElementAt(1).Key;

                userFromDb.CallMethod("RemoveCertificate", new object[] { userFromDb.UserCertificateCollection.First() });
                userFromDb.CallMethod("RemoveClaim", new object[] { userFromDb.ClaimCollection.First() });
                userFromDb.CallMethod("RemoveLinkedAccount", new object[] { userFromDb.LinkedAccountCollection.First() });
                userFromDb.CallMethod("RemoveLinkedAccountClaim", new object[] { userFromDb.LinkedAccountClaimCollection.First() });
                userFromDb.CallMethod("RemoveTwoFactorAuthToken", new object[] { userFromDb.TwoFactorAuthTokenCollection.First() });
                userFromDb.CallMethod("RemovePasswordResetSecret", new object[] { userFromDb.PasswordResetSecretCollection.First() });

                {
                    var child3 = new UserCertificate()
                        .SetField("subject3", m => m.Subject)
                        .SetField("thumbprint3", m => m.Thumbprint);
                    userFromDb.CallMethod("AddCertificate", new object[] { child3 });
                }

                {
                    var child3 = new UserClaim("type3", "value3");
                    userFromDb.CallMethod("AddClaim", new object[] { child3 });
                }

                {
                    var child3 = new LinkedAccount()
                        .SetField("name3", m => m.ProviderName)
                        .SetField("id3", m => m.ProviderAccountID)
                        .SetField(DateTime.Now, m => m.LastLogin);
                    userFromDb.CallMethod("AddLinkedAccount", new object[] { child3 });
                }

                {
                    var child3 = new LinkedAccountClaim()
                        .SetField("name3", m => m.ProviderName)
                        .SetField("id3", m => m.ProviderAccountID)
                        .SetField("type3", m => m.Type)
                        .SetField("value3", m => m.Value);
                    userFromDb.CallMethod("AddLinkedAccountClaim", new object[] { child3 });
                }

                {
                    var child3 = new TwoFactorAuthToken()
                        .SetField("token3", m => m.Token)
                        .SetField(DateTime.Now, m => m.Issued);
                    userFromDb.CallMethod("AddTwoFactorAuthToken", new object[] { child3 });
                }

                {
                    var child3 = new PasswordResetSecret()
                        .SetField(Guid.NewGuid(), m => m.PasswordResetSecretID)
                        .SetField("question3", m => m.Question)
                        .SetField("answer3", m => m.Answer);
                    userFromDb.CallMethod("AddPasswordResetSecret", new object[] { child3 });
                }

                repo.Update(userFromDb);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                Assert.Equal(2, userFromDb.UserCertificateCollection.Count);
                Assert.Equal(2, userFromDb.ClaimCollection.Count);
                Assert.Equal(2, userFromDb.LinkedAccountCollection.Count);
                Assert.Equal(2, userFromDb.LinkedAccountClaimCollection.Count);
                Assert.Equal(2, userFromDb.TwoFactorAuthTokenCollection.Count);
                Assert.Equal(2, userFromDb.PasswordResetSecretCollection.Count);

                Assert.Equal("changed_thumbprint", userFromDb.UserCertificateCollection.First().Thumbprint);
                Assert.Equal("changed_type", userFromDb.ClaimCollection.First().Type);
                Assert.Equal("changed_name", userFromDb.LinkedAccountCollection.First().ProviderName);
                Assert.Equal("changed_value", userFromDb.LinkedAccountClaimCollection.First().Value);
                Assert.Equal("changed_token", userFromDb.TwoFactorAuthTokenCollection.First().Token);
                Assert.Equal("changed_answer", userFromDb.PasswordResetSecretCollection.First().Answer);

                Assert.Equal(certKey, userFromDb.UserCertificateCollection.First().Key);
                Assert.Equal(claimKey, userFromDb.ClaimCollection.First().Key);
                Assert.Equal(linkedKey, userFromDb.LinkedAccountCollection.First().Key);
                Assert.Equal(linkedClaimKey, userFromDb.LinkedAccountClaimCollection.First().Key);
                Assert.Equal(twoFactorKey, userFromDb.TwoFactorAuthTokenCollection.First().Key);
                Assert.Equal(resetKey, userFromDb.PasswordResetSecretCollection.First().Key);

                Assert.Equal("thumbprint3", userFromDb.UserCertificateCollection.ElementAt(1).Thumbprint);
                Assert.Equal("type3", userFromDb.ClaimCollection.ElementAt(1).Type);
                Assert.Equal("name3", userFromDb.LinkedAccountCollection.ElementAt(1).ProviderName);
                Assert.Equal("value3", userFromDb.LinkedAccountClaimCollection.ElementAt(1).Value);
                Assert.Equal("token3", userFromDb.TwoFactorAuthTokenCollection.ElementAt(1).Token);
                Assert.Equal("answer3", userFromDb.PasswordResetSecretCollection.ElementAt(1).Answer);
            }
        }

        [Fact]
        public void DeletingNullRecordThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("item", () => repo.Remove(null));
            }
        }

        [Fact]
        public void CanDeleteUser() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo, false);

                repo.Add(user);
                AssertUserCount(conn, 1);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);
                AssertUsersEqual(user, userFromDb, false);

                repo.Remove(userFromDb);

                AssertUserCount(conn, 0);
                userFromDb = repo.GetByID(user.ID);
                Assert.Null(userFromDb);
            }
        }

        [Fact]
        public void DeletingUserDeletesChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var user = CreateUser(repo);

                repo.Add(user);
                AssertUserCount(conn, 1);
                AssertCertCount(conn, 2);
                AssertClaimCount(conn, 2);
                AssertLinkedCount(conn, 2);
                AssertLinkedClaimsCount(conn, 2);
                AssertTwoFactorCount(conn, 2);
                AssertResetSecretCount(conn, 2);

                var userFromDb = repo.GetByID(user.ID);
                AssertUserIsValid(userFromDb);

                repo.Remove(userFromDb);

                AssertUserCount(conn, 0);
                AssertCertCount(conn, 0);
                AssertClaimCount(conn, 0);
                AssertLinkedCount(conn, 0);
                AssertLinkedClaimsCount(conn, 0);
                AssertTwoFactorCount(conn, 0);
                AssertResetSecretCount(conn, 0);
            }
        }

        #endregion Tests

        #region Extended Classes

        public class ExtendedUserAccount : RelationalUserAccount {
            public string OtherField { get; set; }
        }

        #endregion Extended Classes
    }
}
