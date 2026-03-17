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
    Cuando intento asignar el mismo código de asiento "Z1-A1" a dos posiciones diferentes
    Entonces el sistema debe mostrar un error de validación "Asiento duplicado"
    Y no debe permitir guardar la configuración

  Escenario: Notificación de error técnico al guardar
    Dado que el sistema tiene problemas de conexión con la base de datos
    Cuando intento guardar la configuración de asientos
    Entonces debo recibir una notificación de error técnico
    Y se debe ofrecer la opción de reintentar
