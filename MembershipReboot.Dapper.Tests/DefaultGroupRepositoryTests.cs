using BrockAllen.MembershipReboot;
using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace MembershipReboot.Dapper.Tests {
    public class DefaultGroupRepositoryTests : IDisposable, IClassFixture<CheckDatabase> {
        #region Fields

        public static string ConnectionString => @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=MembershipReboot;Integrated Security=True";

        #endregion Fields

        #region Constructor and Disposable

        public DefaultGroupRepositoryTests() { }

        public void Dispose() { }

        #endregion Constructor and Disposable

        #region Static

        private SqlConnection CreateConnection() => Helpers.CreateConnection(ConnectionString);
        private SqlConnection CreateClosedConnection() => Helpers.CreateClosedConnection(ConnectionString);
        private void ResetDatabase(SqlConnection connection) => Helpers.ResetDatabase(connection);
        private TObj SetField<TObj, TProp>(TObj obj, TProp value, Expression<Func<TObj, TProp>> propExpr) => Helpers.SetField(obj, value, propExpr);
        private void CallMethod<TObj>(TObj obj, string methodName, object[] parameters) => Helpers.CallMethod(obj, methodName, parameters);
        private LimitedPrecisionDateTimeComparer DateTimeComparer => Helpers.DateTimeComparer;

        private static DefaultGroupRepository CreateRepository(IDbConnection connection) {
            var repo = new DefaultGroupRepository(connection);
            return repo;
        }

        #endregion Static

        #region Helpers

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
                Assert.Equal("Groups", repo.GroupTable);
                Assert.Equal("GroupChilds", repo.TableNameMap[typeof(RelationalGroupChild)]);
            }
        }

        [Fact]
        public void CreateReturnsNonNullRelationalGroup() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);

                var newGroup = repo.Create();
                Assert.NotNull(newGroup);
                Assert.IsAssignableFrom<RelationalGroup>(newGroup);
                Assert.NotNull(newGroup.ChildrenCollection);
                Assert.NotNull(newGroup.Children);
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
        public void CanAddNewGroup() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("test name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                repo.Add(group);

                var count = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, count);

                var groupFromDb = conn.Query<RelationalGroup>("select * from Groups").FirstOrDefault();
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);
            }
        }

        [Fact]
        public void CanAddNewGroupWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("test name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);

                var groupChildCount = conn.Query<int>("select count(*) from GroupChilds").FirstOrDefault();
                Assert.Equal(2, groupChildCount);
            }
        }

        [Fact]
        public void ChildrenOfGroupHaveCorrectParentID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("test name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);

                var groupKey = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.NotEqual(0, groupKey);

                var groupChildCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = groupKey }).FirstOrDefault();
                Assert.Equal(2, groupChildCount);

                var parentKeys = conn.Query<int>("select [ParentKey] from GroupChilds");
                Assert.Equal(new int[] { groupKey, groupKey }, parentKeys);
            }
        }

        [Fact]
        public void NonExistantIDReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.GetByID(Guid.NewGuid());
                Assert.Null(group);
            }
        }

        [Fact]
        public void CanGetGroupByID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var group = repo.GetByID(id);
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
            }
        }

        [Fact]
        public void CanGetGroupByIDWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var key = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.True(key > 0);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });
                var childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).FirstOrDefault();
                Assert.Equal(2, childCount);

                var repo = CreateRepository(conn);
                var group = repo.GetByID(id);
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
                Assert.Equal(2, group.Children.Count());
            }
        }

        [Fact]
        public void GetByNameNullTenantThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByName(null, "name"));
            }
        }

        [Fact]
        public void GetByNameNullNameThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("name", () => repo.GetByName("tenant", null));
            }
        }

        [Fact]
        public void GetByNameEmptyTenantThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByName("", "name"));
            }
        }

        [Fact]
        public void GetByNameEmptyNameThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("name", () => repo.GetByName("tenant", ""));
            }
        }

        [Fact]
        public void GetByNameWhitespaceTenantThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("tenant", () => repo.GetByName("  ", "name"));
            }
        }

        [Fact]
        public void GetByNameWhitespaceNameThrows() {
            using (var conn = CreateConnection()) {
                var repo = CreateRepository(conn);
                Assert.Throws<ArgumentNullException>("name", () => repo.GetByName("tenant", " "));
            }
        }

        [Fact]
        public void GetByNameNonExistantTenantReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var group = repo.GetByName("wrong", "name");
                Assert.Null(group);
            }
        }

        [Fact]
        public void GetByNameNonExistantNameReturnsNull() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var group = repo.GetByName("default", "wrong");
                Assert.Null(group);
            }
        }

        [Fact]
        public void CanGetGroupByName() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var group = repo.GetByName("default", "name");
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
            }
        }

        [Fact]
        public void CanGetGroupByNameWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var key = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.True(key > 0);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });
                var childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).FirstOrDefault();
                Assert.Equal(2, childCount);

                var repo = CreateRepository(conn);
                var group = repo.GetByName("default", "name");
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
                Assert.Equal(2, group.Children.Count());
            }
        }

        [Fact]
        public void EmptyIDArrayReturnsNone() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(new Guid[0]);
                Assert.NotNull(groups);
                Assert.Empty(groups);
            }
        }

        [Fact]
        public void NonExistantIDsReturnNone() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(new Guid[] { Guid.NewGuid(), Guid.NewGuid() });
                Assert.NotNull(groups);
                Assert.Empty(groups);
            }
        }

        [Fact]
        public void CanGetGroupByMultipleIDs() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(new Guid[] { id });
                Assert.NotNull(groups);
                Assert.Single(groups);
                var group = groups.FirstOrDefault();
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
            }
        }

        [Fact]
        public void CanGetGroupByMultipleIDsWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(1, groupCount);
                var key = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.True(key > 0);
                var id = conn.Query<Guid>("select ID from Groups").FirstOrDefault();
                Assert.True(id != Guid.Empty);

                conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });
                var childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).FirstOrDefault();
                Assert.Equal(2, childCount);

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(new Guid[] { id });
                Assert.NotNull(groups);
                Assert.Single(groups);
                var group = groups.FirstOrDefault();
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.True(group.Key > 0);
                Assert.Equal(id, group.ID);
                Assert.Equal("default", group.Tenant);
                Assert.Equal("name", group.Name);
                Assert.Equal(2, group.Children.Count());
            }
        }

        [Fact]
        public void CanGetMultipleGroupsByMultipleIDs() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate()), (newid(), 'default', 'name2', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(2, groupCount);
                var ids = conn.Query<Guid>("select ID from Groups");
                Assert.NotNull(ids);

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(ids.ToArray());
                Assert.NotNull(groups);
                Assert.Equal(2, groups.Count());
            }
        }

        [Fact]
        public void CanGetMultipleGroupsByMultipleIDsWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate()), (newid(), 'default', 'name2', getdate(), getdate())");
                var groupCount = conn.Query<int>("select count(*) from Groups").FirstOrDefault();
                Assert.Equal(2, groupCount);
                var keys = conn.Query<int>("select [Key] from Groups");
                Assert.NotNull(keys);
                var ids = conn.Query<Guid>("select ID from Groups");
                Assert.NotNull(ids);

                foreach (var key in keys) {
                    conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });
                    var childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).FirstOrDefault();
                    Assert.Equal(2, childCount);
                }

                var repo = CreateRepository(conn);
                var groups = repo.GetByIDs(ids.ToArray());
                Assert.NotNull(groups);
                Assert.Equal(2, groups.Count());
                foreach (var group in groups) {
                    Assert.NotNull(group);
                    Assert.NotNull(group.Children);
                    Assert.True(group.Key > 0);
                    Assert.Equal(2, group.Children.Count());
                }
            }
        }

        [Fact]
        public void GroupWithoutChildrenNotReturnedByChildGroupID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var childGroupID = Guid.NewGuid();

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");

                var repo = CreateRepository(conn);
                var groups = repo.GetByChildID(childGroupID);
                Assert.NotNull(groups);
                Assert.Empty(groups);
            }
        }

        [Fact]
        public void GroupWithWrongChildrenNotReturnedByChildGroupID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var childGroupID = Guid.NewGuid();

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var key = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.True(key > 0);

                conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });

                var repo = CreateRepository(conn);
                var groups = repo.GetByChildID(childGroupID);
                Assert.NotNull(groups);
                Assert.Empty(groups);
            }
        }

        [Fact]
        public void GroupWithCorrectChildReturnedByChildGroupIDWithAllChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var childGroupID = Guid.NewGuid();

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate())");
                var key = conn.Query<int>("select [Key] from Groups").FirstOrDefault();
                Assert.True(key > 0);

                conn.Execute("insert into GroupChilds values (@key, @childGroupID), (@key, newid())", new { key = key, childGroupID = childGroupID });

                var repo = CreateRepository(conn);
                var groups = repo.GetByChildID(childGroupID);
                Assert.NotNull(groups);
                Assert.Single(groups);
                var group = groups.First();
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.Equal(2, group.Children.Count());
            }
        }

        [Fact]
        public void OnlyCorrectGroupsReturnedByChildGroupID() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var childGroupID = Guid.NewGuid();

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate()), (newid(), 'default', 'name2', getdate(), getdate())");
                var keys = conn.Query<int>("select [Key] from Groups");
                Assert.NotNull(keys);

                var i = 0;
                foreach (var key in keys) {
                    if (i == 0)
                        conn.Execute("insert into GroupChilds values (@key, @childGroupID), (@key, newid())", new { key = key, childGroupID = childGroupID });
                    else
                        conn.Execute("insert into GroupChilds values (@key, newid()), (@key, newid())", new { key = key });
                    ++i;
                }

                var repo = CreateRepository(conn);
                var groups = repo.GetByChildID(childGroupID);
                Assert.NotNull(groups);
                Assert.Single(groups);
                var group = groups.First();
                Assert.NotNull(group);
                Assert.NotNull(group.Children);
                Assert.Equal(2, group.Children.Count());
            }
        }

        [Fact]
        public void AllCorrectGroupsReturnedByChildGroupIDWithAllChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var childGroupID = Guid.NewGuid();

                conn.Execute("insert into Groups values (newid(), 'default', 'name', getdate(), getdate()), (newid(), 'default', 'name2', getdate(), getdate())");
                var keys = conn.Query<int>("select [Key] from Groups");
                Assert.NotNull(keys);

                foreach (var key in keys) {
                    conn.Execute("insert into GroupChilds values (@key, @childGroupID), (@key, newid())", new { key = key, childGroupID = childGroupID });
                }

                var repo = CreateRepository(conn);
                var groups = repo.GetByChildID(childGroupID);
                Assert.NotNull(groups);
                Assert.Equal(2, groups.Count());
                foreach (var group in groups) {
                    Assert.NotNull(group);
                    Assert.NotNull(group.Children);
                    Assert.Equal(2, group.Children.Count());
                }
            }
        }

        [Fact]
        public void CanRetrieveAddedGroup() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                foreach (var child in groupFromDb.Children)
                    Assert.NotNull(child);
            }
        }

        [Fact]
        public void CanRetrieveAddedGroupWithChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }
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
        public void CanUpdateGroup() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                groupFromDb.SetField("new name", m => m.Name);
                groupFromDb.SetField(groupFromDb.Created.AddDays(1), m => m.Created);
                groupFromDb.SetField(groupFromDb.LastUpdated.AddDays(1), m => m.LastUpdated);

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal("new name", groupFromDb.Name);
                Assert.NotEqual(group.Created, groupFromDb.Created);
                Assert.NotEqual(group.LastUpdated, groupFromDb.LastUpdated);
            }
        }

        [Fact]
        public void CanUpdateGroupWithNewChild() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }

                var child3 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                groupFromDb.CallMethod("AddChild", new object[] { child3 });

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(3, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }
            }
        }

        [Fact]
        public void CanUpdateGroupWithNewChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }

                var child3 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child4 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                groupFromDb.CallMethod("AddChild", new object[] { child3 });
                groupFromDb.CallMethod("AddChild", new object[] { child4 });

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(4, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }
            }
        }

        [Fact]
        public void CanUpdateGroupChild() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var initialID = Guid.NewGuid();
                var changedID = Guid.NewGuid();

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(initialID, m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Single(groupFromDb.Children);
                var child = (RelationalGroupChild)groupFromDb.Children.First();
                Assert.NotNull(child);
                Assert.Equal(groupFromDb.Key, child.ParentKey);
                Assert.Equal(initialID, child.ChildGroupID);
                var childKey = child.Key;

                child.SetField(changedID, m => m.ChildGroupID);

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);

                Assert.NotNull(groupFromDb.Children);
                Assert.Single(groupFromDb.Children);
                child = (RelationalGroupChild)groupFromDb.Children.First();
                Assert.NotNull(child);
                Assert.Equal(childKey, child.Key);
                Assert.Equal(groupFromDb.Key, child.ParentKey);
                Assert.Equal(changedID, child.ChildGroupID);
            }
        }

        [Fact]
        public void CanRemoveGroupChild() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }

                var firstChild = groupFromDb.Children.First();
                groupFromDb.CallMethod("RemoveChild", new object[] { firstChild });

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);

                Assert.NotNull(groupFromDb.Children);
                Assert.Single(groupFromDb.Children);
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }
            }
        }

        [Fact]
        public void CanAddUpdateAndRemoveGroupChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var initialID = Guid.NewGuid();
                var changedID = Guid.NewGuid();

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(initialID, m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                Assert.Equal(group.ID, groupFromDb.ID);
                Assert.Equal(group.Tenant, groupFromDb.Tenant);
                Assert.Equal(group.Name, groupFromDb.Name);
                Assert.Equal(group.Created, groupFromDb.Created, DateTimeComparer);
                Assert.Equal(group.LastUpdated, groupFromDb.LastUpdated, DateTimeComparer);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                }

                var firstChild = groupFromDb.Children.First();
                groupFromDb.CallMethod("RemoveChild", new object[] { firstChild });

                var secondChild = (RelationalGroupChild)groupFromDb.Children.First(); // other remove above
                secondChild.SetField(changedID, m => m.ChildGroupID);

                var child3 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                groupFromDb.CallMethod("AddChild", new object[] { child3 });

                repo.Update(groupFromDb);

                groupFromDb = repo.GetByID(groupFromDb.ID);
                Assert.NotNull(groupFromDb);

                Assert.NotNull(groupFromDb.Children);
                Assert.Equal(2, groupFromDb.Children.Count());
                foreach (var child in groupFromDb.ChildrenCollection) {
                    Assert.NotNull(child);
                    Assert.Equal(groupFromDb.Key, child.ParentKey);
                    if (child.Key == secondChild.Key) {
                        Assert.Equal(changedID, child.ChildGroupID);
                    }
                }
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
        public void CanDeleteGroup() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);

                repo.Remove(groupFromDb);

                groupFromDb = repo.GetByID(group.ID);
                Assert.Null(groupFromDb);
            }
        }

        [Fact]
        public void DeletingGroupDeletesChildren() {
            using (var conn = CreateConnection()) {
                ResetDatabase(conn);

                var repo = CreateRepository(conn);
                var group = repo.Create()
                     .SetField(Guid.NewGuid(), m => m.ID)
                     .SetField("default", m => m.Tenant)
                     .SetField("name", m => m.Name)
                     .SetField(DateTime.Now, m => m.Created)
                     .SetField(DateTime.Now, m => m.LastUpdated);

                var child1 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                var child2 = new GroupChild()
                    .SetField(Guid.NewGuid(), m => m.ChildGroupID);
                group.CallMethod("AddChild", new object[] { child1 });
                group.CallMethod("AddChild", new object[] { child2 });

                repo.Add(group);

                var groupFromDb = repo.GetByID(group.ID);
                Assert.NotNull(groupFromDb);
                var key = groupFromDb.Key;

                var childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).First();
                Assert.Equal(2, childCount);

                repo.Remove(groupFromDb);

                groupFromDb = repo.GetByID(group.ID);
                Assert.Null(groupFromDb);

                childCount = conn.Query<int>("select count(*) from GroupChilds where [ParentKey] = @key", new { key = key }).First();
                Assert.Equal(0, childCount);
            }
        }

        #endregion Tests
    }
}
