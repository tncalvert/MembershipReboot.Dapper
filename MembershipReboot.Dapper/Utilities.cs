using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MembershipReboot.Dapper {
    /// <summary>
    /// A class that contains helper methods that can be overridden by
    /// the user of the library.
    /// </summary>
    public class Utilities {
        /// <summary>
        /// A cache of properties per type
        /// </summary>
        protected static ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> _properties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();

        /// <summary>
        /// A cache of child collection properties per type
        /// </summary>
        protected static ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> _childCollectionProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();

        /// <summary>
        /// A cache of formatted columns per type. <see cref="GetColumnIdentifiers{T}"/> 
        /// </summary>
        protected static ConcurrentDictionary<RuntimeTypeHandle, string> _columnIdentifiers = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        /// <summary>
        /// A cache of formatted columns per type. <see cref="GetColumnParameters{T}"/> 
        /// </summary>
        protected static ConcurrentDictionary<RuntimeTypeHandle, string> _columnParameters = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        /// <summary>
        /// A cache of formatted columns per type. <see cref="GetColumnAssignment{T}"/> 
        /// </summary>
        protected static ConcurrentDictionary<RuntimeTypeHandle, string> _columnAssignment = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        /// <summary>
        /// Escapes a provided table name by duplicating any brackets ([,])
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <returns>The table name with any brackets duplicated.</returns>
        public virtual string EscapeTableName(string tableName) {
            if (string.IsNullOrWhiteSpace(tableName)) return tableName;
            return tableName.Replace("[", "[[").Replace("]", "]]");
        }

        /// <summary>
        /// Retrieves the column names formated as identifiers. E.g., `[Key], [Prop1], [Prop1]`
        /// </summary>
        public virtual string GetColumnIdentifiers<T>() {
            return GetColumnIdentifiers(typeof(T));
        }

        /// <summary>
        /// Retrieves the column names formated as identifiers. E.g., `[Key], [Prop1], [Prop1]`
        /// </summary>
        public virtual string GetColumnIdentifiers(Type t) {
            var typeHandle = t.TypeHandle;
            if (!_columnIdentifiers.TryGetValue(typeHandle, out string str)) {
                var properties = GetTypePropertyNames(t);
                str = string.Join(", ", properties.Select(s => $"{QuoteIdentifier(s)}"));
                _columnIdentifiers[typeHandle] = str;
            }
            return str;
        }

        /// <summary>
        /// Retrieves the column names formated as parameters. E.g., `@Key, @Prop1, @Prop2`
        /// </summary>
        public virtual string GetColumnParameters<T>() {
            return GetColumnParameters(typeof(T));
        }

        /// <summary>
        /// Retrieves the column names formated as parameters. E.g., `@Key, @Prop1, @Prop2`
        /// </summary>
        public virtual string GetColumnParameters(Type t) {
            var typeHandle = t.TypeHandle;
            if (!_columnParameters.TryGetValue(typeHandle, out string str)) {
                var properties = GetTypePropertyNames(t);
                str = string.Join(", ", properties.Select(s => $"@{s}"));
                _columnParameters[typeHandle] = str;
            }
            return str;
        }

        /// <summary>
        /// Retrieves the column names formated for assignment.
        /// E.g., `[Key] = @Key, [Prop1] = @Prop1, [Prop2] = @Prop2`
        /// </summary>
        public virtual string GetColumnAssignment<T>() {
            return GetColumnAssignment(typeof(T));
        }

        /// <summary>
        /// Retrieves the column names formated for assignment.
        /// E.g., `[Key] = @Key, [Prop1] = @Prop1, [Prop2] = @Prop2`
        /// </summary>
        public virtual string GetColumnAssignment(Type t) {
            var typeHandle = t.TypeHandle;
            if (!_columnAssignment.TryGetValue(typeHandle, out string str)) {
                var properties = GetTypePropertyNames(t);
                str = string.Join(", ", properties.Select(s => $"{QuoteIdentifier(s)} = @{s}"));
                _columnAssignment[typeHandle] = str;
            }
            return str;
        }

        /// <summary>
        /// Retrieves the properties of the type, with each property being tested by <see cref="IncludeProperty(PropertyInfo)"/>
        /// </summary>
        public virtual IEnumerable<PropertyInfo> GetTypeProperties<T>() {
            return GetTypeProperties(typeof(T));
        }

        /// <summary>
        /// Retrieves the properties of the type, with each property being tested by <see cref="IncludeProperty(PropertyInfo)"/>
        /// </summary>
        public virtual IEnumerable<PropertyInfo> GetTypeProperties(Type t) {
            var typeHandle = t.TypeHandle;
            IEnumerable<PropertyInfo> props = Enumerable.Empty<PropertyInfo>();
            if (!_properties.TryGetValue(typeHandle, out props)) {
                props = t.GetProperties().Where(w => IncludeProperty(w));
                _properties[typeHandle] = props;
            }
            return props;
        }

        /// <summary>
        /// Get an enumerable of the names of included properties
        /// by calling <see cref="GetTypeProperties{T}"/> and taking the <see cref="MemberInfo.Name"/>
        /// for each property.
        /// </summary>
        public IEnumerable<string> GetTypePropertyNames<T>() {
            var properties = GetTypeProperties<T>();
            return properties.Select(s => s.Name);
        }

        /// <summary>
        /// Get an enumerable of the names of included properties
        /// by calling <see cref="GetTypeProperties"/> and taking the <see cref="MemberInfo.Name"/>
        /// for each property.
        /// </summary>
        public IEnumerable<string> GetTypePropertyNames(Type t) {
            var properties = GetTypeProperties(t);
            return properties.Select(s => s.Name);
        }

        /// <summary>
        /// Retrieves the properties of the type that represent a collection of children per <see cref="IsChildCollectionProperty(PropertyInfo)"/>.
        /// </summary>
        public virtual IEnumerable<PropertyInfo> GetChildCollectionProperties<T>() {
            return GetChildCollectionProperties(typeof(T));
        }

        /// <summary>
        /// Retrieves the properties of the type that represent a collection of children per <see cref="IsChildCollectionProperty(PropertyInfo)"/>.
        /// </summary>
        public virtual IEnumerable<PropertyInfo> GetChildCollectionProperties(Type t) {
            var typeHandle = t.TypeHandle;
            IEnumerable<PropertyInfo> props = Enumerable.Empty<PropertyInfo>();
            if (!_childCollectionProperties.TryGetValue(typeHandle, out props)) {
                props = t.GetProperties().Where(w => IsChildCollectionProperty(w));
                _childCollectionProperties[typeHandle] = props;
            }
            return props;
        }

        /// <summary>
        /// Get an enumerable of the names of child collection properties
        /// by calling <see cref="GetChildCollectionProperties{T}"/> and taking the <see cref="MemberInfo.Name"/>
        /// for each property.
        /// </summary>
        public IEnumerable<string> GetChildCollectionPropertyNames<T>() {
            var properties = GetChildCollectionProperties<T>();
            return properties.Select(s => s.Name);
        }

        /// <summary>
        /// Get an enumerable of the names of child collection properties
        /// by calling <see cref="GetChildCollectionProperties"/> and taking the <see cref="MemberInfo.Name"/>
        /// for each property.
        /// </summary>
        public IEnumerable<string> GetChildCollectionPropertyNames(Type t) {
            var properties = GetChildCollectionProperties(t);
            return properties.Select(s => s.Name);
        }

        /// <summary>
        /// Determines if the specified property represents a collection of children that should
        /// be updated with the parent object.
        /// <para>
        /// The default behavior is any property that is an instance of <see cref="ICollection{T}"/>.
        /// </para>
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is a child collection, or false.</returns>
        protected virtual bool IsChildCollectionProperty(PropertyInfo property) {
            var type = property.PropertyType;
            var collectionType = typeof(ICollection<>);

            if (collectionType.Equals(type) || collectionType.IsAssignableFrom(type))
                return true;

            if (type.IsGenericType) {
                var genericType = type.GetGenericTypeDefinition();
                if (collectionType.Equals(genericType) || collectionType.IsAssignableFrom(genericType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the specified property should be included in the list of a given type's properties.
        /// <para>
        /// The default behavior is to check the property name against the <see cref="IgnoredNames"/> list
        /// and check the property type against the <see cref="IgnoredTypes"/> list. The type is
        /// checked for equality and via <see cref="Type.IsAssignableFrom(Type)"/>. If both the property
        /// type and the currently tested type are generic (<see cref="Type.IsGenericType"/>), the equality and assignable tests are repeated
        /// with the generic definitions obtained by calling <see cref="Type.GetGenericTypeDefinition"/>.
        /// </para>
        /// </summary>
        /// <param name="property">The property</param>
        /// <returns>True if the property should be included, or false.</returns>
        protected virtual bool IncludeProperty(PropertyInfo property) {
            if (IgnoredNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) {
                return false;
            }

            var type = property.PropertyType;
            if (IgnoredTypes.Any(a => {
                if (a.Equals(type)) { return true; }
                if (a.IsAssignableFrom(type)) { return true; }

                if (type.IsGenericType && a.IsGenericType) {
                    var genericA = a.GetGenericTypeDefinition();
                    var genericType = type.GetGenericTypeDefinition();
                    if (genericA.Equals(genericType)) { return true; }
                    if (genericA.IsAssignableFrom(genericType)) { return true; }
                }

                return false;
            })) {
                return false;
            }

            return true;
        }

        private Type[] _ignoredTypes = new Type[] {
            typeof(ICollection<>), typeof(IEnumerable<>)
        };
        /// <summary>
        /// A list of types that cause a property to be ignored if its type is
        /// equal to or assignable to (via <see cref="Type.IsAssignableFrom(Type)"/>
        /// to the specified type.
        /// <para>
        /// The default values are <see cref="ICollection{T}"/> and <see cref="IEnumerable{T}"/>.
        /// </para>
        /// </summary>
        protected virtual Type[] IgnoredTypes { get { return _ignoredTypes; } }

        private string[] _ignoredNames = new string[] { "Key", "ParentKey" };
        /// <summary>
        /// A list of property names that will be ignored by default.
        /// <para>
        /// The default values are "Key" and "ParentKey"
        /// </para>
        /// </summary>
        protected virtual string[] IgnoredNames { get { return _ignoredNames; } }

        /// <summary>
        /// Quotes an identifier as necessary for the given database.
        /// </summary>
        /// <param name="id">The identifier to quote.</param>
        /// <returns>A quoted version of the identifier.</returns>
        protected virtual string QuoteIdentifier(string id) {
            return $"[{id}]";
        }
    }
}
