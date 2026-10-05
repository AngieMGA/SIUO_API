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
            string? status,
            string? observacionesGenerales,
            string? nombreRecibe,
            string? nombreSupervisor,
            string? identificadorDispositivo = null)
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
                        nombre_supervisor,
                        identificador_dispositivo
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
                    @nombreSupervisor,
                    @identificadorDispositivo
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
                        (object?)status ?? DBNull.Value
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
                    commandInspeccion.Parameters.AddWithValue(
                        "@identificadorDispositivo",
                        (object?)identificadorDispositivo ?? DBNull.Value
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

// =========================================================
// GUARDAR PESAJE SG-F-24-01
// =========================================================

public async Task GuardarPesajeSGF2401Async(
    int idInspeccion,
    decimal? pesoInicial,
    decimal? pesoFinal,
    decimal? tara,
    string? numeroCodigo,
    decimal? tq1Inicial,
    decimal? tq1Final,
    decimal? psiTq1Inicial,
    decimal? psiTq1Final,
    decimal? tq2Inicial,
    decimal? tq2Final,
    decimal? psiTq2Inicial,
    decimal? psiTq2Final)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[PESAJE_TANQUE]
        (
            id_inspeccion,
            peso_inicial,
            peso_final,
            tara,
            numero_codigo,
            tq1_inicial,
            tq1_final,
            psi_tq1_inicial,
            psi_tq1_final,
            tq2_inicial,
            tq2_final,
            psi_tq2_inicial,
            psi_tq2_final
        )
        VALUES
        (
            @idInspeccion,
            @pesoInicial,
            @pesoFinal,
            @tara,
            @numeroCodigo,
            @tq1Inicial,
            @tq1Final,
            @psiTq1Inicial,
            @psiTq1Final,
            @tq2Inicial,
            @tq2Final,
            @psiTq2Inicial,
            @psiTq2Final
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
        "@pesoInicial",
        (object?)pesoInicial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@pesoFinal",
        (object?)pesoFinal ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tara",
        (object?)tara ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@numeroCodigo",
        (object?)numeroCodigo ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tq1Inicial",
        (object?)tq1Inicial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tq1Final",
        (object?)tq1Final ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@psiTq1Inicial",
        (object?)psiTq1Inicial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@psiTq1Final",
        (object?)psiTq1Final ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tq2Inicial",
        (object?)tq2Inicial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tq2Final",
        (object?)tq2Final ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@psiTq2Inicial",
        (object?)psiTq2Inicial ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@psiTq2Final",
        (object?)psiTq2Final ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR MERMA SG-F-24-01
// =========================================================

public async Task GuardarMermaSGF2401Async(
    int idInspeccion,
    int numeroSaco,
    decimal pesoKg)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[MERMA_AZUCAR]
        (
            id_inspeccion,
            numero_saco,
            peso_kg
        )
        VALUES
        (
            @idInspeccion,
            @numeroSaco,
            @pesoKg
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
        "@numeroSaco",
        numeroSaco
    );

    command.Parameters.AddWithValue(
        "@pesoKg",
        pesoKg
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR RESUMEN DE MERMA SG-F-24-01
// =========================================================

public async Task GuardarResumenMermaSGF2401Async(
    int idInspeccion,
    decimal? totalKg,
    decimal? promedio,
    decimal? diferenciaPromedioTeorico,
    decimal? kgTotalesMerma)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[RESUMEN_MERMA]
        (
            id_inspeccion,
            total_kg,
            promedio,
            diferencia_promedio_teorico,
            kg_totales_merma
        )
        VALUES
        (
            @idInspeccion,
            @totalKg,
            @promedio,
            @diferenciaPromedioTeorico,
            @kgTotalesMerma
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
        "@totalKg",
        (object?)totalKg ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@promedio",
        (object?)promedio ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@diferenciaPromedioTeorico",
        (object?)diferenciaPromedioTeorico ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@kgTotalesMerma",
        (object?)kgTotalesMerma ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// GUARDAR ESTADO DE SUPERSACO SG-F-24-01
// =========================================================

public async Task GuardarEstadoSupersacoSGF2401Async(
    int idInspeccion,
    string tipoFalla,
    int posicion,
    string estado)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[ESTADO_SUPERSACO]
        (
            id_inspeccion,
            tipo_falla,
            posicion,
            estado
        )
        VALUES
        (
            @idInspeccion,
            @tipoFalla,
            @posicion,
            @estado
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
        "@tipoFalla",
        tipoFalla
    );

    command.Parameters.AddWithValue(
        "@posicion",
        posicion
    );

    command.Parameters.AddWithValue(
        "@estado",
        estado
    );

    await command.ExecuteNonQueryAsync();
}
public async Task GuardarDatosOperadorRHF0121Async(
    int idInspeccion,
    int idOperador,
    string? presentacion,
    string? observacionesEpp,
    string? telefono,
    string? licenciaFederal,
    string? tarjetaCirculacion,
    string? numeroImss,
    string? seguroVigente,
    string? cartaPorte)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consultaOperador = @"
        UPDATE [userchecklist].[OPERADOR]
        SET
            telefono = CASE
                WHEN NULLIF(@telefono, '') IS NULL THEN telefono
                ELSE @telefono
            END,
            licencia_federal = CASE
                WHEN NULLIF(@licenciaFederal, '') IS NULL THEN licencia_federal
                ELSE @licenciaFederal
            END,
            tarjeta_circulacion = CASE
                WHEN NULLIF(@tarjetaCirculacion, '') IS NULL THEN tarjeta_circulacion
                ELSE @tarjetaCirculacion
            END,
            numero_imss = CASE
                WHEN NULLIF(@numeroImss, '') IS NULL THEN numero_imss
                ELSE @numeroImss
            END,
            seguro_vigente = CASE
                WHEN NULLIF(@seguroVigente, '') IS NULL THEN seguro_vigente
                ELSE @seguroVigente
            END,
            carta_porte = CASE
                WHEN NULLIF(@cartaPorte, '') IS NULL THEN carta_porte
                ELSE @cartaPorte
            END
        WHERE id_operador = @idOperador;
    ";

    using (var commandOperador = new SqlCommand(
        consultaOperador,
        connection))
    {
        commandOperador.Parameters.AddWithValue(
            "@telefono",
            (object?)telefono ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@licenciaFederal",
            (object?)licenciaFederal ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@tarjetaCirculacion",
            (object?)tarjetaCirculacion ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@numeroImss",
            (object?)numeroImss ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@seguroVigente",
            (object?)seguroVigente ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@cartaPorte",
            (object?)cartaPorte ?? DBNull.Value
        );

        commandOperador.Parameters.AddWithValue(
            "@idOperador",
            idOperador
        );

        await commandOperador.ExecuteNonQueryAsync();
    }

    const string consultaRH = @"
        INSERT INTO [userchecklist].[DATOS_OPERADOR_RH]
        (
            id_inspeccion,
            id_operador,
            presentacion,
            observaciones_epp
        )
        VALUES
        (
            @idInspeccion,
            @idOperador,
            @presentacion,
            @observacionesEpp
        );
    ";

    using var commandRH = new SqlCommand(
        consultaRH,
        connection);

    commandRH.Parameters.AddWithValue(
        "@idInspeccion",
        idInspeccion
    );

    commandRH.Parameters.AddWithValue(
        "@idOperador",
        idOperador
    );

    commandRH.Parameters.AddWithValue(
        "@presentacion",
        (object?)presentacion ?? DBNull.Value
    );

    commandRH.Parameters.AddWithValue(
        "@observacionesEpp",
        (object?)observacionesEpp ?? DBNull.Value
    );

    await commandRH.ExecuteNonQueryAsync();
}

public async Task<int> ObtenerOCrearTransporteRHF0121Async(
    string lineaTransporte,
    string? numeroTractor,
    string? numeroRemolque1,
    string? numeroRemolque2,
    string? placasTractor,
    string? placasRemolque1,
    string? placasRemolque2)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consultaBuscar = @"
        SELECT TOP 1 id_transporte
        FROM [userchecklist].[TRANSPORTE]
        WHERE linea_transporte = @lineaTransporte
          AND ISNULL(numero_tractor, '') = ISNULL(@numeroTractor, '')
          AND ISNULL(numero_remolque1, '') = ISNULL(@numeroRemolque1, '')
          AND ISNULL(numero_remolque2, '') = ISNULL(@numeroRemolque2, '')
          AND ISNULL(placas_tractor, '') = ISNULL(@placasTractor, '')
          AND ISNULL(placas_remolque1, '') = ISNULL(@placasRemolque1, '')
          AND ISNULL(placas_remolque2, '') = ISNULL(@placasRemolque2, '');
    ";

    using (var commandBuscar = new SqlCommand(
        consultaBuscar,
        connection))
    {
        commandBuscar.Parameters.AddWithValue(
            "@lineaTransporte",
            lineaTransporte
        );

        commandBuscar.Parameters.AddWithValue(
            "@numeroTractor",
            (object?)numeroTractor ?? DBNull.Value
        );

        commandBuscar.Parameters.AddWithValue(
            "@numeroRemolque1",
            (object?)numeroRemolque1 ?? DBNull.Value
        );

        commandBuscar.Parameters.AddWithValue(
            "@numeroRemolque2",
            (object?)numeroRemolque2 ?? DBNull.Value
        );

        commandBuscar.Parameters.AddWithValue(
            "@placasTractor",
            (object?)placasTractor ?? DBNull.Value
        );

        commandBuscar.Parameters.AddWithValue(
            "@placasRemolque1",
            (object?)placasRemolque1 ?? DBNull.Value
        );

        commandBuscar.Parameters.AddWithValue(
            "@placasRemolque2",
            (object?)placasRemolque2 ?? DBNull.Value
        );

        var resultado =
            await commandBuscar.ExecuteScalarAsync();

        if (resultado != null)
        {
            return Convert.ToInt32(resultado);
        }
    }

    const string consultaInsertar = @"
        INSERT INTO [userchecklist].[TRANSPORTE]
        (
            linea_transporte,
            numero_tractor,
            numero_remolque1,
            numero_remolque2,
            placas_tractor,
            placas_remolque1,
            placas_remolque2
        )
        OUTPUT INSERTED.id_transporte
        VALUES
        (
            @lineaTransporte,
            @numeroTractor,
            @numeroRemolque1,
            @numeroRemolque2,
            @placasTractor,
            @placasRemolque1,
            @placasRemolque2
        );
    ";

    using var commandInsertar = new SqlCommand(
        consultaInsertar,
        connection);

    commandInsertar.Parameters.AddWithValue(
        "@lineaTransporte",
        lineaTransporte
    );

    commandInsertar.Parameters.AddWithValue(
        "@numeroTractor",
        (object?)numeroTractor ?? DBNull.Value
    );

    commandInsertar.Parameters.AddWithValue(
        "@numeroRemolque1",
        (object?)numeroRemolque1 ?? DBNull.Value
    );

    commandInsertar.Parameters.AddWithValue(
        "@numeroRemolque2",
        (object?)numeroRemolque2 ?? DBNull.Value
    );

    commandInsertar.Parameters.AddWithValue(
        "@placasTractor",
        (object?)placasTractor ?? DBNull.Value
    );

    commandInsertar.Parameters.AddWithValue(
        "@placasRemolque1",
        (object?)placasRemolque1 ?? DBNull.Value
    );

    commandInsertar.Parameters.AddWithValue(
        "@placasRemolque2",
        (object?)placasRemolque2 ?? DBNull.Value
    );

    return Convert.ToInt32(
        await commandInsertar.ExecuteScalarAsync()
    );
}

public async Task GuardarDatosTransporteRHF0121Async(
    int idInspeccion,
    int idOperador,
    int idTransporte,
    string? origenDescarga,
    string? destinoCarga,
    string? numeroSellos,
    string? noDelivery,
    string? tipoTransporte,
    string? tipoUnidad,
    string? nivelTanque1,
    string? nivelTanque2,
    string? refrigerado,
    DateTime? fechaHoraLlegada,
    DateTime? fechaHoraSalida,
    decimal? temperatura,
    string? resultadoFinal,
    string? rampaAsignada,
    string? comentarios)
{
    using var connection = _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[DATOS_TRANSPORTE_RH]
        (
            id_inspeccion,
            id_operador,
            id_transporte,
            origen_descarga,
            destino_carga,
            numero_sellos,
            no_delivery,
            tipo_transporte,
            tipo_unidad,
            nivel_tanque1,
            nivel_tanque2,
            refrigerado,
            fecha_hora_llegada,
            fecha_hora_salida,
            temperatura,
            resultado_final,
            rampa_asignada,
            comentarios
        )
        VALUES
        (
            @idInspeccion,
            @idOperador,
            @idTransporte,
            @origenDescarga,
            @destinoCarga,
            @numeroSellos,
            @noDelivery,
            @tipoTransporte,
            @tipoUnidad,
            @nivelTanque1,
            @nivelTanque2,
            @refrigerado,
            @fechaHoraLlegada,
            @fechaHoraSalida,
            @temperatura,
            @resultadoFinal,
            @rampaAsignada,
            @comentarios
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

    command.Parameters.AddWithValue(
        "@idTransporte",
        idTransporte
    );

    command.Parameters.AddWithValue(
        "@origenDescarga",
        (object?)origenDescarga ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@destinoCarga",
        (object?)destinoCarga ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@numeroSellos",
        (object?)numeroSellos ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@noDelivery",
        (object?)noDelivery ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tipoTransporte",
        (object?)tipoTransporte ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tipoUnidad",
        (object?)tipoUnidad ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@nivelTanque1",
        (object?)nivelTanque1 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@nivelTanque2",
        (object?)nivelTanque2 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@refrigerado",
        (object?)refrigerado ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@fechaHoraLlegada",
        (object?)fechaHoraLlegada ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@fechaHoraSalida",
        (object?)fechaHoraSalida ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@temperatura",
        (object?)temperatura ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@resultadoFinal",
        (object?)resultadoFinal ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@rampaAsignada",
        (object?)rampaAsignada ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@comentarios",
        (object?)comentarios ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}

// =========================================================
// RH-F-01-21
// OBTENER O CREAR OPERADOR COMPLETO
// =========================================================
public async Task<int> ObtenerOCrearOperadorRHAsync(
    string nombre,
    string? telefono,
    string? licenciaFederal,
    string? tarjetaCirculacion,
    string? numeroIMSS,
    string? seguroVigente,
    string? cartaPorte)
{
    using var connection =
        _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    // -----------------------------------------------------
    // Buscar operador por nombre
    // -----------------------------------------------------

    const string consultaBuscar = @"
        SELECT TOP 1
            id_operador
        FROM [userchecklist].[OPERADOR]
        WHERE nombre = @nombre;
    ";

    using (var commandBuscar =
        new SqlCommand(
            consultaBuscar,
            connection))
    {
        commandBuscar.Parameters.AddWithValue(
            "@nombre",
            nombre
        );

        var resultado =
            await commandBuscar.ExecuteScalarAsync();

        if (resultado != null)
        {
            int idOperador =
                Convert.ToInt32(resultado);

            // -------------------------------------------------
            // Actualizar datos del operador existente
            // -------------------------------------------------

            const string consultaActualizar = @"
                UPDATE [userchecklist].[OPERADOR]
                SET
                    telefono = @telefono,
                    licencia_federal = @licenciaFederal,
                    tarjeta_circulacion = @tarjetaCirculacion,
                    numero_imss = @numeroIMSS,
                    seguro_vigente = @seguroVigente,
                    carta_porte = @cartaPorte
                WHERE id_operador = @idOperador;
            ";

            using var commandActualizar =
                new SqlCommand(
                    consultaActualizar,
                    connection);

            commandActualizar.Parameters.AddWithValue(
                "@telefono",
                (object?)telefono ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@licenciaFederal",
                (object?)licenciaFederal ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@tarjetaCirculacion",
                (object?)tarjetaCirculacion ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@numeroIMSS",
                (object?)numeroIMSS ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@seguroVigente",
                (object?)seguroVigente ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@cartaPorte",
                (object?)cartaPorte ?? DBNull.Value
            );

            commandActualizar.Parameters.AddWithValue(
                "@idOperador",
                idOperador
            );

            await commandActualizar.ExecuteNonQueryAsync();

            return idOperador;
        }
    }

    // -----------------------------------------------------
    // Crear operador
    // -----------------------------------------------------

    const string consultaCrear = @"
        INSERT INTO [userchecklist].[OPERADOR]
        (
            nombre,
            telefono,
            licencia_federal,
            tarjeta_circulacion,
            numero_imss,
            seguro_vigente,
            carta_porte
        )
        OUTPUT INSERTED.id_operador
        VALUES
        (
            @nombre,
            @telefono,
            @licenciaFederal,
            @tarjetaCirculacion,
            @numeroIMSS,
            @seguroVigente,
            @cartaPorte
        );
    ";

    using var commandCrear =
        new SqlCommand(
            consultaCrear,
            connection);

    commandCrear.Parameters.AddWithValue(
        "@nombre",
        nombre
    );

    commandCrear.Parameters.AddWithValue(
        "@telefono",
        (object?)telefono ?? DBNull.Value
    );

    commandCrear.Parameters.AddWithValue(
        "@licenciaFederal",
        (object?)licenciaFederal ?? DBNull.Value
    );

    commandCrear.Parameters.AddWithValue(
        "@tarjetaCirculacion",
        (object?)tarjetaCirculacion ?? DBNull.Value
    );

    commandCrear.Parameters.AddWithValue(
        "@numeroIMSS",
        (object?)numeroIMSS ?? DBNull.Value
    );

    commandCrear.Parameters.AddWithValue(
        "@seguroVigente",
        (object?)seguroVigente ?? DBNull.Value
    );

    commandCrear.Parameters.AddWithValue(
        "@cartaPorte",
        (object?)cartaPorte ?? DBNull.Value
    );

    return Convert.ToInt32(
        await commandCrear.ExecuteScalarAsync()
    );
}


// =========================================================
// RH-F-01-21
// GUARDAR TRANSPORTE
// =========================================================
public async Task<int> GuardarTransporteRHAsync(
    string lineaTransporte,
    string? numeroTractor,
    string? numeroRemolque1,
    string? numeroRemolque2,
    string? placasTractor,
    string? placasRemolque1,
    string? placasRemolque2)
{
    using var connection =
        _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[TRANSPORTE]
        (
            linea_transporte,
            numero_tractor,
            numero_remolque1,
            numero_remolque2,
            placas_tractor,
            placas_remolque1,
            placas_remolque2
        )
        OUTPUT INSERTED.id_transporte
        VALUES
        (
            @lineaTransporte,
            @numeroTractor,
            @numeroRemolque1,
            @numeroRemolque2,
            @placasTractor,
            @placasRemolque1,
            @placasRemolque2
        );
    ";

    using var command =
        new SqlCommand(
            consulta,
            connection);

    command.Parameters.AddWithValue(
        "@lineaTransporte",
        lineaTransporte
    );

    command.Parameters.AddWithValue(
        "@numeroTractor",
        (object?)numeroTractor ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@numeroRemolque1",
        (object?)numeroRemolque1 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@numeroRemolque2",
        (object?)numeroRemolque2 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@placasTractor",
        (object?)placasTractor ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@placasRemolque1",
        (object?)placasRemolque1 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@placasRemolque2",
        (object?)placasRemolque2 ?? DBNull.Value
    );

    return Convert.ToInt32(
        await command.ExecuteScalarAsync()
    );
}


// =========================================================
// RH-F-01-21
// GUARDAR DATOS TRANSPORTE RH
// =========================================================
public async Task GuardarDatosTransporteRHAsync(
    int idInspeccion,
    int idOperador,
    int idTransporte,
    string? origenDescarga,
    string? destinoCarga,
    string? numeroSellos,
    string? noDelivery,
    string? tipoTransporte,
    string? tipoUnidad,
    string? nivelTanque1,
    string? nivelTanque2,
    string? refrigerado,
    DateTime? fechaHoraLlegada,
    DateTime? fechaHoraSalida,
    decimal? temperatura,
    string? resultadoFinal,
    string? rampaAsignada,
    string? comentarios)
{
    using var connection =
        _connectionFactory.CreateConnection();

    await connection.OpenAsync();

    const string consulta = @"
        INSERT INTO [userchecklist].[DATOS_TRANSPORTE_RH]
        (
            id_inspeccion,
            id_operador,
            id_transporte,
            origen_descarga,
            destino_carga,
            numero_sellos,
            no_delivery,
            tipo_transporte,
            tipo_unidad,
            nivel_tanque1,
            nivel_tanque2,
            refrigerado,
            fecha_hora_llegada,
            fecha_hora_salida,
            temperatura,
            resultado_final,
            rampa_asignada,
            comentarios
        )
        VALUES
        (
            @idInspeccion,
            @idOperador,
            @idTransporte,
            @origenDescarga,
            @destinoCarga,
            @numeroSellos,
            @noDelivery,
            @tipoTransporte,
            @tipoUnidad,
            @nivelTanque1,
            @nivelTanque2,
            @refrigerado,
            @fechaHoraLlegada,
            @fechaHoraSalida,
            @temperatura,
            @resultadoFinal,
            @rampaAsignada,
            @comentarios
        );
    ";

    using var command =
        new SqlCommand(
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

    command.Parameters.AddWithValue(
        "@idTransporte",
        idTransporte
    );

    command.Parameters.AddWithValue(
        "@origenDescarga",
        (object?)origenDescarga ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@destinoCarga",
        (object?)destinoCarga ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@numeroSellos",
        (object?)numeroSellos ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@noDelivery",
        (object?)noDelivery ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tipoTransporte",
        (object?)tipoTransporte ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@tipoUnidad",
        (object?)tipoUnidad ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@nivelTanque1",
        (object?)nivelTanque1 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@nivelTanque2",
        (object?)nivelTanque2 ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@refrigerado",
        (object?)refrigerado ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@fechaHoraLlegada",
        (object?)fechaHoraLlegada ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@fechaHoraSalida",
        (object?)fechaHoraSalida ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@temperatura",
        (object?)temperatura ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@resultadoFinal",
        (object?)resultadoFinal ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@rampaAsignada",
        (object?)rampaAsignada ?? DBNull.Value
    );

    command.Parameters.AddWithValue(
        "@comentarios",
        (object?)comentarios ?? DBNull.Value
    );

    await command.ExecuteNonQueryAsync();
}


    }

}