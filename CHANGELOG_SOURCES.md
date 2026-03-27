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

## 5to commit
- **Acción:** Documentación de pregunta de diseño
- **Descripción:**  No estoy del todo seguro de implementar la vista de despliegue para la feature, ya que realmente aun no se encuentra en etapa productiva el software por lo que realmente quedoi pendiente de validar la viabilidad y conveniencia de incluirlo.

## 2026-03-27 (Viernes)

## First Commit

- **Acción:** Se agrega el diagrama de vista lógica de feature.
- **Descripción:** el Diagrama de vista lógica nos permite ver como se van a comunicar las clases de la nueva feature.
- **Detalles:** Este diagrama es de el servicio de waitlist netamente y representa sus componentes de arquitectura hexagonal.
- **Próximos pasos:** Proceder con la redacción de los criterios de aceptación en gherkin que tenia pendientes.
- **Recomendación de IA:** No realizar los criterios de aceptación en gherkin
- **Por que no tome esa alternativa?:** Principalmente por un tema de que es más fácil que esten en gherkin para evaluar esos criterios de aceptación luego, además de que esa fue una recomendación de la última revisi´øn que tuvimos de conceptos.


## Second Commit

- **Acción:** Se refactoriza el documento para usar un formato de tablas y facilitar la lectura del mismo
- **Descripción:** En lugar de solo escribir texto plano se incluyen tablas estructuradas.
- **Detalles:** Anteriormente en otra experiencia laboral que tuve se usaba un formato similar, por lo que me estoy basando en ello para la creación del documento.


## Third Commit

- **Acción:** Se documenta requisitos funcionales y no funcionales
- **Descripción:** Me ayude de la IA para esta parte un poco, sin embargo los requisitos no funcionales tienen una métrica muy especifica que quuero validar si es viable de implementarlo como requisito.


## Fourth Commit

- **Acción:** Se toma como decisión final no incluir la vista física de la feature
- **Descripción:** Se lo toma como decisión ya que al no estar el sistema en sí en un servidor o tener múltiples ambientes aún pues se mantiene solo localhost, sería sobreingenieria incluirlo actualmente.
- **Implicación de la IA:** La IA recomendaba fuertemente incluirlo, sin embargo, esto era principalmente por la contextualización de donde esta ahora mismo construyendose el sistema.
- **Human check:** El diagrama de vista física no lo veo necesario ya que todo esta en localhost, sin embargo, no descarto por completo su implementaci´øn basado en los tiempos disponibles.


## 5TO Commit

- **Acción:** Se realizó una investigación acerca de redis y sql como cola de FIFO
- **Descripción:** Se realizó la investigaci´øn para clarificar el descarte de una alternativa.
- **Implicación de la IA:** La IA me recomendaba fuertemente incluir redis para la cola FIFO.
- **Por qué no tome esa opción:** Tras mi investigación caí en cuenta que lo que mas se suele usar es redis, pero más que nada por el enfoque una lista de espera normalmente es en tiempo real y de baja latencia, la idea es que este disponible ahi rapidamente cuando un sistema falla rapidamente un pago y demás o incluso a veces va mas por temas de balanceo de carga, para no sobrecargar el sistema de pagos se crea dicha lista de espera para reaccionar rapido cuando ya me permita acceder al servicio o pantalla, sin embargo mi enfoque es distinto, porque va mas a un contexto donde se registra en una lista de espera por si acaso un boleto se libera, es decir no es en tiempo real y podría liberarse como no ese boleto, por ello prefiero hacer un flujo mas sencillo y persistente en SQL porque a la final mas que un evento en tiempo real es un evento reactivo.
- **Referencia para la decisión:** https://dev-to.translate.goog/lazypro/message-queue-in-redis-38dm?_x_tr_sl=en&_x_tr_tl=es&_x_tr_hl=es&_x_tr_pto=sge


## 6TO Commit

- **Acción:** Se evalúo una opción arquitectonica dada por la IA
- **Descripción:** Actualmente estoy haciendo un refinamiento de las decisiones para saber si fue lo óptimo.
- **Implicación de la IA:** La IA me dio la sugerencia de hacer trigges en base de datos.
- **Por qué no tome esa opción:** Hacer triggers en base de datos me parecia que cambiaba el enfoque de kafka y además en cierto punto podía llegar a llevarme a duplicar lógica que esta presente en los servicios a nivel de .NET en la base de datos, por lo que no me pareció óptimo, mas simple, si, pero no óptimo a nivel de mantenibilidad.


## 7mo Commit

- **Acción:** Se documenta los riesgos e impacto
- **Descripción:** Hay varios riesgos, a los que los sistemas de ticketing se enfrentan comunmente, por lo que es importante tratar de documentar la mayor.ia para mitigarlos.


## 8vo Commit

- **Acción:** El riesgo de los bots registrandose en lista de espera y haciendo perder la venta a usuarios reales debe ser documentado
- **Descripción:** Es uno de los problemas actuales más graves, ya que causa diversas complicaciones
- **Implicación de la IA:** La IA me dio la solución de rate limiting, excelente, pero realmente no tomo en cuenta otra alternativa para complementar.
- **Que agregué?:** Cloudflare es el estandar para evitar y filtrar este tipo de ataques a nivel mundial, por lo que decidí incuirlo como un aspecto de mitigación.
- **Referencia para la decisión:** 
https://www.cloudflare.com/en-gb/application-services/products/bot-management/



## 9no Commit

- **Acción:** Se indagó acerca de un término que en mi anterior empleo se usaba muchísimo, las pruebas de regresión.
- **Descripción:** Todo esto surgió de una pregunta, si estoy modificando servicios también aparte del nuevo, lo lógico tambien no es volver a probarlos?
- **Conclusión:** Tras una revisión llegue al concepto de pruebas de regresión, donde se documentaba que en una nueva feature igualmente se hacen pruebas de los sistema afectados para validar que su comportamiento no haya cambiado.
- **Referencia para la decisión:** 
https://www.ibm.com/es-es/think/topics/regression-testing



## 10mo Commit

- **Acción:** Se indagó acerca de como estimar el esfuerzo por HU
- **Descripción:** Para realizar esto fue intriducido inicialmente en la clase brindada por Santiago, sin emabrgo, decidi indagar más porque la IA me genero una confusión
- **Recomendación de la IA:** La IA me dio la recomendación de estimar por componentes, tareas y demás.
- **Por qué no la acogí:** No me pareció lo idoneo, ya que según fuentes y santiago en el workshop en realidad esto se hace por HU en sí.
- **Referencia para la decisión:** https://www.youtube.com/watch?v=nERa84eHSRs&themeRefresh=1
- **Tabla de referencia para estimación:**
Tabla de equivalencia de referencia:

Talla	Puntos	Descripción
XS	1	Cambio trivial, sin lógica de negocio
S	3	Endpoint simple con validación y persistencia directa
M	5	Lógica de negocio moderada con una dependencia externa
L	13	Coordinación multi-servicio con procesamiento asíncrono
XL	21	Alta complejidad distribuida con dependencia bloqueante externa





