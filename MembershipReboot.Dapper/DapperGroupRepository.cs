using BrockAllen.MembershipReboot;
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
    /// The repository used by the group service.
    /// </summary>
    /// <typeparam name="TGroup">The type of group.</typeparam>
    public class DapperGroupRepository<TGroup> : IGroupRepository<TGroup>
        where TGroup : RelationalGroup {

        #region Public Properties

        /// <summary>
        /// The name of the table that represents Groups. Default from constructor is "Groups".
        /// </summary>
        public virtual string GroupTable { get; set; }

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

        #endregion Helper Methods

        #region Constructors

        /// <summary>
        /// Construct a new instance of the <see cref="DapperGroupRepository{TGroup}"/>
        /// </summary>
        /// <param name="connection">The connection to the database. If the connection is not open, an attempt is made to open it.</param>
        /// <param name="utilities">An instance of the <see cref="Utilities"/> class. Defaults to null, in which case a new instance is created.</param>
        /// <param name="groupTable">The name of the table that represents Groups. Default is "Groups".</param>
        /// <param name="tableNameMap">A dictionary mapping any custom types used to their corresponding table names.</param>
        /// <param name="keySelectorMap">A dictionary mapping any custom types used to a <see cref="PropertyInfo"/> object that can be used to retrieve the primary key.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> is null, or <paramref name="groupTable"/> is null or whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="connection"/> fails to open.
        /// </exception>
        public DapperGroupRepository(IDbConnection connection, Utilities utilities = null, string groupTable = "Groups",
            Dictionary<Type, string> tableNameMap = null, Dictionary<Type, PropertyInfo> keySelectorMap = null) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(groupTable)) throw new ArgumentNullException(nameof(groupTable));

            if (connection.State != ConnectionState.Open) {
                if (connection.State == ConnectionState.Broken) {
                    connection.Close();
                }
                connection.Open();
            }

            if (connection.State != ConnectionState.Open) throw new ArgumentException("Cannot open the connection", nameof(connection));

            Connection = connection;
            _utilities = utilities ?? new Utilities();

            GroupTable = _utilities.EscapeTableName(groupTable);

            // NOTE(tim): Ignore results, we just want to populate the cache.
            _utilities.GetTypeProperties<TGroup>();
            _utilities.GetChildCollectionProperties<TGroup>();
            _utilities.GetTypeProperties<RelationalGroupChild>();

            TableNameMap = tableNameMap ?? new Dictionary<Type, string>();
            foreach (var kv in TableNameMap.ToList()) {
                if (string.IsNullOrWhiteSpace(kv.Value)) {
                    throw new Exception($"The table name specified for {kv.Key.Name} is invalid.");
                }
                TableNameMap[kv.Key] = _utilities.EscapeTableName(kv.Value);
            }

            if (!TableNameMap.ContainsKey(typeof(RelationalGroupChild))) { TableNameMap[typeof(RelationalGroupChild)] = "GroupChilds"; }

            KeySelectorMap = keySelectorMap ?? new Dictionary<Type, PropertyInfo>();

            if (!KeySelectorMap.ContainsKey(typeof(RelationalGroupChild))) { KeySelectorMap[typeof(RelationalGroupChild)] = typeof(RelationalGroupChild).GetProperty("Key"); }
        }

        #endregion Constructors

        #region Repository Methods

        /// <summary>
        /// Creates an instace of a <typeparamref name="TGroup"/> by calling <see cref="Activator.CreateInstance{T}"/>.
        /// <para>
        /// NOTE(tim): If you override <see cref="Utilities.IsChildCollectionProperty(PropertyInfo)"/> to return something
        /// other than <see cref="ICollection{T}"/>, then you will need to override this to correctly create this child
        /// properties.
        /// </para>
        /// </summary>
        /// <returns>The new <typeparamref name="TGroup"/></returns>
        public virtual TGroup Create() {
            var group = Activator.CreateInstance<TGroup>();

            // NOTE(tim): The default behavior of GetChildCollectionProperties is to return
            // any property that is an instance of ICollection<T>, so we create lists
            // with the same generic argument.
            var childProperties = _utilities.GetChildCollectionProperties<TGroup>();
            foreach (var prop in childProperties) {
                var genericArgs = prop.PropertyType.GetGenericArguments();
                var listType = typeof(List<>).MakeGenericType(genericArgs);
                prop.SetValue(group, Activator.CreateInstance(listType));
            }

            return group;
        }

        /// <summary>
        /// Gets an SQL statement that selects all children where `[ParentKey] = @primaryKey`.
        /// This will need to be overridden if child collections are not represented solely by
        /// instances of <see cref="ICollection{T}"/>.
        /// </summary>
        /// <returns>An SQL statement that retrieves all children.</returns>
        protected virtual string SelectChildren(bool multiple = false) {
            var childProps = _utilities.GetChildCollectionProperties<TGroup>();
            var builder = new StringBuilder();

            foreach (var prop in childProps) {
                var childType = prop.PropertyType.GetGenericArguments()[0];
                var tableName = GetTableName(childType);
                if (!multiple) {
                    builder.AppendLine($"select * from [dbo].[{tableName}] where [ParentKey] = @primaryKey;");
                } else {
                    builder.AppendLine($"select * from [dbo].[{tableName}] where [ParentKey] in (select [Key] from @primaryKeys);");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns a single group with its children. The group
        /// is identified by the specified where clause. The statement in the where
        /// clause can identify the current group by [Group].
        /// </summary>
        /// <param name="whereClause">The where clause to use to filter groups. The current group can be identified by [Group].</param>
        /// <param name="parameters">The parameters to pass to the query.</param>
        /// <returns>A single group matching the query, or null if none match.</returns>
        protected virtual TGroup SelectSingle(string whereClause, object parameters) {
            var sql =
$@"declare @primaryKey int = -1;
select @primaryKey = [Key] from [dbo].[{GroupTable}] as [Group] where {whereClause};
select * from [dbo].[{GroupTable}] where [Key] = @primaryKey;
{SelectChildren()}";

            using (var multi = Connection.QueryMultiple(sql, parameters)) {
                var group = multi.ReadSingleOrDefault<TGroup>();
                if (group == null) {
                    return group;
                }

                var childProps = _utilities.GetChildCollectionProperties<TGroup>();
                foreach (var prop in childProps) {
                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    var data = multi.Read(childType);
                    var listType = typeof(List<>).MakeGenericType(childType);
                    var list = (IList)Activator.CreateInstance(listType);
                    foreach (var item in data) {
                        list.Add(item);
                    }
                    prop.SetValue(group, list);
                }

                return group;
            }
        }

        /// <summary>
        /// Returns an enumerable of groups with their children. The applicable
        /// groups are identified throught the where clause. The statement in the where
        /// clause can identify the current group by [Group].
        /// </summary>
        /// <param name="whereClause">The where clause to use to filter groups. The current group can be identified by [Group].</param>
        /// <param name="parameters">The parameters to pass to the query.</param>
        /// <returns>An enumerable, possibly empty, containing all groups that match the where clause.</returns>
        protected virtual IEnumerable<TGroup> SelectMultiple(string whereClause, object parameters) {
            var sql =
$@"declare @primaryKeys table([Key] int);
insert into @primaryKeys select [Key] from [dbo].[{GroupTable}] as [Group] where {whereClause};
select * from [dbo].[{GroupTable}] where [Key] in (select [Key] from @primaryKeys);
{SelectChildren(true)}";

            using (var multi = Connection.QueryMultiple(sql, parameters)) {
                var groups = multi.Read<TGroup>();
                if (groups.Count() == 0) {
                    return groups;
                }

                var childProps = _utilities.GetChildCollectionProperties<TGroup>();
                foreach (var prop in childProps) {
                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    var parentKeyPropInfo = childType.GetProperty("ParentKey");
                    var data = multi.Read(childType);
                    var listType = typeof(List<>).MakeGenericType(childType);
                    var dictType = typeof(Dictionary<,>).MakeGenericType(parentKeyPropInfo.PropertyType, listType);
                    var childMap = (IDictionary)Activator.CreateInstance(dictType);

                    foreach (var item in data) {
                        var parentKey = parentKeyPropInfo.GetValue(item);
                        if (childMap[parentKey] == null) {
                            var list = (IList)Activator.CreateInstance(listType);
                            list.Add(item);
                            childMap.Add(parentKey, list);
                        } else {
                            ((IList)childMap[parentKey]).Add(item);
                        }
                    }

                    foreach (var group in groups) {
                        var children = childMap[group.Key];
                        if (children != null) {
                            prop.SetValue(group, children);
                        } else {
                            var list = Activator.CreateInstance(listType);
                            prop.SetValue(group, list);
                        }
                    }
                }

                return groups;
            }
        }

        /// <summary>
        /// Gets a group by the provided id.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>A group with the provided id, or null.</returns>
        public virtual TGroup GetByID(Guid id) {
            var where = "[ID] = @id";
            return SelectSingle(where, new { id = id });
        }

        /// <summary>
        /// Gets a group by matching the tenant and name.
        /// </summary>
        /// <param name="tenant">The tenant to match.</param>
        /// <param name="name">The name to match.</param>
        /// <returns>A group with the matching tenant and name, or null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> or <paramref name="name"/> are null or whitespace</exception>
        public virtual TGroup GetByName(string tenant, string name) {
            if (string.IsNullOrWhiteSpace(tenant)) throw new ArgumentNullException(nameof(tenant));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var where = "[Tenant] = @tenant and [Name] = @name";
            return SelectSingle(where, new { tenant = tenant, name = name });
        }

        /// <summary>
        /// Gets an enumerable of groups that match the provided ids.
        /// </summary>
        /// <param name="ids">An array of ids to match.</param>
        /// <returns>An enumerable of groups, or an empty enumerable if no ids match.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ids"/> is null.</exception>
        public virtual IEnumerable<TGroup> GetByIDs(Guid[] ids) {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var where = "[ID] in @ids";
            return SelectMultiple(where, new { ids = ids });
        }

        /// <summary>
        /// Returns all groups that have at least one child that matches <paramref name="childGroupID"/>
        /// </summary>
        /// <param name="childGroupID">The child group id to search for</param>
        /// <returns>An enumerable of groups that have matching children, or an empty enumerable.</returns>
        public virtual IEnumerable<TGroup> GetByChildID(Guid childGroupID) {
            var where = $"(select count(*) from [dbo].[{GetTableName(typeof(RelationalGroupChild))}] as [Child] where [Child].[ParentKey] = [Group].[Key] and [Child].[ChildGroupID] = @childGroupID) > 0";
            return SelectMultiple(where, new { childGroupID = childGroupID });
        }

        /// <summary>
        /// Adds a new group, and any provided children, to the database.
        /// </summary>
        /// <param name="item">The group.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public virtual void Add(TGroup item) {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var groupColumns = _utilities.GetColumnIdentifiers<TGroup>();
            var groupParams = _utilities.GetColumnParameters<TGroup>();

            using (var trx = new AutoDbTransaction(Connection)) {
                var sql = $"insert into [dbo].[{GroupTable}] ({groupColumns}) values ({groupParams}); select SCOPE_IDENTITY() as id;";

                int key = -1;
                using (var multi = Connection.QueryMultiple(sql, item, trx.Trx)) {
                    key = multi.ReadSingle<int>();
                }

                if (key <= 0) {
                    throw new Exception($"Received invalid identity key for new Group: {key}");
                }

                var childrenProps = _utilities.GetChildCollectionProperties<TGroup>();
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
            var sql = $"insert into [dbo].[{GetTableName(type)}] ([ParentKey], {childColumns}) values ({parentKey}, {childParams});";
            Connection.Execute(sql, collection, trx);
        }

        /// <summary>
        /// Updates the provided group in the database. Any children in the database that no longer
        /// exist in the group's child collection will be deleted. Any previously existing children,
        /// will be updated, and any new children will be added.
        /// </summary>
        /// <param name="item">The group to update.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public virtual void Update(TGroup item) {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var columnAssign = _utilities.GetColumnAssignment<TGroup>();

            using (var trx = new AutoDbTransaction(Connection)) {

                var sql = $"update [dbo].[{GroupTable}] set {columnAssign} where [Key] = @key;";
                Connection.Execute(sql, item, trx.Trx);

                var childrenProps = _utilities.GetChildCollectionProperties<TGroup>();
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

                var sql = $"delete from [dbo].[{tableName}] where [ParentKey] = @key;";
                Connection.Execute(sql, new { key = parentKey }, trx);
            } else {
                var type = FirstFromCollection(collection).GetType();
                var tableName = GetTableName(type);

                var sql = $"delete from [dbo].[{tableName}] where [ParentKey] = @key and [Key] not in @childKeys;";
                Connection.Execute(sql, new { key = parentKey, childKeys = GetChildKeys(collection) }, trx);

                var columns = _utilities.GetColumnIdentifiers(type);
                var parameters = _utilities.GetColumnParameters(type);
                var props = _utilities.GetTypePropertyNames(type);
                var insert = string.Join(", ", props.Select(s => $"[Source].[{s}]"));
                var update = string.Join(", ", props.Select(s => $"[Target].[{s}] = [Source].[{s}]"));

                sql =
$@"merge [dbo].[{tableName}] as [Target]
using (select @key, @parentKey, {parameters}) as [Source] ([Key], [ParentKey], {columns})
on ([Target].[Key] = [Source].[Key])
when not matched then insert ([ParentKey], {columns}) values ([Source].[ParentKey], {insert})
when matched then update set [Target].[ParentKey] = [Source].[ParentKey], {update};";
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
            var childProps = _utilities.GetChildCollectionProperties<TGroup>();
            var builder = new StringBuilder();

            foreach (var prop in childProps) {
                var childType = prop.PropertyType.GetGenericArguments()[0];
                var tableName = GetTableName(childType);
                builder.AppendLine($"delete from [dbo].[{tableName}] where [ParentKey] = @key;");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Removes the provided group from the database, along with all of its children.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public virtual void Remove(TGroup item) {
            if (item == null) throw new ArgumentNullException(nameof(item));

            using (var trx = new AutoDbTransaction(Connection)) {
                var sql =
$@"{DeleteChildren()}
delete from [dbo].[{GroupTable}] where [Key] = @key;";

                Connection.Execute(sql, new { key = item.Key }, trx.Trx);

                trx.Commit();
            }
        }

        #endregion Repository Methods
    }
}
