using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Relational;
using System;
using System.Data;

namespace MembershipReboot.Dapper {
    /// <summary>
    /// A default implementation of <see cref="DapperUserAccountRepository{TAccount}"/>
    /// that uses <see cref="RelationalUserAccount"/>.
    /// </summary>
    public class DefaultUserAccountRepository
        : DapperUserAccountRepository<RelationalUserAccount>, IUserAccountRepository {

        /// <summary>
        /// Construct a new instance of the <see cref="DefaultUserAccountRepository"/>. The default values
        /// are used for the table names.
        /// </summary>
        /// <param name="connection">The connection to the database. If the connection is not open, an attempt is made to open it.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="connection"/> fails to open.
        /// </exception>
        public DefaultUserAccountRepository(IDbConnection connection)
            : base(connection) { }

        /// <summary>
        /// Creates an instace of a <see cref="UserAccount"/> by calling <see cref="Activator.CreateInstance{T}"/>
        /// </summary>
        /// <returns>The new <see cref="UserAccount"/></returns>
        UserAccount IUserAccountRepository<UserAccount>.Create() {
            return base.Create();
        }

        /// <summary>
        /// Gets a user by the provided id.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>A user with the provided id, or null.</returns>
        UserAccount IUserAccountRepository<UserAccount>.GetByID(Guid id) {
            return base.GetByID(id);
        }

        /// <summary>
        /// Gets a user by matching the provided username.
        /// </summary>
        /// <param name="username">The username to look for.</param>
        /// <returns>A user with the provided username, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="username"/> is null or whitespace
        /// </exception>
        UserAccount IUserAccountRepository<UserAccount>.GetByUsername(string username) {
            return base.GetByUsername(username);
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
        UserAccount IUserAccountRepository<UserAccount>.GetByUsername(string tenant, string username) {
            return base.GetByUsername(tenant, username);
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
        UserAccount IUserAccountRepository<UserAccount>.GetByEmail(string tenant, string email) {
            return base.GetByEmail(tenant, email);
        }

        /// <summary>
        /// Gets a user by matching the provided verification key.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>A user with the provided key, or null.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is null or whitespace
        /// </exception>
        UserAccount IUserAccountRepository<UserAccount>.GetByVerificationKey(string key) {
            return base.GetByVerificationKey(key);
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
        UserAccount IUserAccountRepository<UserAccount>.GetByCertificate(string tenant, string thumbprint) {
            return base.GetByCertificate(tenant, thumbprint);
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
        UserAccount IUserAccountRepository<UserAccount>.GetByLinkedAccount(string tenant, string provider, string id) {
            return base.GetByLinkedAccount(tenant, provider, id);
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
        UserAccount IUserAccountRepository<UserAccount>.GetByMobilePhone(string tenant, string phone) {
            return base.GetByMobilePhone(tenant, phone);
        }

        /// <summary>
        /// Add the user and any provided children to the database.
        /// </summary>
        /// <param name="item">The user.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public void Add(UserAccount item) {
            base.Add((RelationalUserAccount)item);
        }

        /// <summary>
        /// Updates the provided user in the database. Any children in the database that no longer
        /// exist in the user's child collections will be deleted. Any previously existing children,
        /// will be updated, and any new children will be added.
        /// </summary>
        /// <param name="item">The user to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public void Update(UserAccount item) {
            base.Update((RelationalUserAccount)item);
        }

        /// <summary>
        /// Removes the provided user from the database, along with all of its children.
        /// </summary>
        /// <param name="item">The user to remove.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
        public void Remove(UserAccount item) {
            base.Remove((RelationalUserAccount)item);
        }
    }
}
