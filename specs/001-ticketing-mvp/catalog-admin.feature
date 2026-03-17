# language: es
Característica: Administración de Catálogo de Eventos
  Como administrador del sistema de ticketing
  Quiero gestionar eventos y asientos desde una interfaz administrativa
  Para configurar la oferta de eventos disponibles para la venta

  Antecedentes:
    Dado que existe un servicio de Catalog corriendo
    Y existe base de datos "bc_catalog" con tablas "Events" y "Seats" 
    Y tengo credenciales de administrador válidas
    Y el sistema de autenticación está configurado con política "RequireRole('Admin')"

  # ========================================
  # Escenario CRUD Completo para Taller Automatización
  # ========================================

  @CRUD @Mastery
  Escenario: Ciclo de vida completo de un evento musical (CRUD)
    Dado que el Administrador tiene acceso al servicio de Catálogo
    Cuando crea un nuevo evento con el nombre "Rock Fest 2026" y precio 150.0
    Entonces el evento debe ser creado exitosamente con un ID válido
    Y al consultar el evento por su ID el nombre debe ser "Rock Fest 2026"
    Y al actualizar el nombre del evento a "Rock Fest 2026 - Sold Out"
    Entonces el cambio debe persistirse correctamente en el sistema
    Y al desactivar el evento mediante el proceso de borrado lógico
    Entonces el evento ya no debe figurar como activo para la venta

  # ========================================
  # Escenario CRUD Completo para Taller Automatización
  # ========================================

  @CRUD @Mastery
  Escenario: Ciclo de vida completo de un evento musical (CRUD)
    Dado que el Administrador tiene acceso al servicio de Catálogo
    Cuando crea un nuevo evento con el nombre "Rock Fest 2026" y precio 150.0
    Entonces el evento debe ser creado exitosamente con un ID válido
    Y al consultar el evento por su ID el nombre debe ser "Rock Fest 2026"
    Y al actualizar el nombre del evento a "Rock Fest 2026 - Sold Out"
    Entonces el cambio debe persistirse correctamente en el sistema
    Y al desactivar el evento mediante el proceso de borrado lógico
    Entonces el evento ya no debe figurar como activo para la venta

  # ========================================
  # Escenarios para Task T100/T101 - Creación de eventos
  # ========================================

  Escenario: Crear evento válido (T101)
    Dado que soy un administrador autenticado
    Cuando creo un evento con los siguientes datos:
      | name | Concierto Foo Fighters 2026 |
      | description | Concierto en el Estadio Nacional |
      | event_date | 2026-06-15T20:00:00Z |
      | venue | Estadio Nacional |
      | max_capacity | 50000 |
      | base_price | 120.50 |
    Entonces el evento se crea exitosamente
    Y el evento tiene un ID generado automáticamente
    Y el evento está en estado "active"
    Y la respuesta contiene los datos del evento creado

  Escenario: Crear evento con datos inválidos - nombre vacío (T101)
    Dado que soy un administrador autenticado
    Cuando intento crear un evento con los siguientes datos:
      | name | |
      | description | Evento sin nombre |
      | event_date | 2026-06-15T20:00:00Z |
      | base_price | 120.50 |
    Entonces la creación del evento falla
    Y recibo un error de validación "El nombre del evento es obligatorio"
    Y NO se persiste ningún registro en la base de datos

  Escenario: Crear evento con fecha pasada (T101)
    Dado que soy un administrador autenticado
    Cuando intento crear un evento con los siguientes datos:
      | name | Evento Pasado |
      | description | Evento en el pasado |
      | event_date | 2020-01-01T20:00:00Z |
      | base_price | 50.00 |
    Entonces la creación del evento falla
    Y recibo un error de validación "La fecha del evento debe ser futura"

  Escenario: Crear evento con precio negativo (T101)
    Dado que soy un administrador autenticado
    Cuando intento crear un evento con los siguientes datos:
      | name | Evento Gratis |
      | description | Evento con precio inválido |
      | event_date | 2026-06-15T20:00:00Z |
      | base_price | -10.00 |
    Entonces la creación del evento falla
    Y recibo un error de validación "El precio base debe ser mayor a cero"

  # ========================================
  # Escenarios para Task T102 - Command Handler
  # ========================================

  Escenario: Procesar comando CreateEvent a través de MediatR (T102)
    Dado que tengo un CreateEventCommand válido
    Cuando el CreateEventCommandHandler procesa el comando
    Entonces se invoca el repositorio para persistir el evento
    Y se publica un evento de dominio "EventCreated"
    Y se retorna un CreateEventResponse con el ID del evento creado

  # ========================================
  # Escenarios para Task T103 - Generación masiva de asientos
  # ========================================

  Escenario: Generar asientos masivos para un evento (T103)
    Dado que existe un evento creado con ID "11111111-1111-1111-1111-111111111111"
    Cuando genero asientos masivos con la siguiente configuración:
      | sections | A,B,C |
      | rows_per_section | 50 |
      | seats_per_row | 20 |
      | base_price_multiplier | 1.0 |
    Entonces se crean 3000 asientos en total (3 secciones × 50 filas × 20 asientos)
    Y todos los asientos tienen estado "available"
    Y los asientos están numerados correctamente:
      | sección | A | filas 1-50 | asientos 1-20 |
      | sección | B | filas 1-50 | asientos 1-20 |
      | sección | C | filas 1-50 | asientos 1-20 |

  Escenario: Generar asientos con diferentes precios por sección (T103)
    Dado que existe un evento creado con base_price "100.00"
    Cuando genero asientos con configuración de precios:
      | section | rows | seats_per_row | price_multiplier |
      | VIP     | 5    | 10            | 2.0              |
      | GOLD    | 10   | 15            | 1.5              |
      | REGULAR | 20   | 25            | 1.0              |
    Entonces se crean asientos con los siguientes precios:
      | VIP     | 200.00 | 50 asientos  |
      | GOLD    | 150.00 | 150 asientos |
      | REGULAR | 100.00 | 500 asientos |

  Escenario: Error al generar asientos para evento inexistente (T103)
    Dado que NO existe un evento con ID "99999999-9999-9999-9999-999999999999"
    Cuando intento generar asientos masivos para ese evento
    Entonces la operación falla
    Y recibo un error "Evento no encontrado"
    Y NO se crean asientos

  # ========================================
  # Escenarios para Task T104 - Endpoints de Admin protegidos
  # ========================================

  Escenario: Acceso autorizado a endpoints de admin (T104)
    Dado que tengo un token JWT con role "Admin"
    Cuando hago una petición POST a "/admin/events"
    Entonces la petición es autorizada
    Y puedo crear el evento

  Escenario: Acceso denegado sin credenciales de admin (T104)
    Dado que tengo un token JWT con role "Customer"
    Cuando intento hacer una petición POST a "/admin/events"
    Entonces recibo un error 403 Forbidden
    Y la petición es rechazada

  Escenario: Acceso denegado sin token (T104)
    Dado que NO tengo token de autenticación
    Cuando intento hacer una petición POST a "/admin/events"
    Entonces recibo un error 401 Unauthorized
    Y la petición es rechazada

  # ========================================
  # Escenarios para Task T105 - Integration Test completo
  # ========================================

  Escenario: Flujo completo Admin - Crear Evento → Generar Asientos → Verificar Read Model (T105)
    Dado que soy un administrador autenticado con Testcontainers ejecutándose
    Cuando creo un evento "Rock Festival 2026"
    Y genero 1000 asientos distribuidos en 4 secciones
    Entonces puedo consultar el evento a través del endpoint público GET /events/{id}/seatmap
    Y la respuesta contiene:
      | event_id | coincide con el ID del evento creado |
      | event_name | Rock Festival 2026 |
      | seats | array de 1000 asientos |
      | all_seats_status | available |
    Y cada asiento tiene:
      | id | GUID válido |
      | section_code | A, B, C, o D |
      | row_number | 1-25 |
      | seat_number | 1-10 |
      | price | precio calculado según sección |
      | status | available |

  # ========================================
  # Escenarios para Task T106 - Actualización y Desactivación
  # ========================================

  Escenario: Actualizar información de evento existente (T106)
    Dado que existe un evento con ID "22222222-2222-2222-2222-222222222222"
    Cuando actualizo el evento con los siguientes cambios:
      | name | Concierto Foo Fighters 2026 - SOLD OUT |
      | description | Evento agotado - últimas entradas |
      | max_capacity | 45000 |
    Entonces el evento se actualiza exitosamente
    Y los cambios se persisten en la base de datos
    Y la fecha del evento NO puede ser modificada
    Y el base_price NO puede ser modificado si ya existen reservas

  Escenario: Desactivar evento (Soft Delete) (T106)
    Dado que existe un evento activo con ID "33333333-3333-3333-3333-333333333333"
    Y el evento NO tiene reservas activas ni boletos vendidos
    Cuando desactivo el evento
    Entonces el evento cambia a estado "inactive"
    Y la fecha de desactivación se registra
    Y el evento NO aparece en consultas públicas de eventos activos
    Y el evento sigue siendo accesible para administradores
    Y todos los asientos asociados cambian a estado "unavailable"

  Escenario: Intentar desactivar evento con reservas activas (T106)
    Dado que existe un evento con ID "44444444-4444-4444-4444-444444444444"
    Y el evento tiene 5 reservas activas
    Cuando intento desactivar el evento
    Entonces la operación falla
    Y recibo un error "No se puede desactivar un evento con reservas activas"
    Y el evento permanece en estado "active"

  Escenario: Reactivar evento desactivado (T106)
    Dado que existe un evento en estado "inactive"
    Y la fecha del evento es futura
    Cuando reactivo el evento
    Entonces el evento vuelve a estado "active"
    Y aparece nuevamente en consultas públicas
    Y los asientos vuelven a estado "available"

  # ========================================
  # Escenarios de Edge Cases y Validaciones
  # ========================================

  Escenario: Prevenir creación de eventos duplicados por nombre y fecha
    Dado que existe un evento "Metal Fest 2026" programado para "2026-08-15T21:00:00Z"
    Cuando intento crear otro evento con el mismo nombre y fecha
    Entonces la creación falla
    Y recibo un error "Ya existe un evento con ese nombre en la misma fecha"

  Escenario: Validar capacidad máxima vs asientos generados
    Dado que creo un evento con max_capacity "100"
    Cuando intento generar 150 asientos
    Entonces la operación falla
    Y recibo un error "La cantidad de asientos excede la capacidad máxima del evento"

  Escenario: Búsqueda y filtrado de eventos para administradores
    Dado que existen múltiples eventos en diferentes estados
    Cuando consulto la lista de eventos como administrador
    Entonces puedo filtrar por:
      | estado | active, inactive, cancelled |
      | fecha | rango de fechas |
      | venue | nombre del venue |
    Y puedo ver eventos inactivos que no aparecen al público general