CREATE DATABASE SIUO_Dispositivos;
GO

USE SIUO_Dispositivos;
GO


CREATE TABLE Dispositivos (
    IdDispositivo INT IDENTITY(1,1) PRIMARY KEY,

    IdentificadorDispositivo NVARCHAR(150) NULL,

    NombreDispositivo NVARCHAR(100) NOT NULL,

    TipoDispositivo NVARCHAR(30) NULL,

    Activo BIT NOT NULL DEFAULT 1,

    FechaRegistro DATETIME2 NOT NULL DEFAULT GETDATE(),

    FechaActualizacion DATETIME2 NULL
);
GO


CREATE UNIQUE INDEX IX_Dispositivos_Identificador
ON Dispositivos(IdentificadorDispositivo)
WHERE IdentificadorDispositivo IS NOT NULL;
GO


CREATE UNIQUE INDEX IX_Dispositivos_Nombre
ON Dispositivos(NombreDispositivo);
GO

INSERT INTO Dispositivos
    (NombreDispositivo, TipoDispositivo)
VALUES
    ('TELEFONO1', 'TELEFONO'),
    ('TELEFONO2', 'TELEFONO'),
    ('TELEFONO3', 'TELEFONO'),
    ('TELEFONO4', 'TELEFONO'),
    ('TELEFONO5', 'TELEFONO'),
    ('TELEFONO6', 'TELEFONO'),
    ('TELEFONO7', 'TELEFONO'),
    ('TELEFONO8', 'TELEFONO'),
    ('TELEFONO9', 'TELEFONO'),
    ('TELEFONO10', 'TELEFONO'),
    ('TELEFONO11', 'TELEFONO'),
    ('TELEFONO12', 'TELEFONO'),
    ('TELEFONO13', 'TELEFONO'),
    ('TELEFONO14', 'TELEFONO'),
    ('TELEFONO15', 'TELEFONO'),
    ('TELEFONO16', 'TELEFONO'),
    ('TELEFONO17', 'TELEFONO'),
    ('TELEFONO18', 'TELEFONO'),
    ('TELEFONO19', 'TELEFONO'),
    ('TELEFONO20', 'TELEFONO'),
    ('TELEFONO21', 'TELEFONO'),
    ('TELEFONO22', 'TELEFONO'),
    ('TELEFONO23', 'TELEFONO'),
    ('TELEFONO24', 'TELEFONO'),
    ('TELEFONO25', 'TELEFONO'),
    ('TELEFONO26', 'TELEFONO'),
    ('TELEFONO27', 'TELEFONO'),
    ('TELEFONO28', 'TELEFONO'),
    ('TELEFONO29', 'TELEFONO'),
    ('TELEFONO30', 'TELEFONO'),
    ('TELEFONO31', 'TELEFONO'),
    ('TELEFONO32', 'TELEFONO'),
    ('TELEFONO33', 'TELEFONO'),
    ('TELEFONO34', 'TELEFONO'),
    ('TELEFONO35', 'TELEFONO'),
    ('TELEFONO36', 'TELEFONO'),
    ('TELEFONO37', 'TELEFONO'),
    ('TELEFONO38', 'TELEFONO'),
    ('TELEFONO39', 'TELEFONO'),
    ('TELEFONO40', 'TELEFONO');

GO