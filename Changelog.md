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
