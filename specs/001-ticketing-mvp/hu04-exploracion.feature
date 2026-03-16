# language: es
@HU-04 @Discovery
Característica: Exploración y selección de asientos para reserva en eventos
  Como visitante
  Quiero explorar el catálogo de eventos y visualizar mapas interactivos
  Para encontrar eventos de interés y seleccionar asientos

  Escenario: Filtrado de eventos en el catálogo
    Dado que existen múltiples eventos en el sistema
    Cuando el visitante filtra por categoria "Conciertos" y rango de precio "50-100"
    Entonces el sistema debe mostrar solo los eventos que cumplen con los criterios
    Y los resultados deben estar paginados

  Escenario: Visualización de disponibilidad en tiempo real en el mapa
    Dado que el visitante selecciona el evento "Rock Fest 2026"
    Cuando accede al mapa interactivo de asientos
    Entonces debe visualizar los asientos diferenciados por colores:
      | Estado     | Color esperado |
      | Disponible | Verde          |
      | Reservado  | Amarillo       |
      | Vendido    | Rojo           |

  Escenario: Restricción de máximo de asientos por usuario
    Dado que el visitante ya ha pre-seleccionado 6 asientos
    Cuando intenta seleccionar un 7mo asiento
    Entonces el sistema debe impedir la selección
    Y mostrar una notificación "Máximo 6 asientos por cliente permitido"
