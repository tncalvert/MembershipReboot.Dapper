using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Relational;
using Dapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MembershipReboot.Dapper {
    /// <summary>
    /// The repository used by the user service.
    /// </summary>
    /// <typeparam name="TAccount">The type of user account.</typeparam>
    public class DapperUserAccountRepository<TAccount> : IUserAccountRepository<TAccount>
        where TAccount : RelationalUserAccount {

        #region Public Properties

        /// <summary>
        /// The schema for the tables. Default from constructor is "dbo".
        /// </summary>
        public virtual string Schema { get; set; }

        /// <summary>
        /// The name of the table that represents User Accounts. Default from constructor is "UserAccounts".
        /// </summary>
        public virtual string UserAccountTable { get; set; }

        /// <summary>
        /// A mapping from types to the corresponding table names. Used to lookup the appropriate table names for children.
        /// </summary>
        public virtual Dictionary<Type, string> TableNameMap { get; set; }

        /// <summary>
        /// A mapping from types to their primary key property.
        /// </summary>
        public virtual Dictionary<Type, PropertyInfo> KeySelectorMap { get; set; }

        /// <summary>
        /// The connection used by the repository.
        /// </summary>
        public virtual IDbConnection Connection { get; protected set; }

        #endregion Public Properties

        #region Protected Properties

        /// <summary>
        /// The <see cref="Utilities"/> class used by the repository
        /// </summary>
        protected virtual Utilities _utilities { get; set; }

        /// <summary>
        /// A quoted version of the schema name.
        /// </summary>
        protected virtual string QSchema => Q(Schema);

        #endregion Protected Properties

        #region Helper Methods

        /// <summary>
        /// Retrieves the specified table name for the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The table name.</returns>
        protected virtual string GetTableName(Type type) {
            TableNameMap.TryGetValue(type, out string name);
            if (string.IsNullOrWhiteSpace(name)) {
                throw new Exception($"There is no table name specified for {type.Name} or it is invalid.");
            }
            return name;
        }

        /// <summary>
        /// Retrieves the specified key property for the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The key property.</returns>
        protected virtual PropertyInfo GetKeySelector(Type type) {
            KeySelectorMap.TryGetValue(type, out PropertyInfo prop);
            if (prop == null) {
                throw new Exception($"There is no key property specified for {type.Name} or it is invalid.");
            }
            return prop;
        }

        private object FirstFromCollection(ICollection collection) {
            foreach (var o in collection)
                return o;
            return null;
        }

        /// <summary>
        /// Quotes a string for use in a SQL statement by using <see cref="Utilities.QuoteIdentifier(string)"/>.
        /// </summary>
        /// <param name="str">The string to quote.</param>
        /// <returns>The quoted string.</returns>
        private string Q(string str) {
            return _utilities.QuoteIdentifier(str);
        }

        #endregion Helper Methods

        #region Constructors

        /// <summary>
        /// Construct a new instance of the <see cref="DapperUserAccountRepository{TAccount}"/>
        /// </summary>
        /// <param name="connection">The connection to the database. If the connection is not open, an attempt is made to open it.</param>
        /// <param name="utilities">An instance of the <see cref="Utilities"/> class. Defaults to null, in which case a new instance is created.</param>
        /// <param name="schema">The schema used for the tables. Default is "dbo".</param>
        /// <param name="userAccountTable">The name of the table that represents User Accounts. Default is "UserAccounts".</param>
        /// <param name="tableNameMap">A dictionary mapping any custom types used to their corresponding table names.</param>
        /// <param name="keySelectorMap">A dictionary mapping any custom types used to a <see cref="PropertyInfo"/> object that can be used to retrieve the primary key.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> is null, or any table name parameter is null or whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="connection"/> fails to open.
        /// </exception>
        public DapperUserAccountRepository(IDbConnection connection, Utilities utilities = null, string schema = "dbo", string userAccountTable = "UserAccounts",
            Dictionary<Type, string> tableNameMap = null, Dictionary<Type, PropertyInfo> keySelectorMap = null) {

            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(userAccountTable)) throw new ArgumentNullException(nameof(userAccountTable));

            if (connection.State != ConnectionState.Open) {
                if (connection.State == ConnectionState.Broken) {
                    connection.Close();
                }
                connection.Open();
            }

            if (connection.State != ConnectionState.Open) throw new ArgumentException("Cannot open the connection", nameof(connection));

            Connection = connection;
            _utilities = utilities ?? new Utilities();

            Schema = _utilities.EscapeTableName(schema);
            UserAccountTable = _utilities.EscapeTableName(userAccountTable);

            // NOTE(tim): Ignore results, we just want to populate the cache.
            _utilities.GetTypeProperties<TAccount>();
            _utilities.GetChildCollectionProperties<TAccount>();
            _utilities.GetTypeProperties<RelationalUserCertificate>();
            _utilities.GetTypeProperties<RelationalUserClaim>();
            _utilities.GetTypeProperties<RelationalLinkedAccount>();
            _utilities.GetTypeProperties<RelationalLinkedAccountClaim>();
            _utilities.GetTypeProperties<RelationalTwoFactorAuthToken>();
            _utilities.GetTypeProperties<RelationalPasswordResetSecret>();

            TableNameMap = tableNameMap ?? new Dictionary<Type, string>();
            foreach (var kv in TableNameMap.ToList()) {
                if (string.IsNullOrWhiteSpace(kv.Value)) {
                    throw new Exception($"The table name specified for {kv.Key.Name} is invalid.");
                }
                TableNameMap[kv.Key] = _utilities.EscapeTableName(kv.Value);
            }

            if (!TableNameMap.ContainsKey(typeof(RelationalUserCertificate))) { TableNameMap[typeof(RelationalUserCertificate)] = "UserCertificates"; }
            if (!TableNameMap.ContainsKey(typeof(RelationalUserClaim))) { TableNameMap[typeof(RelationalUserClaim)] = "UserClaims"; }
            if (!TableNameMap.ContainsKey(typeof(RelationalLinkedAccount))) { TableNameMap[typeof(RelationalLinkedAccount)] = "LinkedAccounts"; }
            if (!TableNameMap.ContainsKey(typeof(RelationalLinkedAccountClaim))) { TableNameMap[typeof(RelationalLinkedAccountClaim)] = "LinkedAccountClaims"; }
            if (!TableNameMap.ContainsKey(typeof(RelationalTwoFactorAuthToken))) { TableNameMap[typeof(RelationalTwoFactorAuthToken)] = "TwoFactorAuthTokens"; }
            if (!TableNameMap.ContainsKey(typeof(RelationalPasswordResetSecret))) { TableNameMap[typeof(RelationalPasswordResetSecret)] = "PasswordResetSecrets"; }

            KeySelectorMap = keySelectorMap ?? new Dictionary<Type, PropertyInfo>();

            if (!KeySelectorMap.ContainsKey(typeof(RelationalUserCertificate))) { KeySelectorMap[typeof(RelationalUserCertificate)] = typeof(RelationalUserCertificate).GetProperty("Key"); }
            if (!KeySelectorMap.ContainsKey(typeof(RelationalUserClaim))) { KeySelectorMap[typeof(RelationalUserClaim)] = typeof(RelationalUserClaim).GetProperty("Key"); }
            if (!KeySelectorMap.ContainsKey(typeof(RelationalLinkedAccount))) { KeySelectorMap[typeof(RelationalLinkedAccount)] = typeof(RelationalLinkedAccount).GetProperty("Key"); }
            if (!KeySelectorMap.ContainsKey(typeof(RelationalLinkedAccountClaim))) { KeySelectorMap[typeof(RelationalLinkedAccountClaim)] = typeof(RelationalLinkedAccountClaim).GetProperty("Key"); }
            if (!KeySelectorMap.ContainsKey(typeof(RelationalTwoFactorAuthToken))) { KeySelectorMap[typeof(RelationalTwoFactorAuthToken)] = typeof(RelationalTwoFactorAuthToken).GetProperty("Key"); }
            if (!KeySelectorMap.ContainsKey(typeof(RelationalPasswordResetSecret))) { KeySelectorMap[typeof(RelationalPasswordResetSecret)] = typeof(RelationalPasswordResetSecret).GetProperty("Key"); }
        }

        #endregion Constructors

        #region Repository Methods

        /// <summary>
        /// Creates an instace of a <typeparamref name="TAccount"/> by calling <see cref="Activator.CreateInstance{T}"/>.
        /// <para>
        /// NOTE(tim): If you override <see cref="Utilities.IsChildCollectionProperty(PropertyInfo)"/> to return something
        /// other than <see cref="ICollection{T}"/>, then you will need to override this to correctly create this child
        /// properties.
        /// </para>
        /// </summary>
        /// <returns>The new <typeparamref name="TAccount"/></returns>
        public virtual TAccount Create() {
            var user = Activator.CreateInstance<TAccount>();

            // NOTE(tim): The default behavior of GetChildCollectionProperties is to return
            // any property that is an instance of ICollection<T>, so we create lists
            // with the same generic argument.
            var childProperties = _utilities.GetChildCollectionProperties<TAccount>();
            foreach (var prop in childProperties) {
                var genericArgs = prop.PropertyType.GetGenericArguments();
                var listType = typeof(List<>).MakeGenericType(genericArgs);
                prop.SetValue(user, Activator.CreateInstance(listType));
            }

            return user;
        }

        /// <summary>
        /// Gets an SQL statement that selects all children where `[ParentKey] = @primaryKey`.
        /// This will need to be overridden if child collections are not represented solely by
        /// instances of <see cref="ICollection{T}"/>.
        /// </summary>
        /// <returns>An SQL statement that retrieves all children.</returns>
        protected virtual string SelectChildren() {
            var childProps = _utilities.GetChildCollectionProperties<TAccount>();
            var builder = new StringBuilder();

            foreach (var prop in childProps) {
                var childType = prop.PropertyType.GetGenericArguments()[0];
                var tableName = GetTableName(childType);
                builder.AppendLine($"select * from {QSchema}.{Q(tableName)} where {Q("ParentKey")} = @primaryKey;");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns a single user with its children. The user
        /// is identified by the specified where clause. The statement in the where
        /// clause can identify the current group by [User].
        /// </summary>
        /// <param name="whereClause">The where clause to use to filter users. The current group can be identified by [User].</param>
        /// <param name="parameters">The parameters to pass to the query.</param>
        /// <returns>A single user matching the query, or null if none match.</returns>
        protected virtual TAccount SelectSingle(string whereClause, object parameters) {
            if (string.IsNullOrWhiteSpace(whereClause)) { throw new ArgumentNullException(nameof(whereClause)); }

            var sql =
$@"declare @primaryKey int = -1;
select @primaryKey = {Q("Key")} from {QSchema}.{Q(UserAccountTable)} as {Q("User")} where {whereClause};
select * from {QSchema}.{Q(UserAccountTable)} where {Q("Key")} = @primaryKey;
{SelectChildren()}";

            using (var multi = Connection.QueryMultiple(sql, parameters)) {
                var user = multi.ReadSingleOrDefault<TAccount>();
                if (user == null) {
                    return user;
                }

                var childProps = _utilities.GetChildCollectionProperties<TAccount>();
                foreach (var prop in childProps) {
                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    var data = multi.Read(childType);
                    var listType = typeof(List<>).MakeGenericType(childType);
                    var list = (IList)Activator.CreateInstance(listType);
                    foreach (var item in data) {
                        list.Add(item);
                    }
                    prop.SetValue(user, list);
                }

                return user;
            }
        }

        /// <summary>
        /// Gets a user by the provided id.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>A user with the provided id, or null.</returns>
        public virtual TAccount GetByID(Guid id) {
            var where = $"{Q("User")}.{Q("ID")} = @id";
            return SelectSingle(where, new { id = id });
        }

        /// <summary>
        /// Gets a user by matching the provided username.
        /// </summary>
        /// <param name="username">The username to look for.</param>
        /// <returns>A user with the provided username, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="username"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByUsername(string username) {
            if (string.IsNullOrWhiteSpace(username)) { throw new ArgumentNullException(nameof(username)); }

            var where = $"{Q("User")}.{Q("Username")} = @username";
            return SelectSingle(where, new { username = username });
        }

        /// <summary>
        /// Gets a user by matching the provided tenant and username.
        /// </summary>
        /// <param name="tenant">The tenant to look for.</param>
        /// <param name="username">The username to look for.</param>
        /// <returns>A user with the provided tenant and username, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tenant"/> or <paramref name="username"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByUsername(string tenant, string username) {
            if (string.IsNullOrWhiteSpace(tenant)) { throw new ArgumentNullException(nameof(tenant)); }
            if (string.IsNullOrWhiteSpace(username)) { throw new ArgumentNullException(nameof(username)); }

            var where = $"{Q("User")}.{Q("Tenant")} = @tenant and {Q("User")}.{Q("Username")} = @username";
            return SelectSingle(where, new { tenant = tenant, username = username });
        }

        /// <summary>
        /// Gets a user by matching the provided tenant and email.
        /// </summary>
        /// <param name="tenant">The tenant to look for.</param>
        /// <param name="email">The email to look for.</param>
        /// <returns>A user with the provided tenant and email, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tenant"/> or <paramref name="email"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByEmail(string tenant, string email) {
            if (string.IsNullOrWhiteSpace(tenant)) { throw new ArgumentNullException(nameof(tenant)); }
            if (string.IsNullOrWhiteSpace(email)) { throw new ArgumentNullException(nameof(email)); }

            var where = $"{Q("User")}.{Q("Tenant")} = @tenant and {Q("User")}.{Q("Email")} = @email";
            return SelectSingle(where, new { tenant = tenant, email = email });
        }

        /// <summary>
        /// Gets a user by matching the provided verification key.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>A user with the provided key, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByVerificationKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentNullException(nameof(key)); }

            var where = $"{Q("User")}.{Q("VerificationKey")} = @key";
            return SelectSingle(where, new { key = key });
        }

        /// <summary>
        /// Gets a user by matching the provided tenant and certificate thumbprint.
        /// </summary>
        /// <param name="tenant">The tenant to look for.</param>
        /// <param name="thumbprint">The thumbprint to look for.</param>
        /// <returns>A user with the provided tenant and thumbprint, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tenant"/> or <paramref name="thumbprint"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByCertificate(string tenant, string thumbprint) {
            if (string.IsNullOrWhiteSpace(tenant)) { throw new ArgumentNullException(nameof(tenant)); }
            if (string.IsNullOrWhiteSpace(thumbprint)) { throw new ArgumentNullException(nameof(thumbprint)); }

            var where =
$@"{Q("User")}.{Q("Tenant")} = @tenant
  and (select count(*)
       from {QSchema}.{Q(GetTableName(typeof(RelationalUserCertificate)))} as {Q("InnerCert")}
       where {Q("InnerCert")}.{Q("ParentKey")} = {Q("User")}.{Q("Key")}
         and {Q("InnerCert")}.{Q("Thumbprint")} = @thumbprint) > 0";

            return SelectSingle(where, new { tenant = tenant, thumbprint = thumbprint });
        }

        /// <summary>
        /// Gets a user by matching the provided tenant and provider/id on a linked account.
        /// </summary>
        /// <param name="tenant">The tenant to look for.</param>
        /// <param name="provider">The provider to look for.</param>
        /// <param name="id">The id to look for</param>
        /// <returns>A user with the provided tenant, provider and id, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tenant"/>, <paramref name="provider"/> or <paramref name="id"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByLinkedAccount(string tenant, string provider, string id) {
            if (string.IsNullOrWhiteSpace(tenant)) { throw new ArgumentNullException(nameof(tenant)); }
            if (string.IsNullOrWhiteSpace(provider)) { throw new ArgumentNullException(nameof(provider)); }
            if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

            var where =
$@"{Q("User")}.{Q("Tenant")} = @tenant
  and (select count(*)
       from {QSchema}.{Q(GetTableName(typeof(RelationalLinkedAccount)))} as {Q("InnerLinked")}
       where {Q("InnerLinked")}.{Q("ParentKey")} = {Q("User")}.{Q("Key")}
         and {Q("InnerLinked")}.{Q("ProviderName")} = @provider
         and {Q("InnerLinked")}.{Q("ProviderAccountID")} = @id) > 0";

            return SelectSingle(where, new { tenant = tenant, provider = provider, id = id });
        }

        /// <summary>
        /// Gets a user by matching the provided tenant and mobile phone.
        /// </summary>
        /// <param name="tenant">The tenant to look for.</param>
        /// <param name="phone">The phone to look for.</param>
        /// <returns>A user with the provided tenant and phone, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="tenant"/> or <paramref name="phone"/> is null or whitespace
        /// </exception>
        public virtual TAccount GetByMobilePhone(string tenant, string phone) {
            if (string.IsNullOrWhiteSpace(tenant)) { throw new ArgumentNullException(nameof(tenant)); }
            if (string.IsNullOrWhiteSpace(phone)) { throw new ArgumentNullException(nameof(phone)); }

            var where = $"{Q("User")}.{Q("Tenant")} = @tenant and {Q("User")}.{Q("MobilePhoneNumber")} = @phone";

            return SelectSingle(where, new { tenant = tenant, phone = phone });
        }

        /// <summary>
        /// Add the user and any provided children to the database.
        /// </summary>
        /// <param name="item">The user.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public virtual void Add(TAccount item) {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            var userColumns = _utilities.GetColumnIdentifiers<TAccount>();
            var userParams = _utilities.GetColumnParameters<TAccount>();

            using (var trx = new AutoDbTransaction(Connection)) {
                var sql = $"insert into {QSchema}.{Q(UserAccountTable)} ({userColumns}) values ({userParams}); select SCOPE_IDENTITY() as id;";

                int key = -1;
                using (var multi = Connection.QueryMultiple(sql, item, trx.Trx)) {
                    key = multi.ReadSingle<int>();
                }

                if (key <= 0) {
                    throw new Exception($"Received invalid identity key for new User: {key}");
                }

                var childrenProps = _utilities.GetChildCollectionProperties<TAccount>();
                foreach (var prop in childrenProps) {
                    var value = prop.GetValue(item);
                    AddChildren(value as ICollection, key, trx.Trx);
                }

                trx.Commit();
            }
        }

        /// <summary>
        /// Executes the SQL statements necessary to insert a collection of children. If
        /// the collection is null or has no entries, no SQL is executed.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="parentKey">The primary key value of the parent.</param>
        /// <param name="trx">The transaction that the statement will run under.</param>
        protected virtual void AddChildren(ICollection collection, int parentKey, IDbTransaction trx) {
            if (collection == null || collection.Count == 0)
                return;

            var type = FirstFromCollection(collection).GetType();

            var childColumns = _utilities.GetColumnIdentifiers(type);
            var childParams = _utilities.GetColumnParameters(type);
            var sql = $"insert into {QSchema}.{Q(GetTableName(type))} ({Q("ParentKey")}, {childColumns}) values ({parentKey}, {childParams});";
            Connection.Execute(sql, collection, trx);
        }

        /// <summary>
        /// Updates the provided user in the database. Any children in the database that no longer
        /// exist in the user's child collections will be deleted. Any previously existing children,
        /// will be updated, and any new children will be added.
        /// </summary>
        /// <param name="item">The user to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public virtual void Update(TAccount item) {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            var columnAssign = _utilities.GetColumnAssignment<TAccount>();

            using (var trx = new AutoDbTransaction(Connection)) {
                var sql = $"update {QSchema}.{Q(UserAccountTable)} set {columnAssign} where {Q("Key")} = @key;";
                Connection.Execute(sql, item, trx.Trx);

                var childrenProps = _utilities.GetChildCollectionProperties<TAccount>();
                foreach (var prop in childrenProps) {
                    var value = prop.GetValue(item);
                    UpdateChildren(value as ICollection, item.Key, trx.Trx);
                }

                trx.Commit();
            }
        }

        /// <summary>
        /// Executes the SQL statements to update a child collection. If the collection is null or empty,
        /// all associated children of this type will be deleted. If the collection is not empty,
        /// then any children in the database that no longer exist in the collection are delete,
        /// any existing chidren are updated and new children are inserted.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="parentKey">The primary key value of the parent.</param>
        /// <param name="trx">The transaction that the statement will run under.</param>
        protected virtual void UpdateChildren(ICollection collection, int parentKey, IDbTransaction trx) {
            if (collection == null || collection.Count == 0) {
                var colType = collection.GetType();
                if (!colType.IsGenericType) {
                    throw new Exception($"The provided collection was not generic, so the correct table name cannot be determined.");
                }
                var genType = colType.GetGenericArguments()[0];
                var tableName = GetTableName(genType);

                var sql = $"delete from {QSchema}.{Q(tableName)} where {Q("ParentKey")} = @key;";
                Connection.Execute(sql, new { key = parentKey }, trx);
            } else {
                var type = FirstFromCollection(collection).GetType();
                var tableName = GetTableName(type);

                var sql = $"delete from {QSchema}.{Q(tableName)} where {Q("ParentKey")} = @key and {Q("Key")} not in @childKeys;";
                Connection.Execute(sql, new { key = parentKey, childKeys = GetChildKeys(collection) }, trx);

                var columns = _utilities.GetColumnIdentifiers(type);
                var parameters = _utilities.GetColumnParameters(type);
                var props = _utilities.GetTypePropertyNames(type);
                var insert = string.Join(", ", props.Select(s => $"{Q("Source")}.{Q(s)}"));
                var update = string.Join(", ", props.Select(s => $"{Q("Target")}.{Q(s)} = {Q("Source")}.{Q(s)}"));

                sql =
$@"merge {QSchema}.{Q(tableName)} as {Q("Target")}
using (select @key, @parentKey, {parameters}) as {Q("Source")} ({Q("Key")}, {Q("ParentKey")}, {columns})
on ({Q("Target")}.{Q("Key")} = {Q("Source")}.{Q("Key")})
when not matched then insert ({Q("ParentKey")}, {columns}) values ({Q("Source")}.{Q("ParentKey")}, {insert})
when matched then update set {Q("Target")}.{Q("ParentKey")} = {Q("Source")}.{Q("ParentKey")}, {update};";
                Connection.Execute(sql, collection, trx);
            }
        }

        private IEnumerable<object> GetChildKeys(ICollection col) {
            var keys = new List<object>();
            var type = FirstFromCollection(col).GetType();
            var selector = GetKeySelector(type);

            foreach (var obj in col) {
                keys.Add(selector.GetValue(obj));
            }

            return keys;
        }

        /// <summary>
        /// Gets an SQL statement that deletes all children where `[ParentKey] = @key`.
        /// This will need to be overridden if child collections are not represented solely by
        /// instances of <see cref="ICollection{T}"/>.
        /// </summary>
        /// <returns>An SQL statement that retrieves all children.</returns>
        protected virtual string DeleteChildren() {
            var childProps = _utilities.GetChildCollectionProperties<TAccount>();
            var builder = new StringBuilder();

            foreach (var prop in childProps) {
                var childType = prop.PropertyType.GetGenericArguments()[0];
                var tableName = GetTableName(childType);
                builder.AppendLine($"delete from {QSchema}.{Q(tableName)} where {Q("ParentKey")} = @key;");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Removes the provided user from the database, along with all of its children.
        /// </summary>
        /// <param name="item">The user to remove.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public virtual void Remove(TAccount item) {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            using (var trx = new AutoDbTransaction(Connection)) {
                var sql =
$@"{DeleteChildren()}
delete from {QSchema}.{Q(UserAccountTable)} where {Q("Key")} = @key;";
                Connection.Execute(sql, new { key = item.Key }, trx.Trx);

                trx.Commit();
            }
        }

        #endregion Repository Methods
    }
}
