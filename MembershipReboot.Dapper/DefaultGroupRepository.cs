using BrockAllen.MembershipReboot;
using System;
using System.Collections.Generic;
using System.Data;

namespace MembershipReboot.Dapper {
    /// <summary>
    /// A default implementation of <see cref="DapperGroupRepository{TGroup}"/>
    /// that uses <see cref="RelationalGroup"/>.
    /// </summary>
    public class DefaultGroupRepository
        : DapperGroupRepository<RelationalGroup>, IGroupRepository {

        /// <summary>
        /// Construct a new instance of the <see cref="DefaultGroupRepository"/>. The default values
        /// are used for the group and group child tables.
        /// </summary>
        /// <param name="connection">The connection to the database. If the connection is not open, an attempt is made to open it.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="connection"/> fails to open.
        /// </exception>
        public DefaultGroupRepository(IDbConnection connection)
            : base(connection) { }

        /// <summary>
        /// Adds a new group, and any provided children, to the database.
        /// </summary>
        /// <param name="item">The group.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public void Add(Group item) {
            base.Add((RelationalGroup)item);
        }

        /// <summary>
        /// Removes the provided group from the database, along with all of its children.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public void Remove(Group item) {
            base.Remove((RelationalGroup)item);
        }

        /// <summary>
        /// Updates the provided group in the database. Any children in the database that no longer
        /// exist in the group's child collection will be deleted. Any previously existing children,
        /// will be updated, and any new children will be added.
        /// </summary>
        /// <param name="item">The group to add.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is null</exception>
        public void Update(Group item) {
            base.Update((RelationalGroup)item);
        }

        /// <summary>
        /// Creates an instace of a <see cref="Group"/> by calling <see cref="Activator.CreateInstance{T}"/>
        /// </summary>
        /// <returns>The new <see cref="Group"/></returns>
        Group IGroupRepository<Group>.Create() {
            return base.Create();
        }

        /// <summary>
        /// Returns all groups that have at least one child that matches <paramref name="childGroupID"/>
        /// </summary>
        /// <param name="childGroupID">The child group id to search for</param>
        /// <returns>An enumerable of groups that have matching children, or an empty enumerable.</returns>
        IEnumerable<Group> IGroupRepository<Group>.GetByChildID(Guid childGroupID) {
            return base.GetByChildID(childGroupID);
        }

        /// <summary>
        /// Gets a group by the provided id.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>A group with the provided id, or null.</returns>
        Group IGroupRepository<Group>.GetByID(Guid id) {
            return base.GetByID(id);
        }

        /// <summary>
        /// Gets an enumerable of groups that match the provided ids.
        /// </summary>
        /// <param name="ids">An array of ids to match.</param>
        /// <returns>An enumerable of groups, or an empty enumerable if no ids match.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ids"/> is null.</exception>
        IEnumerable<Group> IGroupRepository<Group>.GetByIDs(Guid[] ids) {
            return base.GetByIDs(ids);
        }

        /// <summary>
        /// Gets a group by matching the tenant and name.
        /// </summary>
        /// <param name="tenant">The tenant to match.</param>
        /// <param name="name">The name to match.</param>
        /// <returns>A group with the matching tenant and name, or null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> or <paramref name="name"/> are null or whitespace</exception>
        Group IGroupRepository<Group>.GetByName(string tenant, string name) {
            return base.GetByName(tenant, name);
        }
    }
}
