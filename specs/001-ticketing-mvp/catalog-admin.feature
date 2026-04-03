# language: es

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

  


  Escenario: Registro exitoso de un evento
    Dado que soy un administrador autenticado
    Y me encuentro en el módulo de gestión de eventos
    Cuando registro un nuevo evento con información válida
    Entonces debería visualizar un mensaje de confirmación de registro exitoso
    Y el evento debería aparecer en el listado de eventos
  🔴
  Escenario negativo (nombre vacío)
  Escenario: Validación de nombre obligatorio en el registro de eventos
    Dado que soy un administrador autenticado
    Y me encuentro en el módulo de gestión de eventos
    Cuando intento registrar un evento sin nombre
    Entonces debería visualizar un mensaje indicando que el nombre es obligatorio


  Escenario negativo (precio inválido)
  Escenario: Validación de precio inválido en el registro de eventos
    Dado que soy un administrador autenticado
    Y me encuentro en el módulo de gestión de eventos
    Cuando intento registrar un evento con un precio negativo
    Entonces debería visualizar un mensaje indicando que el precio debe ser mayor a cero


  Escenario de autorización
  Escenario: Restricción de acceso al módulo administrativo
    Dado que soy un usuario que se autentica
    Y no tengo permisos de administrador
    Cuando intento acceder al módulo de gestión de eventos
    Entonces debería visualizar un mensaje de acceso denegado