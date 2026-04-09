# 01 — Business Context

> **Fase SDLC:** Planificación + Análisis de Requisitos
> **Audiencia:** Negocio, Product Owner

---

## El problema

### Situación actual

Una plataforma de venta de boletos tiene un momento de alta fricción: la reserva expira. Un usuario reservó un asiento, tuvo un tiempo limitado para completar el pago y no lo hizo. El asiento queda libre.

Lo que ocurría: ese asiento volvía al mercado sin ningún criterio de equidad. El primer usuario en recargar la página y hacer clic lo obtenía — una carrera donde gana quien tiene mejor conexión o más reflejos, no quien llegó primero.

```
Asiento liberado → todos los usuarios compiten al mismo tiempo
     │
     ├── Usuario A hace clic primero (gana)
     ├── Usuario B hace clic (pierde)
     ├── Usuario C hace clic (pierde)
     └── Usuario D... ya abandonó la plataforma
```

### Consecuencias para el negocio

| Problema | Impacto |
|---------|---------|
| Percepción de injusticia | Usuarios frustrados abandonan la plataforma |
| Demanda insatisfecha sin registrar | No existe forma de saber cuántos usuarios querían comprar |
| Asientos que nunca se venden | El ciclo de reservas que expiran se repite indefinidamente |
| Pérdida de ventas | Usuarios dispuestos a comprar nunca encuentran el momento |

---

## La solución

La **Lista de Espera Inteligente** captura la demanda insatisfecha y la administra con equidad. Cuando un evento se agota, los usuarios pueden registrarse con su correo. El sistema mantiene un orden estricto de llegada — el primero en registrarse es el primero en ser atendido — y cada vez que un asiento queda disponible, se lo ofrece automáticamente al primero en la cola, con tiempo suficiente para completar el pago.

```
Asiento liberado
     │
     ▼
¿Hay alguien esperando?
     │
     ├── Sí → El primero en la cola recibe el asiento automáticamente
     │          + 30 minutos para pagar
     │          + notificación inmediata por correo
     │
     └── No → El asiento vuelve al mercado general
```

### Impacto esperado

| Antes | Después |
|-------|---------|
| Asiento libre → carrera de clics | Asiento libre → ofrecido al primero que esperó |
| Demanda insatisfecha sin registrar | Cada usuario interesado queda en la fila |
| Usuario frustrado abandona | Usuario en cola recibe notificación y actúa |
| Asientos que nadie termina de pagar | Rotación automática hasta agotar la fila |

### En una frase

> Un asiento que expira no es un asiento perdido — es una oportunidad que le pertenece a quien esperó más tiempo.

---

## Glosario del dominio

| Término | Definición |
|---------|-----------|
| **Lista de Espera** | Fila de usuarios interesados en un evento que ya no tiene boletos disponibles |
| **Entrada en Lista de Espera** | El registro de un usuario en la fila de un evento específico |
| **Orden de llegada** | El primero en registrarse en la lista es el primero en recibir un asiento |
| **Asignación Automática** | El sistema le reserva el asiento al primero de la fila sin que el usuario tenga que hacer nada |
| **Ventana de Pago** | Los 30 minutos que tiene el usuario para completar el pago una vez que el asiento le fue asignado |
| **Rotación** | Cuando el usuario asignado no paga a tiempo, el asiento pasa automáticamente al siguiente en la fila |
| **Asiento Retenido** | Mientras hay usuarios en la fila, el asiento no vuelve al mercado general — se transfiere directamente al siguiente |

---

## Reglas de negocio

| ID | Regla | Qué pasa si se viola |
|----|-------|----------------------|
| **RN-01** | Un usuario solo puede estar una vez en la lista de espera de un evento | El sistema rechaza el registro con un mensaje informativo |
| **RN-02** | No se puede entrar a la lista de espera si aún hay boletos disponibles | El sistema indica que hay boletos disponibles para compra directa |
| **RN-03** | La fila respeta el orden de llegada estricto | El primero en registrarse siempre es el primero en recibir un asiento |
| **RN-04** | El usuario asignado tiene exactamente 30 minutos para completar el pago | Si no paga en ese tiempo, pierde su turno y el asiento pasa al siguiente |
| **RN-05** | Si hay alguien más en la fila, el asiento se transfiere directamente sin volver al mercado general | Garantiza que ningún usuario externo pueda "colarse" durante el traspaso |
| **RN-06** | Si la fila está vacía cuando un asiento expira, el asiento vuelve al mercado general | El asiento queda disponible para cualquier usuario |

---

## Historias de Usuario

### HU-01 — Registro en Lista de Espera

```
Como  usuario que ve un evento agotado
Quiero  dejar mi correo para unirme a la lista de espera
Para  ser considerado automáticamente si un asiento se libera
```

**Valor de negocio:** Captura demanda insatisfecha. Sin esta historia, los usuarios que no pudieron comprar simplemente se van — y la plataforma no sabe que existieron.

---

### HU-02 — Asignación Automática

```
Como  usuario en la lista de espera
Quiero  que el sistema me asigne un asiento automáticamente cuando uno se libere
Para  asegurar mi lugar sin tener que competir nuevamente
```

**Valor de negocio:** Elimina la carrera de clics. El usuario que esperó recibe el asiento sin competencia.

---

### HU-03 — Rotación por Inacción

```
Como  plataforma de venta de boletos
Quiero  detectar cuando un usuario asignado no completa el pago a tiempo
        y ofrecer el asiento al siguiente en la fila sin perderlo en el proceso
Para  garantizar que ningún asiento quede sin venderse
      y que la equidad de la fila se mantenga durante todo el proceso
```

**Valor de negocio:** Elimina el problema de asientos que nadie termina de pagar pero tampoco están disponibles para otros.

---

## Servicios afectados

La nueva funcionalidad se añade como un componente independiente que se integra con el sistema existente sin modificar su comportamiento actual.

| Servicio | Rol |
|---------|-----|
| **Lista de Espera** (nuevo) | Gestiona la fila, detecta expirados y coordina asignaciones |
| **Catálogo** | Consultado para verificar si el evento tiene boletos disponibles antes de permitir el registro |
| **Inventario** | Notifica cuando una reserva expira; retiene el asiento mientras haya usuarios en la fila |
| **Órdenes** | Recibe la instrucción de crear una orden automática para el usuario asignado |
| **Notificaciones** | Envía el correo de confirmación y el enlace de pago al usuario asignado |
