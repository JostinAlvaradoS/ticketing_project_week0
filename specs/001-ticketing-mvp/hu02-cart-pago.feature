# language: es
@HU-02 @PurchaseFlow
Característica: Agregar Asiento Reservado al Carrito y Proceso de Pago
  Como Cliente
  Quiero agregar mi asiento reservado al carrito y realizar el pago correspondiente
  Para completar la compra y asegurar el acceso al evento.

  Antecedentes:
    Dado que el cliente tiene uno o más asientos en estado "reservado"

  Escenario: Agregar asiento reservado al carrito exitosamente
    Dado que el asiento "A-15" está reservado y el TTL no ha expirado
    Cuando el cliente agrega el asiento "A-15" al carrito
    Entonces el carrito debe mostrar el asiento con su precio correspondiente

  Escenario: Error al agregar asiento con reserva expirada
    Dado que la reserva del asiento "B-20" ha expirado
    Cuando el cliente intenta agregar el asiento "B-20" al carrito
    Entonces el sistema debe mostrar un mensaje de error "La reserva ha expirado"
    Y el asiento no debe añadirse al carrito

  Escenario: Pago exitoso dentro del tiempo límite
    Dado que el cliente tiene asientos en el carrito con reserva activa
    Cuando el cliente completa el proceso de pago exitosamente
    Entonces el estado de los asientos debe cambiar a "vendido"
    Y se debe crear una orden con estado "paid"

  Escenario: Error en el pago después de expiración
    Dado que el cliente inicia el pago en el último segundo
    Y la reserva expira mientras el pago se procesaba
    Cuando el sistema de pagos intenta confirmar
    Entonces el sistema debe cancelar la transacción
    Y mostrar un mensaje de error por expiración de reserva
