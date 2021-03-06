## MembershipReboot.Dapper

MembershipReboot.Dapper is an extension to MembershipReboot that allows you to use Dapper as the ORM. It takes the place of the
MembershipReboot.EntityFramework project that comes with MembershipReboot.

This project has only been tested with SQL Server.

### Building

Clone the repository

```
git clone https://github.com/tncalvert/MembershipReboot.Dapper.git
```

The project targets .NET Framework 4.5.2 and is built using the .NET Core 2.0.2 which can be found [here](https://www.microsoft.com/net/download/windows).

You can build the project through Visual Studio 2017 or from the command line with the `dotnet` command.

### Usage

There are two main classes:

`DapperUserAccountRepository` which implements `IUserAccountRepository<TAccount>` where `TAccount` is a
`RelationalUserAccount`.

`DapperGroupRepository` which implements `IGroupRepository<TGroup>`  where `TGroup` is a `RelationalGroup`.

If you are going to use `RelationalUserAccount` and `RelationalGroup` explicitly, there are two default version
that you can use directly: `DefaultUserAccountRepository` and `DefaultGroupRepository`.

The repository must be provided to the appropriate MembershipReboot service, either directly or through dependency injection.

#### API

* `DapperUserAccountRepository`
```C#
DapperUserAccountRepository(IDbConnection connection, Utilities utilities = null, string schema = "dbo", string userAccountTable = "UserAccounts",
            Dictionary<Type, string> tableNameMap = null, Dictionary<Type, PropertyInfo> keySelectorMap = null)
```
| Parameter | Purpose | Default | Notes |
| --- | --- | --- | --- |
| connection | The instance of `IDbConnection` that is connected to the database. | none | Must be instantiated. If it is closed, it will be opened. |
| utilities | An instance of the `Utilities` class | null | See below for details. |
| schema | The name of the schema used for tables | "dbo" | |
| userAccountTable | The name of the table used to store accounts | "UserAccounts" | |
| tableNameMap | Used to map child type objects (e.g., user claims) to the appropriate table. | null | Default values are provided for the child types in `RelationalUserAccount`. See below. |
| keySelectorMap | Used to define the key property of child types. | null | Default values are provided for the types in `RelationalUserAccount`. See below. |

* `DapperGroupRepository`
```C#
DapperGroupRepository(IDbConnection connection, Utilities utilities = null, string schema = "dbo", string groupTable = "Groups",
            Dictionary<Type, string> tableNameMap = null, Dictionary<Type, PropertyInfo> keySelectorMap = null)
```
| Parameter | Purpose | Default | Notes |
| --- | --- | --- | --- |
| connection | The instance of `IDbConnection` that is connected to the database. | none | Must be instantiated. If it is closed, it will be opened. |
| utilities | An instance of the `Utilities` class | null | See below for details. |
| schema | The name of the schema used for tables | "dbo" | |
| groupTable | The name of the table used to store accounts | "Groups" | |
| tableNameMap | Used to map child type objects (e.g., group childs) to the appropriate table. | null | Default values are provided for the child types in `RelationalGroup`. See below. |
| keySelectorMap | Used to define the key property of child types. | null | Default values are provided for the types in `RelationalGroup`. See below. |

#### Utilities

The `Utilities` class is used to provide a way to overload certain actions used in the repository. It contains methods to quote and
escape identifiers, identify the properties and child collections of a type, and to build property lists that can be used to
create SQL statements. Nearly every method in the class can be overridden to provide whatever functionality is needed.

##### Methods

```
public virtual EscapeTableName(string tableName)
```
Escapes any illegal characters in a table name. The default is to duplicate any square brackets ([, ]).

```
public virtual string GetColumnIdentifiers<T>()
public virtual string GetColumnIdentifiers(Type t)
```
Returns the properties identified by `GetTypeProperties` for the given type formatted as `[Key], [Prop1], [Prop1]`.
The identifiers are quoted using `QuoteIdentifier`.

```
public virtual string GetColumnParameters<T>()
public virtual string GetColumnParameters(Type t)
```
Returns the properties identified by `GetTypeProperties` for the given type formatted as `@Key, @Prop1, @Prop2`
to be used in SQL statements.

```
public virtual string GetColumnAssignment<T>()
public virtual string GetColumnAssignment(Type t)
```
Returns the properties identified by `GetTypeProperties` for the given type formatted as `[Key] = @Key, [Prop1] = @Prop1, [Prop2] = @Prop2`
to be used in SQL statements. The identifiers are quoted using `QuoteIdentifier`.

```
public virtual IEnumerable<PropertyInfo> GetTypeProperties<T>()
public virtual IEnumerable<PropertyInfo> GetTypeProperties(Type t)
```
Retrieves the properties (as a `PropertyInfo`) for the given type as identified by `IncludeProperty`. The results of this will be used to
determine which properties are included in `SELECT`, `INSERT`, and `UPDATE` statements. The results of this are cached.

```
public IEnumerable<string> GetTypePropertyNames<T>()
public IEnumerable<string> GetTypePropertyNames(Type t)
```
Retrieves the property names returned by `GetTypeProperties`.

```
public virtual IEnumerable<PropertyInfo> GetChildCollectionProperties<T>()
public virtual IEnumerable<PropertyInfo> GetChildCollectionProperties(Type t)
```
Retrieves the child collection properties (as a `PropertyInfo`) for the given type as identified by `IsChildCollectionProperty`. The results of this will be used to
determine which child collection are retrieved or updated in `SELECT`, `INSERT`, and `UPDATE` statements. The results of this are cached.

```
public IEnumerable<string> GetChildCollectionPropertyNames<T>()
public IEnumerable<string> GetChildCollectionPropertyNames(Type t)
```
Retrieves the property names returned by `GetChildCollectionProperties`.

```
protected virtual bool IsChildCollectionProperty(PropertyInfo property)
```
Determines if a property represents a collection of child objects. The default behavior is to return only properties
that implement `ICollection<>`.

```
protected virtual bool IncludeProperty(PropertyInfo property)
```
Determines if a property should be included. The default behavior is to check `IgnoredTypes` and `IgnoredNames`.
Additionally, read-only properties are ignored.

```
protected virtual Type[] IgnoredTypes
```
Returns an array of types containing types that cause a property to be ignored when retrieving the properties from a type.
The default values are `[ typeof(ICollection<>), typeof(IEnumerable<>) ]`. A type matches if it is is equal
to or assignable (via `Type.IsAssignableFrom(Type)`) to the listed type. If the property being checked is a generic
type, the generic definition will be retrieved (via `Type.GetGenericTypeDefinition()`) before comparison.

```
protected virtual string[] IgnoredNames
```
Returns an array of string containing the names that are ignored by default when retrieving the properties from a type.
The default values are `[ "Key", "ParentKey" ]`.

```
public virtual string QuoteIdentifier(string id)
```
Quotes an identifier for use in an SQL statement. The default is to use square brackets ([, ]).

#### Default Table Names and Keys

| Type | Table Name | Key |
| --- | --- | --- |
| UserAccount | "UserAccounts" | `Key` |
| UserClaim | "UserClaims" | `Key` |
| LinkedAccount | "LinkedAccounts" | `Key` |
| LinkedAccountClaim | "LinkedAccountClaims" | `Key` |
| PasswordResetSecret | "PasswordResetSecrets" | `Key` |
| TwoFactorAuthToken | "TwoFactorAuthTokens" | `Key` |
| UserCertificate | "UserCertificates" | `Key` |
| Group | "Groups" | `Key` |
| GroupChild | "GroupChilds" | `Key` |

### Tests

There is a test suite in the `MembershipReboot.Dapper.Tests` project. It uses xUnit. The tests can be run through
Visual Studio or by navigating to the project folder and running `dotnet test`.

You can generate and view test coverage (through [OpenCover](https://github.com/OpenCover/opencover) and [ReportGenerator](https://github.com/danielpalme/ReportGenerator)) by running the `run_test_coverage.ps1`
script. This will create a folder called `test_coverage` in the solution folder. Inside will be the coverage results
and generated report. The report will automatically open after the script is finished.

### Thanks

* Brock Allen for [MembershipReboot](https://github.com/brockallen/BrockAllen.MembershipReboot)
* StackExchange for [Dapper](https://github.com/StackExchange/dapper-dot-net)

### License

MIT
