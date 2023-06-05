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

### Usage
OmerkckEF is designed to support applications running on a wide variety of database systems. Each DAL class under DBSchemas can perform operations specific to the relevant database system.
To use this structure, first, you need to edit your database connection information in the `Bisco.cs` and `DBServer.cs` files.
Then, you can perform your database operations using the DAL class appropriate to the relevant database.
