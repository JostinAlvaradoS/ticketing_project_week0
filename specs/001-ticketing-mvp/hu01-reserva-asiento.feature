# language: es
@HU-01 @PurchaseFlow
Característica: Selección y Reserva Temporal de Asiento
  Como Cliente
  Quiero seleccionar un asiento específico disponible en el mapa de un evento y reservarlo temporalmente
  Para asegurar que el asiento esté bloqueado a mi nombre mientras decido completar la compra.

  Antecedentes:
    Dado que existe un evento con asientos configurados
    Y el cliente está visualizando el mapa de asientos

  Escenario: Reserva exitosa de un asiento disponible
    Dado que soy un usuario autenticado
    Y que un asiento se encuentra en estado "disponible"
    Cuando el cliente selecciona dicho asiento para reservarlo
    Entonces el sistema debe confirmar la reserva y la adición al carrito del usuario de forma exitosa
    Y debe iniciar automáticamente el conteo del tiempo de reserva temporal (TTL)
    Y el asiento seleccionado debe quedar bloqueado para otros usuarios mientras el tiempo de reserva esté vigente

  Escenario: Liberación automática por expiración de tiempo (TTL)
    Dado que el cliente ya tiene un asiento en su carrito con una reserva activa
    Y el tiempo de reserva temporal permitido ha expirado sin que se complete la compra
    Cuando el sistema procesa la expiración de dicha reserva
    Entonces el asiento debe volver automáticamente al estado "disponible" en el mapa
    Y el sistema debe notificar al cliente que su reserva ha expirado por tiempo agotado

  Escenario: Intento de reserva de un asiento ya ocupado por otro cliente
    Dado que un asiento ya ha sido reservado por otro usuario previamente
    Cuando un nuevo cliente intenta seleccionar ese mismo asiento para comprarlo
    Entonces el sistema debe mostrar indisponibilidad del asiento

  Escenario: Visualización del tiempo restante
    Dado que el cliente tiene una reserva activa
    Cuando consulta el estado de su proceso
    Entonces el sistema debe mostrar un temporizador con el tiempo restante para completar la compra
