﻿ALTER DATABASE [test2] SET TRUSTWORTHY ON;

USE [test2]
GO

DROP FUNCTION NewGlobalId
go

DROP FUNCTION GetHeadItems
go

DROP FUNCTION GetDataItems
go

DROP FUNCTION ValuesListToTable
go

DROP FUNCTION ObjListToTable
go


DROP AGGREGATE ValuesToList
go

DROP AGGREGATE ObjToList
go

DROP TYPE IfcValue
go

DROP TYPE IfcObj 
go

DROP TYPE IfcFile 
go

DROP ASSEMBLY [IFCSQL]
go


-- Import the assembly
CREATE ASSEMBLY [IFCSQL]
FROM 'E:\git_hub\IFCSQL\IFCSQL\IFCSQL\bin\Debug\IFCSQL.dll'
WITH PERMISSION_SET = UNSAFE;
go

CREATE TYPE IfcValue 
EXTERNAL NAME [IFCSQL].[IfcValue];  
GO  

CREATE TYPE IfcObj 
EXTERNAL NAME [IFCSQL].[IfcObj];  
GO  

CREATE TYPE IfcFile 
EXTERNAL NAME [IFCSQL].[IfcFile];  
GO  

CREATE FUNCTION [dbo].[GetHeadItems](@wfile [IfcFile])
RETURNS  TABLE ([obj] IfcObj) WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [IFCSQL].[Functions].[GetHeadItems]
GO

CREATE FUNCTION [dbo].[GetDataItems](@wfile [IfcFile])
RETURNS  TABLE ([obj] IfcObj) 
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [IFCSQL].[Functions].[GetDataItems]
GO

CREATE FUNCTION [dbo].[ValuesListToTable](@wlist IfcValue)
RETURNS  TABLE ([obj] IfcValue) 
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [IFCSQL].[Functions].[ValuesListToTable]
GO

CREATE FUNCTION [dbo].[ObjListToTable](@wlist IfcValue)
RETURNS  TABLE ([obj] IfcObj) 
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [IFCSQL].[Functions].[ObjListToTable]
GO

CREATE FUNCTION [dbo].[NewGlobalId]()
RETURNS NVARCHAR(22)
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [IFCSQL].[Functions].[NewGlobalId]
GO

CREATE AGGREGATE ValuesToList (@input IfcValue) RETURNS IfcValue
EXTERNAL NAME [IFCSQL].ValuesToList;  

CREATE AGGREGATE ObjToList (@input IfcObj) RETURNS IfcValue
EXTERNAL NAME [IFCSQL].ObjToList;  