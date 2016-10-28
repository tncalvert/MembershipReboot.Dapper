using System;
using System.Data;

namespace MembershipReboot.Dapper {
    /// <summary>
    /// A wrapper class that will automatically commit or rollback the transaction
    /// on disposal depending on its internal flag. The flag is changed by calling
    /// <see cref="Commit"/> and <see cref="Rollback"/>. The default is to
    /// rollback the transaction.
    /// </summary>
    public class AutoDbTransaction : IDisposable {
        private bool _commit = false;
        private IDbTransaction _trx;

        /// <summary>
        /// Construct a new <see cref="AutoDbTransaction"/>, specifying the transaction.
        /// </summary>
        /// <param name="trx">The transaction</param>
        public AutoDbTransaction(IDbTransaction trx) {
            if (trx == null) throw new ArgumentNullException(nameof(trx));

            _trx = trx;
        }

        /// <summary>
        /// Construct a new <see cref="AutoDbTransaction"/>, specifying the connection
        /// on which to begin a transaction.
        /// </summary>
        /// <param name="connection">The connection</param>
        public AutoDbTransaction(IDbConnection connection) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            _trx = connection.BeginTransaction();
        }

        /// <summary>
        /// Construct a new <see cref="AutoDbTransaction"/>, specifying the connection
        /// on which to begin a transaction, and the isolation level for the transaction.
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="isolationLevel">The isolation level</param>
        public AutoDbTransaction(IDbConnection connection, IsolationLevel isolationLevel) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            _trx = connection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// The internal transaction.
        /// </summary>
        public IDbTransaction Trx => _trx;

        /// <summary>
        /// Mark the <see cref="AutoDbTransaction"/> to commit the transaction
        /// when it is disposed of.
        /// </summary>
        public void Commit() { _commit = true; }

        /// <summary>
        /// Mark the <see cref="AutoDbTransaction"/> to rollback the transaction
        /// when it is disposed of.
        /// </summary>
        public void Rollback() { _commit = false; }

        #region IDisposable

        private bool _disposed = false;

        /// <summary>
        /// Dispose and commit or rollback the transaction
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    if (_trx != null) {
                        if (_commit) {
                            _trx.Commit();
                        } else {
                            _trx.Rollback();
                        }

                        _trx.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Dispose and commit or rollback the transaction
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }

        #endregion IDisposable
    }
}
