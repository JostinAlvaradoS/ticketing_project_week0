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
    Dado que el asiento "A-15" está en estado "disponible"
    Cuando el cliente selecciona el asiento "A-15"
    Entonces el sistema debe marcar el asiento como "reservado"
    Y debe iniciar un temporizador de 15 minutos (TTL)
    Y el asiento debe quedar bloqueado exclusivamente para este cliente

  Escenario: Liberación automática por expiración de tiempo (TTL)
    Dado que el cliente tiene el asiento "B-10" reservado
    Y el temporizador de 15 minutos ha expirado sin completar la compra
    Cuando el sistema procesa la expiración
    Entonces el asiento "B-10" debe volver a estado "disponible"
    Y debe publicarse el evento "reservation-expired" en Kafka

  Escenario: Intento de reserva de un asiento ya reservado (Concurrencia)
    Dado que el cliente A ya tiene reservado el asiento "C-05"
    Cuando el cliente B intenta seleccionar el mismo asiento "C-05"
    Entonces el sistema debe rechazar la solicitud del cliente B
    Y debe mostrar un mensaje "El asiento ya no está disponible"

  Escenario: Visualización del tiempo restante
    Dado que el cliente tiene una reserva activa
    Cuando consulta el estado de su proceso
    Entonces el sistema debe mostrar un temporizador con el tiempo restante para completar la compra
