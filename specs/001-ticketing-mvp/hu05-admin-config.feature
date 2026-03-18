# language: es
@HU-05 @Admin
Característica: Creación de eventos y configuración de asientos
  Como organizador
  Quiero crear eventos y configurar los asientos del lugar
  Para que se puedan vender entradas para mis eventos

  Escenario: Creación exitosa de un evento básico
    Dado que soy un organizador autenticado
    Cuando ingreso los datos del evento:
      | campo         | valor               |
      | nombre        | Festival Jazz 2026  |
      | fecha         | 2026-10-15          |
      | recinto       | Teatro Nacional     |
      | capacidad     | 500                 |
    Y defino las zonas y precios del mapa
    Entonces el evento debe guardarse exitosamente
    Y recibir una confirmación de creación

  # ========================================
  # Escenario CRUD Completo para Taller Automatización API
  # ========================================

  @CRUD @Mastery
  Escenario: Ciclo de vida completo de un evento musical mediante API (CRUD)
    Dado que el Administrador tiene acceso al servicio de Catálogo
    Cuando crea un nuevo evento con el nombre "Rock Fest 2026" y precio 150.0
    Entonces el evento debe ser creado exitosamente con un ID válido
    Y al consultar el evento por su ID el nombre debe ser "Rock Fest 2026"
    Y al actualizar el nombre del evento a "Rock Fest 2026 - Sold Out"
    Entonces el cambio debe persistirse correctamente en el sistema
    Y al desactivar el evento mediante el proceso de borrado lógico
    Entonces el evento ya no debe figurar como activo para la venta

  Escenario: Validación de unicidad de asientos al guardar
    Dado que estoy configurando el mapa de un recinto
    Cuando intento asignar el mismo código de asiento a dos posiciones diferentes
    Entonces el sistema debe mostrar un error de validación "Asiento duplicado"
    Y no debe permitir guardar la configuración

  # ========================================
  # Flujos Positivos Adicionales
  # ========================================

  Escenario: Crear evento con múltiples zonas de precios
    Dado que soy un organizador autenticado
    Cuando creo un evento con las siguientes zonas:
      | nombre        | cantidad | precio |
      | VIP           | 100      | 250.0  |
      | General       | 300      | 150.0  |
      | Balcón        | 100      | 100.0  |
    Entonces el evento debe tener 500 asientos distribuidos
    Y cada zona debe tener su precio asignado correctamente

  Escenario: Actualizar datos de un evento existente
    Dado que soy el organizador del evento "Rock Fest 2026"
    Cuando actualizo la fecha del evento a "2026-11-20"
    Entonces el cambio debe ser persistido correctamente
    Y el evento debe continuar activo

  # ========================================
  # Flujos Negativos - Validaciones de Error
  # ========================================

  Escenario: Error al crear evento sin datos obligatorios
    Dado que soy un organizador autenticado
    Cuando intento crear un evento sin especificar el nombre
    Entonces debo recibir un error "El nombre del evento es requerido"
    Y el evento no debe ser creado

  Escenario: Error al crear evento con fecha pasada
    Dado que soy un organizador autenticado
    Cuando intento crear un evento con fecha "2020-01-15"
    Entonces debo recibir un error "La fecha debe ser en el futuro"
    Y el evento no debe ser creado

  Escenario: Error al crear evento con capacidad inválida
    Dado que soy un organizador autenticado
    Cuando intento crear un evento con capacidad "0"
    Entonces debo recibir un error "La capacidad debe ser mayor a 0"
    Y el evento no debe ser creado

  Escenario: Error al crear evento sin autenticación
    Dado que no estoy autenticado
    Cuando intento crear un evento
    Entonces debo recibir un error "No autorizado" con estado 401

  Escenario: Error al actualizar evento con datos inválidos
    Dado que soy el organizador del evento "Rock Fest 2026"
    Cuando intento actualizar el precio a "-50.0"
    Entonces debo recibir un error "El precio debe ser mayor a 0"
    Y los datos originales deben mantenerse sin cambios

