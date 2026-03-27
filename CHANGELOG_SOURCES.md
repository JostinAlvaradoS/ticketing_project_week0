# Changelog

Este documento es una bitácora diaria de trabajo. Aquí se registran las acciones realizadas cada día, basadas en conversaciones, clases y tareas del proyecto.

## 2026-03-25 (Miércoles)

## First Commit

- **Acción:** Creación de la plantilla guía para el documento formal en Google Docs de la nueva feature.
- **Descripción:** Se creó una plantilla destinada a servir como guía para el documento formal de la nueva funcionalidad (Google Docs). La plantilla fue elaborada basándose en las conversaciones y clases sostenidas entre el martes y el miércoles 25 de marzo.
- **Detalles de la plantilla:** estructura sugerida para el documento (Objetivo, Alcance, Requisitos funcionales y no funcionales, Diseño técnico, Flujo de datos, Plan de pruebas, Tareas/Responsables, Referencias y Enlaces). Recomendaciones sobre formato y consistencia para colaboración en Google Docs.
- **Próximos pasos:** Crear el documento en Google Docs usando esta plantilla, compartirlo y refinarlo.
- **Link:** https://docs.google.com/document/d/1Teef8YfCd141CmIf1hqBqYZGNViP_FGyHLoJf4Do_rw/edit?usp=sharing

## Second Commit

- **Acción:** Para esta parte realice multiples consultas acerca de como estructuirar correctamente una nueva feature en un sistema existente y validar el impacto que tendrá.
- **Descripción:**  Me basé bastante en mis dos últimos proyectos universitarias, tanto mi tesis como un sistema interno ya que contiene ejemplos de las vistas 4+1 de diagramas.
- **Detalles:** Los enlaces que brindaré a continuación corresponden a una investigaci´øn y aplicación exhaustiva que hice en su momento acerca de estas emtodologías.
- **Enlaces de referencia:** 
https://aslanovmustafa.medium.com/architecture-viewpoints-4-1-avm-by-kruchten-468d08b64d2d

https://github.com/DevMinds1/MantenimientoVehiculos/tree/main/Diseño%20arquitectonico

https://miro-com.translate.goog/diagramming/what-is-a-use-case-diagram/?_x_tr_sl=en&_x_tr_tl=es&_x_tr_hl=es&_x_tr_pto=tc&_x_tr_hist=true

- **Próximos pasos:** Indagar a profundidad en los enlaces de referencia para aplicar todo a la nueva feature que propondré pendiente a decisión aún.

## Third Commit

- **Acción:** Identificar correctamente las necesidades de mi sistema, que hacer actualmente, para buscar una feature adecuada que me permita realmente darme cuenta del impacto que puede llegar a tener una implementación o algo por el estilo.
- **Descripción:**  Mi sistema es actualmente un sistema de ticketing, un siustema distribuido con kafka y redis para lockear asientos, manejar reservass, ventas, etc, mi idea por ahora realmente se basa en agergar una lista de espeara.
- **Detalles:** Cuantas veces no nos ha pasado que queremos ir a un concierto o algo por el estilo y ya no hay disponibilidad de boletos, puede ser frustrante si es un evento al que de verdad queremos ir, sin embargo a veces las listas de espera nos brindan esperanza, entonces, mi idea en si es implementar ello, para que luego en caso de que alguna reserva no se concrete o algo por el estilo se libere y con FIFO cada usuario que entra a lista de espera pueda ir ocupando esos asiento equitativamente conforme se libera.
- **Próximos pasos:** Indagar a profundidad en las metodologías y buenas prácticas para partir de una HU hacía lo necesario de ir documentando previo a la solución.

## Fourt Commit

- **Acción:** Identificar y documentar en el google docs correctamente las necesidades de mi sistema, que hacer actualmente, para buscar una feature adecuada que me permita realmente darme cuenta del impacto que puede llegar a tener una implementación o algo por el estilo.
- **Descripción:**  Mi sistema es actualmente un sistema de ticketing, un siustema distribuido con kafka y redis para lockear asientos, manejar reservass, ventas, etc, mi idea por ahora realmente se basa en agergar una lista de espeara.
- **Detalles:** Cuantas veces no nos ha pasado que queremos ir a un concierto o algo por el estilo y ya no hay disponibilidad de boletos, puede ser frustrante si es un evento al que de verdad queremos ir, sin embargo a veces las listas de espera nos brindan esperanza, entonces, mi idea en si es implementar ello, para que luego en caso de que alguna reserva no se concrete o algo por el estilo se libere y con FIFO cada usuario que entra a lista de espera pueda ir ocupando esos asiento equitativamente conforme se libera.
- **Próximos pasos:** Indagar a profundidad en las metodologías y buenas prácticas para partir de una HU hacía lo necesario de ir documentando previo a la solución.

## 5to Commit

- **Acción:** Me ayude de la IA para evaluar que tan buena es la feature que quiero implementar con respecto a mi proyecto.
- **Descripción:**  Con ayuda de un agente recorri el proyecto original y me recomendaba una feature distinta.
- **Feature propuesta por la IA:** la ia proponia que lo siguiente:
    Como: Usuario comprador.
    Quiero: Que mi orden se cancele y el ticket se libere automáticamente si mi pago es rechazado o el servicio de pagos falla.
    Criterios de Aceptación: Implementación de eventos de Kafka (PaymentFailed), transacciones compensatorias en Inventory y actualización de estado en Ordering.
- **Por qué la refute?:** Creo que no es una mala feature, sin embargo, creo que no esta bien alineada al negocio, ya que estamos diciendo que si el servicio de pagos falla, saga va a liberar ese ticket, por que debería suceder esto?, de cierto modo estamos haciendo que el cliente o usuario final pueda llegar a perder su boleto que reservo por un fallo del sistema, entonces por ello la descarté, creo que es una feature que debe implementarse pero con un enfoque algo diferente.
- **Próximos pasos:** Proceder con la feature que describi anteriormente propuesta por mí ya que me pareció más adecuada y resuelve algo más real de problema de negocio.

## 2026-03-26 (Jueves)

## First Commit

- **Acción:** Tras la confirmación de alcance de la feature se procede a trabajar en base a esa.
- **Descripción:**  La feature nace en base a una limitación actual del sistema, por lo que procedemos a redactar esa documentación en el documento en google docs.
- **Detalles:** Actualmente el sistema vende los tickets, su limitante esta en no tener una forma de que tickets que se liberen se vendan luego de forma rápida.
- **Próximos pasos:** Proceder con la docuemntaci´øn completa dividida ya en épica y HU documentando todo claro en el google docs.

## Second Commit

- **Acción:** Adicionar HU a la documentación respectiva.
- **Descripción:**  En el google docs se adiciona las HU necesarias para empezar a trabajar los diagramas de la respectiva feature, desde casos de uso hasta despliegue.
- **Detalles:** Las HU se trabajan en un formato de épica y formato de Como... Quiero.... Para.....
- **Próximos pasos:** Proceder con la la documentaci´øn de los casos de USO y los criterios de aceptación de las HU.

## Third Commit

- **Acción:** Adicionar casos de Uso en el google docs
- **Descripción:**  Se adicionan los 3 casos de uso necesarios para definir correctamente la documentación de la nueva feature.
- **Detalles:** Los casos de uso fueron realizados en draw.io
- **Próximos pasos:** Proceder con el diagrama de secuencia respectivo.

## Foutrhy commit
- **Acción:** Adicionar diagramas de secuencia
- **Descripción:**  Se adicionan los diagramas de secuencia de cada flujo que habrá con el fin de tener claridad de que sistemas interactuan y como se debe implementar todo
- **Detalles:** Son un diagrama de secuencia por cada caso de uso.
- **Próximos pasos:** Proceder con la vista de desarrollo.