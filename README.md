# OmerkckEF: An ADO.NET ORM Tool Supporting Multiple Databases
## About
OmerkckEF is an ADO.NET ORM (Object-Relational Mapping) tool designed to support multiple database systems. This application has a structure similar to Microsoft's EntityFramework and aims to work compatibly with different database systems. It provides more flexibility and control to its users, thereby making database operations more efficient.

## Content
### DBContext:

* `Bisco.cs`: Contains database connection information.
* `DBServer.cs`: Contains database server information.
* `EntityContext.cs`: Class used for performing operations over the Entity framework.

### DBContext > DBSchemas:

* `MySqlDAL.cs`: Performs MySQL database operations.
* `SqlDAL.cs`: Performs Microsoft SQL Server operations.
* `OracleDAL.cs`: Performs Oracle database operations.
* `PostgreSQLDAL.cs`: Performs PostgreSQL database operations.

### Interface:

* `IDALFactory.cs`: Interface for Data Access Layer factory classes.
* `IORM.cs`: Interface for ORM operations.

### Repositories:

* `DALFactoryBase.cs`: Abstract base class used to create the relevant DAL class depending on the database type.
* `ORMBase.cs`: Base class of ORM operations.

### ToolKit:

* `Attributes.cs`: Contains custom attributes.
* `Enums.cs`: Contains enums used throughout the application.
* `BisExpression.cs`: Class used to create an expression tree.
* `Extensions.cs`: Contains extension methods used throughout the application.
* `Result.cs`: Holds the result of database operations.
* `Tools.cs`: Contains general-purpose helper functions.

### Convert Classes to Table:

This repository contains a set of methods for interacting with a MySQL database schema. These methods enable various operations such as creating tables, dropping tables, updating tables, and modifying table columns. Each method is designed to provide flexibility and ease of use when working with database schemas in a .NET environment.

The methods provided in this repository include:
* `CreateTable:` Creates a new table in the specified schema or the default database schema if none is provided.
* `DropTable:` Drops an existing table from the specified schema or the default database schema if none is provided.
* `UpdateTable:` Updates an existing table in the specified schema or the default database schema if none is provided. This method allows adding new columns to the table based on the provided class properties.
* `RemoveTableColumn:` Removes a column from an existing table in the specified schema or the default database schema if none is provided. This method also supports removing all columns from the table.

Additionally, there is a method called AddAttributeToTableColumn which allows adding specific attributes to columns in an existing table. This method provides flexibility in modifying column properties dynamically.
Feel free to use these methods in your projects to simplify database schema management tasks. If you have any questions or suggestions, please don't hesitate to reach out.


### Usage
OmerkckEF is designed to support applications running on a wide variety of database systems.
Each DAL class under DBSchemas can perform operations specific to the relevant database system.
To use this structure, first, you need to edit your database connection information in the `Bisco.cs` and `DBServer.cs` files.
Then, you can perform your database operations using the DAL class appropriate to the relevant database.

```cs
public static class ExFunction
{
  private static EntityContext? _entityContext;
  public static EntityContext? OmerkckEfContexts
  {
      get
      {
          _entityContext ??= new EntityContext(new DBServer
          {
              DbIp = "127.0.0.1",
              DbSchema = "rootDB",
              DbUser = "root",
              DbPassword = "12345",
              DbSslMode = "Required"
          });
          return _entityContext;
      }
  }
}

// ex: _ = ExFunction.OmerkckEfContexts;
```
