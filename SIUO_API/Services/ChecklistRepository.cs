using Microsoft.Data.SqlClient;

namespace SIUO_API.Services
{
    public class ChecklistRepository
    {
        private readonly ChecklistConnectionFactory _connectionFactory;

        public ChecklistRepository(
            ChecklistConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> GuardarInspeccionAsync(
            string codigoChecklist,
            string folio,
            DateTime fecha,
            TimeSpan hora,
            string status,
            string? observacionesGenerales,
            string? nombreRecibe,
            string? nombreSupervisor)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // =====================================================
                // OBTENER CHECKLIST Y VERSIÓN ACTIVA
                // =====================================================

                const string consultaVersion = @"
                    SELECT TOP 1
                        c.id_checklist,
                        v.id_version
                    FROM [userchecklist].[CHECKLIST] c
                    INNER JOIN [userchecklist].[VERSION_CHECKLIST] v
                        ON v.id_checklist = c.id_checklist
                    WHERE c.codigo = @codigoChecklist
                      AND c.activo = 1
                      AND v.activo = 1
                    ORDER BY v.id_version DESC;
                ";

                int idChecklist;
                int idVersion;

                using (var commandVersion = new SqlCommand(
                    consultaVersion,
                    connection,
                    transaction))
                {
                    commandVersion.Parameters.AddWithValue(
                        "@codigoChecklist",
                        codigoChecklist
                    );

                    using var reader =
                        await commandVersion.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        throw new InvalidOperationException(
                            $"No se encontró el checklist activo: {codigoChecklist}"
                        );
                    }

                    idChecklist = reader.GetInt32(
                        reader.GetOrdinal("id_checklist")
                    );

                    idVersion = reader.GetInt32(
                        reader.GetOrdinal("id_version")
                    );
                }

                // =====================================================
                // INSERTAR INSPECCIÓN
                // =====================================================

                const string consultaInspeccion = @"
                    INSERT INTO [userchecklist].[INSPECCION]
                    (
                        id_checklist,
                        id_version,
                        folio,
                        fecha,
                        hora,
                        status,
                        observaciones_generales,
                        nombre_recibe,
                        nombre_supervisor
                    )
                    OUTPUT INSERTED.id_inspeccion
                    VALUES
                    (
                        @idChecklist,
                        @idVersion,
                        @folio,
                        @fecha,
                        @hora,
                        @status,
                        @observacionesGenerales,
                        @nombreRecibe,
                        @nombreSupervisor
                    );
                ";

                int idInspeccion;

                using (var commandInspeccion = new SqlCommand(
                    consultaInspeccion,
                    connection,
                    transaction))
                {
                    commandInspeccion.Parameters.AddWithValue(
                        "@idChecklist",
                        idChecklist
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@idVersion",
                        idVersion
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@folio",
                        folio
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@fecha",
                        fecha.Date
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@hora",
                        hora
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@status",
                        status
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@observacionesGenerales",
                        (object?)observacionesGenerales ?? DBNull.Value
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@nombreRecibe",
                        (object?)nombreRecibe ?? DBNull.Value
                    );

                    commandInspeccion.Parameters.AddWithValue(
                        "@nombreSupervisor",
                        (object?)nombreSupervisor ?? DBNull.Value
                    );

                    idInspeccion = Convert.ToInt32(
                        await commandInspeccion.ExecuteScalarAsync()
                    );
                }

                transaction.Commit();

                return idInspeccion;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task GuardarDatosQuimicosAsync(
    int idInspeccion,
    string? producto,
    DateTime? fechaRecepcion,
    DateTime? fechaTermino,
    string? placas,
    string? horaRecepcion,
    string? horaTermino,
    string? numeroSellos,
    string? numeroFactura,
    string? turno,
    string? tripulacion)
{
    using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[DATOS_QUIMICOS]
        (
            id_inspeccion,
            producto,
            fecha_recepcion,
            fecha_termino,
            placas,
            hora_recepcion,
            hora_termino,
            numero_sellos,
            numero_factura,
            turno,
            tripulacion
        )
        VALUES
        (
            @idInspeccion,
            @producto,
            @fechaRecepcion,
            @fechaTermino,
            @placas,
            @horaRecepcion,
            @horaTermino,
            @numeroSellos,
            @numeroFactura,
            @turno,
            @tripulacion
        );
    ";

    

    using var command = new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue("@idInspeccion", idInspeccion);
    command.Parameters.AddWithValue(
        "@producto",
        (object?)producto ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@fechaRecepcion",
        (object?)fechaRecepcion?.Date ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@fechaTermino",
        (object?)fechaTermino?.Date ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@placas",
        (object?)placas ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@horaRecepcion",
        (object?)horaRecepcion ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@horaTermino",
        (object?)horaTermino ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@numeroSellos",
        (object?)numeroSellos ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@numeroFactura",
        (object?)numeroFactura ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@turno",
        (object?)turno ?? DBNull.Value
    );
    command.Parameters.AddWithValue(
        "@tripulacion",
        (object?)tripulacion ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
        }

                public async Task<int> ObtenerOCrearOperadorAsync(
            string nombreOperador)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string consultaBuscar = @"
                SELECT TOP 1 id_operador
                FROM [userchecklist].[OPERADOR]
                WHERE nombre = @nombre;
            ";

            using (var commandBuscar = new SqlCommand(
                consultaBuscar,
                connection))
            {
                commandBuscar.Parameters.AddWithValue(
                    "@nombre",
                    nombreOperador
                );

                var resultado =
                    await commandBuscar.ExecuteScalarAsync();

                if (resultado != null)
                {
                    return Convert.ToInt32(resultado);
                }
            }

            const string consultaInsertar = @"
                INSERT INTO [userchecklist].[OPERADOR]
                (
                    nombre
                )
                OUTPUT INSERTED.id_operador
                VALUES
                (
                    @nombre
                );
            ";

            using var commandInsertar = new SqlCommand(
                consultaInsertar,
                connection);

            commandInsertar.Parameters.AddWithValue(
                "@nombre",
                nombreOperador
            );

            return Convert.ToInt32(
                await commandInsertar.ExecuteScalarAsync()
            );
        }

        public async Task RelacionarOperadorConInspeccionAsync(
            int idInspeccion,
            int idOperador)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string consulta = @"
                INSERT INTO [userchecklist].[INSPECCION_OPERADOR]
                (
                    id_inspeccion,
                    id_operador
                )
                VALUES
                (
                    @idInspeccion,
                    @idOperador
                );
            ";

            using var command = new SqlCommand(
                consulta,
                connection);

            command.Parameters.AddWithValue(
                "@idInspeccion",
                idInspeccion
            );

            command.Parameters.AddWithValue(
                "@idOperador",
                idOperador
            );

            await command.ExecuteNonQueryAsync();
        }

        public async Task GuardarNivelTanqueAsync(
    int idInspeccion,
    string? nivelAntes,
    string? nivelDespues)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[PESAJE_TANQUE]
        (
            id_inspeccion,
            nivel_antes,
            nivel_despues
        )
        VALUES
        (
            @idInspeccion,
            @nivelAntes,
            @nivelDespues
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@nivelAntes",
        (object?)nivelAntes ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@nivelDespues",
        (object?)nivelDespues ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

public async Task GuardarRespuestaCondicionAsync(
    int idInspeccion,
    string codigoPregunta,
    bool respuesta)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[RESPUESTA]
        (
            id_inspeccion,
            id_pregunta,
            id_opcion,
            valor,
            observaciones
        )
        SELECT
            @idInspeccion,
            p.id_pregunta,
            o.id_opcion,
            @valor,
            NULL
        FROM [userchecklist].[PREGUNTA] p
        INNER JOIN [userchecklist].[OPCION_RESPUESTA] o
            ON o.id_pregunta = p.id_pregunta
        WHERE p.codigo = @codigoPregunta
          AND o.valor = @valorOpcion;
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    string valor = respuesta ? "true" : "false";
    string valorOpcion = respuesta ? "SI" : "NO";

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@codigoPregunta",
        codigoPregunta
    );

    command.Parameters.AddWithValue(
        "@valor",
        valor
    );

    command.Parameters.AddWithValue(
        "@valorOpcion",
        valorOpcion
    );

    await command.ExecuteNonQueryAsync();

}

public async Task GuardarCaducidadAsync(
    int idInspeccion,
    string producto,
    DateTime fechaCaducidad)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[CADUCIDAD]
        (
            id_inspeccion,
            producto,
            fecha_caducidad
        )
        VALUES
        (
            @idInspeccion,
            @producto,
            @fechaCaducidad
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@producto",
        producto
    );

    command.Parameters.AddWithValue(
        "@fechaCaducidad",
        fechaCaducidad.Date
    );

    await command.ExecuteNonQueryAsync();
}

public async Task GuardarRespuestaOpcionAsync(
    int idInspeccion,
    string codigoPregunta,
    string valorRespuesta,
    string? observaciones)
{
    using var connection =
        _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[RESPUESTA]
        (
            id_inspeccion,
            id_pregunta,
            id_opcion,
            valor,
            observaciones
        )
        SELECT
            @idInspeccion,
            p.id_pregunta,
            o.id_opcion,
            @valorRespuesta,
            @observaciones
        FROM [userchecklist].[PREGUNTA] p

        INNER JOIN [userchecklist].[OPCION_RESPUESTA] o
            ON o.id_pregunta = p.id_pregunta

        WHERE p.codigo = @codigoPregunta
          AND o.valor = @valorRespuesta;
    ";

    using var command =
        new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@codigoPregunta",
        codigoPregunta
    );

    command.Parameters.AddWithValue(
        "@valorRespuesta",
        valorRespuesta
    );

    command.Parameters.AddWithValue(
        "@observaciones",
        (object?)observaciones ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

// GUARDAR RESPUESTA DE CONDICIONES DEL MATERIAL
// Este método guarda una respuesta de tipo texto para una
// pregunta del checklist.
//
// Se utiliza para preguntas que manejan las opciones:
// CUMPLE, NO_CUMPLE y NA.
//
// A diferencia de GuardarRespuestaCondicionAsync(bool),
// este método recibe el valor seleccionado como texto.
// =========================================================

public async Task GuardarRespuestaOpcionPorChecklistAsync(
    int idInspeccion,
    string tipoChecklist,
    string codigoPregunta,
    string valorRespuesta,
    string? observaciones)
{
    using var connection =
        _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[RESPUESTA]
        (
            id_inspeccion,
            id_pregunta,
            id_opcion,
            valor,
            observaciones
        )
        SELECT
            @idInspeccion,
            p.id_pregunta,
            o.id_opcion,
            @valorRespuesta,
            @observaciones
        FROM [userchecklist].[INSPECCION] i

        INNER JOIN [userchecklist].[VERSION_CHECKLIST] v
            ON v.id_version = i.id_version

        INNER JOIN [userchecklist].[SECCION] s
            ON s.id_version = v.id_version

        INNER JOIN [userchecklist].[PREGUNTA] p
            ON p.id_seccion = s.id_seccion

        INNER JOIN [userchecklist].[OPCION_RESPUESTA] o
            ON o.id_pregunta = p.id_pregunta

        INNER JOIN [userchecklist].[CHECKLIST] c
            ON c.id_checklist = v.id_checklist

        WHERE i.id_inspeccion = @idInspeccion
          AND c.codigo = @tipoChecklist
          AND p.codigo = @codigoPregunta
          AND o.valor = @valorRespuesta;
    ";

    using var command =
        new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@tipoChecklist",
        tipoChecklist
    );

    command.Parameters.AddWithValue(
        "@codigoPregunta",
        codigoPregunta
    );

    command.Parameters.AddWithValue(
        "@valorRespuesta",
        valorRespuesta
    );

    command.Parameters.AddWithValue(
        "@observaciones",
        (object?)observaciones ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}


// =========================================================
// GUARDAR TIPO DE ACTIVIDAD DE TRASVASE
// =========================================================
// Guarda el tipo de trabajo seleccionado en el frontend
// relacionado con la inspección correspondiente.
//
// Tabla destino:
// [userchecklist].[ACTIVIDAD_TRASVASE]
//
// Valores esperados:
// preventivo
// trasvase
// correctivo
// modificacion
//
// id_transvase se genera automáticamente mediante IDENTITY.
//
// Compatible con SQL Server 2016.
// =========================================================
public async Task GuardarActividadTrasvaseAsync(
    int idInspeccion,
    string tipoActividad)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[ACTIVIDAD_TRASVASE]
        (
            id_inspeccion,
            tipo_actividad
        )
        VALUES
        (
            @idInspeccion,
            @tipoActividad
        );
    ";

    using var command = new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@tipoActividad",
        tipoActividad
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR PELIGRO DE LA INSPECCIÓN
// =========================================================
// Guarda cada peligro asociado a una inspección.
// El campo seleccionado se almacena como BIT:
// 1 = seleccionado
// 0 = no seleccionado.
//
// Compatible con SQL Server 2016.
// =========================================================
public async Task GuardarPeligroAsync(
    int idInspeccion,
    string peligro,
    bool seleccionado)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[PELIGRO]
        (
            id_inspeccion,
            peligro,
            seleccionado
        )
        VALUES
        (
            @idInspeccion,
            @peligro,
            @seleccionado
        );
    ";

    using var command = new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@peligro",
        peligro
    );

    command.Parameters.AddWithValue(
        "@seleccionado",
        seleccionado
    );

    await command.ExecuteNonQueryAsync();
}


// =========================================================
// GUARDAR ASPECTO AMBIENTAL DE LA INSPECCIÓN
// =========================================================
// Guarda cada aspecto ambiental asociado a una inspección.
// El campo seleccionado se almacena como BIT:
// 1 = seleccionado
// 0 = no seleccionado.
//
// Compatible con SQL Server 2016.
// =========================================================
public async Task GuardarAspectoAmbientalAsync(
    int idInspeccion,
    string aspecto,
    bool seleccionado)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[ASPECTO_AMBIENTAL]
        (
            id_inspeccion,
            aspecto,
            seleccionado
        )
        VALUES
        (
            @idInspeccion,
            @aspecto,
            @seleccionado
        );
    ";

    using var command = new SqlCommand(consulta, connection);

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@aspecto",
        aspecto
    );

    command.Parameters.AddWithValue(
        "@seleccionado",
        seleccionado
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR GRADO DE RIESGO DE LA INSPECCIÓN
// =========================================================
// Guarda un riesgo seleccionado asociado a una inspección.
//
// Tabla destino:
// [userchecklist].[GRADO_RIESGO]
//
// Cada registro representa un riesgo seleccionado.
// La tabla relaciona el riesgo directamente con
// la inspección mediante id_inspeccion.
//
// Compatible con SQL Server 2008.
// =========================================================
public async Task GuardarGradoRiesgoAsync(
    int idInspeccion,
    string tipoRiesgo)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[GRADO_RIESGO]
        (
            id_inspeccion,
            tipo_riesgo
        )
        VALUES
        (
            @idInspeccion,
            @tipoRiesgo
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@tipoRiesgo",
        tipoRiesgo
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR LLANTA DE UNA INSPECCIÓN
// =========================================================
// Guarda la información principal de una llanta:
//
// - Inspección a la que pertenece.
// - Tipo de remolque: SENCILLO o FULL.
// - Número de llanta: R1, R2, ..., R34.
// - Estado: BIEN, OBSERVACION o DANADA.
// - Comentario.
//
// Las incidencias se guardan posteriormente en
// LLANTA_INCIDENCIA.
//
// Compatible con SQL Server 2008.
// =========================================================
public async Task<int> GuardarLlantaAsync(
    int idInspeccion,
    string tipoRemolque,
    string numeroLlanta,
    string estado,
    string? comentario)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[LLANTA_INSPECCION]
        (
            id_inspeccion,
            tipo_remolque,
            numero_llanta,
            estado,
            comentario
        )
        OUTPUT INSERTED.id_llanta_inspeccion
        VALUES
        (
            @idInspeccion,
            @tipoRemolque,
            @numeroLlanta,
            @estado,
            @comentario
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@tipoRemolque",
        tipoRemolque
    );

    command.Parameters.AddWithValue(
        "@numeroLlanta",
        numeroLlanta
    );

    command.Parameters.AddWithValue(
        "@estado",
        estado
    );

    command.Parameters.AddWithValue(
        "@comentario",
        (object?)comentario ?? DBNull.Value
    );

    return Convert.ToInt32(
        await command.ExecuteScalarAsync()
    );
}


// =========================================================
// GUARDAR INCIDENCIA DE UNA LLANTA
// =========================================================
// Guarda una incidencia asociada a una llanta previamente
// registrada en LLANTA_INSPECCION.
//
// Ejemplos:
// BAJA_PRESION
// PONCHADA
// DESGASTE_IRREGULAR
// GRIETAS
// CORTE_LATERAL
// etc.
//
// Compatible con SQL Server 2008.
// =========================================================
public async Task GuardarIncidenciaLlantaAsync(
    int idLlantaInspeccion,
    string incidencia)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[LLANTA_INCIDENCIA]
        (
            id_llanta_inspeccion,
            incidencia
        )
        VALUES
        (
            @idLlantaInspeccion,
            @incidencia
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idLlantaInspeccion",
        idLlantaInspeccion
    );

    command.Parameters.AddWithValue(
        "@incidencia",
        incidencia
    );

    await command.ExecuteNonQueryAsync();
}


// =========================================================
// GUARDAR DATOS DE RECEPCIÓN
// CHECKLIST: SG-F-24-01
// =========================================================
public async Task GuardarDatosRecepcionAsync(
    int idInspeccion,
    string area,
    string? material,
    string proveedor,
    string operador,
    string? lote,
    string? turno,
    string? diseno,
    string? especificarMaterial,
    string? tripulacion,
    string? placasNumero,
    string? ordenCompra,
    string? facturaRemision,
    bool? alergenoMicroSensitivo)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    // ---------------------------------------------------------
    // Obtener ID del área
    // ---------------------------------------------------------

    int idArea;

    const string consultaArea = @"
        SELECT TOP 1 id_area
        FROM [userchecklist].[AREA]
        WHERE nombre = @nombre
          AND activo = 1;
    ";

    using (var commandArea = new SqlCommand(
        consultaArea,
        connection))
    {
        commandArea.Parameters.AddWithValue(
            "@nombre",
            area
        );

        var resultado = await commandArea.ExecuteScalarAsync();

        if (resultado == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el área activa: {area}"
            );
        }

        idArea = Convert.ToInt32(resultado);
    }

    // ---------------------------------------------------------
    // Obtener ID del material
    // ---------------------------------------------------------

    int? idMaterial = null;

    if (!string.IsNullOrWhiteSpace(material))
    {
        const string consultaMaterial = @"
            SELECT TOP 1 id_material
            FROM [userchecklist].[MATERIAL]
            WHERE nombre = @nombre
              AND activo = 1;
        ";

        using var commandMaterial = new SqlCommand(
            consultaMaterial,
            connection
        );

        commandMaterial.Parameters.AddWithValue(
            "@nombre",
            material
        );

        var resultadoMaterial =
            await commandMaterial.ExecuteScalarAsync();

        if (resultadoMaterial == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el material activo: {material}"
            );
        }

        idMaterial = Convert.ToInt32(resultadoMaterial);
    }

    // ---------------------------------------------------------
    // Obtener ID del proveedor
    // ---------------------------------------------------------

    int idProveedor;

// =========================================================
// OBTENER O CREAR PROVEEDOR
// =========================================================

using (var commandProveedor = new SqlCommand(
    @"
    SELECT id_proveedor
    FROM [userchecklist].[PROVEEDOR]
    WHERE nombre = @nombre
    ",
    connection))
{
    commandProveedor.Parameters.AddWithValue(
        "@nombre",
        proveedor.Trim()
    );

    object? resultado =
        await commandProveedor.ExecuteScalarAsync();

    if (resultado != null)
    {
        idProveedor = Convert.ToInt32(resultado);
    }
    else
    {
        using var commandCrearProveedor = new SqlCommand(
            @"
            INSERT INTO [userchecklist].[PROVEEDOR]
            (
                nombre,
                activo
            )
            OUTPUT INSERTED.id_proveedor
            VALUES
            (
                @nombre,
                1
            );
            ",
            connection);

        commandCrearProveedor.Parameters.AddWithValue(
            "@nombre",
            proveedor.Trim()
        );

        idProveedor = Convert.ToInt32(
            await commandCrearProveedor.ExecuteScalarAsync()
        );
    }
}
    // ---------------------------------------------------------
    // Obtener o crear operador
    // ---------------------------------------------------------

    int idOperador =
        await ObtenerOCrearOperadorAsync(
            operador
        );

    // ---------------------------------------------------------
    // Guardar DATOS_RECEPCION
    // ---------------------------------------------------------

    const string consulta = @"
        INSERT INTO [userchecklist].[DATOS_RECEPCION]
        (
            id_inspeccion,
            id_area,
            id_material,
            id_proveedor,
            id_operador,
            lote,
            turno,
            diseno,
            especificar_material,
            tripulacion,
            placas_numero,
            orden_compra,
            factura_remision,
            alergeno_micro_sensitivo
        )
        VALUES
        (
            @idInspeccion,
            @idArea,
            @idMaterial,
            @idProveedor,
            @idOperador,
            @lote,
            @turno,
            @diseno,
            @especificarMaterial,
            @tripulacion,
            @placasNumero,
            @ordenCompra,
            @facturaRemision,
            @alergenoMicroSensitivo
        );
    ";

    using var command = new SqlCommand(
        consulta,
        connection
    );

    command.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    command.Parameters.AddWithValue(
        "@idArea",
        idArea
    );

    command.Parameters.AddWithValue(
        "@idMaterial",
        (object?)idMaterial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@idProveedor",
        idProveedor
    );

    command.Parameters.AddWithValue(
        "@idOperador",
        idOperador
    );

    command.Parameters.AddWithValue(
        "@lote",
        (object?)lote ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@turno",
        (object?)turno ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@diseno",
        (object?)diseno ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@especificarMaterial",
        (object?)especificarMaterial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tripulacion",
        (object?)tripulacion ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@placasNumero",
        (object?)placasNumero ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@ordenCompra",
        (object?)ordenCompra ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@facturaRemision",
        (object?)facturaRemision ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@alergenoMicroSensitivo",
        (object?)alergenoMicroSensitivo ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

    }

}