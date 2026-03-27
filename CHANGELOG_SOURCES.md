# Changelog — Bitácora de Trabajo

Registro diario de acciones, decisiones, investigaciones e implicaciones de IA durante el desarrollo de la feature **Sistema de Lista de Espera Inteligente**. La bitácora consolida las herramientas utilizadas, los recursos de investigación consultados y las decisiones de alcance tomadas a lo largo del proceso.

> El uso de IA fue mínimo y puntual: recomendaciones y apoyo en redacción de aspectos ya definidos por mí. La mayor parte del trabajo se basó en experiencias, consultas y proyectos anteriores. No utilicé la IA para escanear factores críticos del proyecto ni nada por el estilo ya que mi idea era regirme al escenario de no tener a disposición IA por ende agentes y demás, la solución fue modelada en base al readme y los contracts definidos en la documentación.

---

## Índice

| Sección | Descripción |
|---|---|
| [Herramientas utilizadas](#herramientas-utilizadas) | Stack de herramientas empleadas durante el proceso |
| [Recursos de referencia](#recursos-de-referencia-principales) | Fuentes consultadas para investigación y decisiones |
| [Decisiones de alcance](#decisiones-de-alcance) | Decisiones clave que delimitan el documento |
| [2026-03-25 · Miércoles](#fecha-2026-03-25) | 5 commits — Plantilla, investigación 4+1, decisión de feature |
| [2026-03-26 · Jueves](#fecha-2026-03-26) | 4 commits — HU, casos de uso, diagramas de secuencia |
| [2026-03-27 · Viernes](#fecha-2026-03-27) | 10 commits — Vista lógica, requisitos, ADRs, riesgos, pruebas, estimación |

---

<a id="herramientas-utilizadas"></a>
## Herramientas utilizadas

| Herramienta | Uso |
|---|---|
| draw.io | Diagramas de casos de uso |
| Mermaid | Diagramas en código (secuencia, clases, componentes, despliegue) |
| Google Docs | Documento formal de la feature (borrador colaborativo) |
| PlantUML | Diagramas de casos de uso en formato texto |
| IA (Claude) | Recomendaciones puntuales y apoyo en redacción de secciones ya definidas |

<a id="recursos-de-referencia-principales"></a>
## Recursos de referencia principales

| Recurso | Descripción |
|---|---|
| Workshop de Santiago Posada | Principal referencia metodológica; sus diapositivas guiaron la estructura del documento |
| Tesis universitaria propia | Ejemplos aplicados de vistas 4+1 y arquitectura hexagonal |
| Sistema interno universitario | Referencia de estructura de documentación técnica formal |

<a id="decisiones-de-alcance"></a>
## Decisiones de alcance

| Decisión | Justificación |
|---|---|
| Vista de despliegue (Kruchten) no incluida | El sistema aún opera en localhost sin ambientes diferenciados; incluirla sería sobreingeniería en esta etapa |
| Criterios de aceptación en Gherkin | Facilita la evaluación posterior y fue recomendación explícita de la revisión de conceptos del curso |

---

<a id="fecha-2026-03-25"></a>
## 2026-03-25 · Miércoles

### Commit 1 — Plantilla guía en Google Docs

| Campo | Detalle |
|---|---|
| **Acción** | Creación de la plantilla guía para el documento formal de la nueva feature |
| **Descripción** | Se creó una plantilla destinada a servir como guía para el documento formal de la nueva funcionalidad en Google Docs, elaborada a partir de las conversaciones y clases del martes y miércoles 25 de marzo |
| **Contenido de la plantilla** | Objetivo, Alcance, Requisitos funcionales y no funcionales, Diseño técnico, Flujo de datos, Plan de pruebas, Tareas/Responsables, Referencias y enlaces |
| **Próximos pasos** | Crear el documento en Google Docs usando esta plantilla, compartirlo y refinarlo |
| **Enlace** | [Google Docs](https://docs.google.com/document/d/1Teef8YfCd141CmIf1hqBqYZGNViP_FGyHLoJf4Do_rw/edit?usp=sharing) |

---

### Commit 2 — Investigación de metodologías 4+1

| Campo | Detalle |
|---|---|
| **Acción** | Investigación sobre cómo estructurar correctamente una nueva feature en un sistema existente y validar su impacto |
| **Descripción** | Se tomó como base los dos últimos proyectos universitarios (tesis y sistema interno) que contienen ejemplos de las vistas 4+1 de diagramas |
| **Próximos pasos** | Indagar a profundidad en los enlaces de referencia para aplicar todo a la nueva feature |

**Referencias consultadas:**

| Recurso | Enlace |
|---|---|
| Architecture Viewpoints 4+1 (Kruchten) | [Medium](https://aslanovmustafa.medium.com/architecture-viewpoints-4-1-avm-by-kruchten-468d08b64d2d) |
| Proyecto universitario de referencia | [GitHub — MantenimientoVehiculos](https://github.com/DevMinds1/MantenimientoVehiculos/tree/main/Diseño%20arquitectonico) |
| Diagramas de casos de uso | [Miro](https://miro-com.translate.goog/diagramming/what-is-a-use-case-diagram/?_x_tr_sl=en&_x_tr_tl=es&_x_tr_hl=es&_x_tr_pto=tc&_x_tr_hist=true) |

---

### Commit 3 — Identificación de la feature

| Campo | Detalle |
|---|---|
| **Acción** | Identificar correctamente las necesidades del sistema actual para proponer una feature con impacto real de negocio |
| **Descripción** | El sistema es un ticketing distribuido con Kafka y Redis para lockeo de asientos, reservas y ventas. La idea inicial es agregar una Lista de Espera |
| **Razonamiento** | Cuando un evento se agota, los usuarios quedan sin opción. Una Lista de Espera con orden FIFO permitiría asignar equitativamente los asientos que se liberen por reservas no concretadas |
| **Próximos pasos** | Indagar en metodologías y buenas prácticas para documentar partiendo de las HU |

---

### Commit 4 — Documentación de la feature en Google Docs

| Campo | Detalle |
|---|---|
| **Acción** | Documentar formalmente la feature identificada en el commit anterior dentro del Google Docs |
| **Descripción** | Se trasladó el razonamiento y contexto de la Lista de Espera al documento formal, describiendo la limitante actual del sistema y el flujo propuesto |
| **Próximos pasos** | Proceder con la documentación dividida en épica y Historias de Usuario |

---

### Commit 5 — Evaluación de feature alternativa propuesta por IA

| Campo | Detalle |
|---|---|
| **Acción** | Evaluación de una feature alternativa sugerida por un agente de IA tras recorrer el proyecto |
| **Feature propuesta por IA** | Cancelación y liberación automática de ticket si el pago es rechazado o el servicio de pagos falla (`PaymentFailed` → transacciones compensatorias en Inventory + Ordering) |
| **Motivo del rechazo** | La feature no está bien alineada al negocio: implicaría que un fallo del sistema (no del usuario) cause la pérdida del boleto reservado. Es una feature válida pero con un enfoque diferente al correcto |
| **Próximos pasos** | Proceder con la feature de Lista de Espera propuesta, que resuelve un problema de negocio más real y tangible |

---

<a id="fecha-2026-03-26"></a>
## 2026-03-26 · Jueves

### Commit 1 — Confirmación de alcance y contexto del problema

| Campo | Detalle |
|---|---|
| **Acción** | Tras confirmación de alcance se documenta el contexto del problema en Google Docs |
| **Descripción** | La feature nace de una limitación concreta: el sistema no tiene mecanismo para que los tickets liberados se vendan rápidamente de forma equitativa |
| **Próximos pasos** | Documentar épica e Historias de Usuario en Google Docs |

---

### Commit 2 — Historias de Usuario

| Campo | Detalle |
|---|---|
| **Acción** | Adición de Historias de Usuario a la documentación en Google Docs |
| **Descripción** | Se documentaron las HU necesarias para trabajar los diagramas de la feature, en formato épica y `Como... Quiero... Para...` cumpliendo criterios INVEST |
| **Próximos pasos** | Documentar casos de uso y criterios de aceptación de las HU |

---

### Commit 3 — Casos de Uso

| Campo | Detalle |
|---|---|
| **Acción** | Adición de los 3 casos de uso al Google Docs |
| **Descripción** | Se definieron los casos de uso necesarios para cubrir correctamente la documentación de la nueva feature |
| **Herramienta** | draw.io |
| **Próximos pasos** | Proceder con los diagramas de secuencia |

---

### Commit 4 — Diagramas de secuencia y pregunta de diseño

| Campo | Detalle |
|---|---|
| **Acción** | Adición de diagramas de secuencia por cada flujo de la feature |
| **Descripción** | Un diagrama de secuencia por cada caso de uso para tener claridad de qué sistemas interactúan y cómo debe implementarse |
| **Nota de diseño** | Quedó pendiente validar la viabilidad de incluir la vista de despliegue, dado que el sistema aún no está en un servidor productivo con múltiples ambientes |
| **Próximos pasos** | Proceder con la vista de desarrollo |

---

<a id="fecha-2026-03-27"></a>
## 2026-03-27 · Viernes

### Commit 1 — Vista lógica y criterios de aceptación en Gherkin

| Campo | Detalle |
|---|---|
| **Acción** | Adición del diagrama de vista lógica de la feature y redacción de criterios de aceptación en Gherkin |
| **Descripción** | La vista lógica representa los componentes de arquitectura hexagonal del Servicio de Lista de Espera y cómo se comunican sus clases |
| **Implicación IA** | La IA recomendó no usar Gherkin para los criterios de aceptación |
| **Decisión propia** | Se mantiene Gherkin porque facilita la evaluación posterior de los criterios y fue una recomendación explícita de la última revisión de conceptos del curso |

---

### Commit 2 — Refactorización del documento a formato de tablas

| Campo | Detalle |
|---|---|
| **Acción** | Refactorización del documento para usar tablas y facilitar la lectura |
| **Descripción** | Se reemplazó el texto plano por tablas estructuradas basándose en un formato similar usado en experiencia laboral previa |

---

### Commit 3 — Requisitos funcionales y no funcionales

| Campo | Detalle |
|---|---|
| **Acción** | Documentación de requisitos funcionales y no funcionales |
| **Implicación IA** | Se usó IA de apoyo para esta sección |
| **Pendiente** | Validar si la métrica específica de los requisitos no funcionales es viable de implementar como requisito formal |

---

### Commit 4 — Decisión sobre vista física

| Campo | Detalle |
|---|---|
| **Acción** | Decisión final de no incluir la vista física de la feature en el documento principal |
| **Justificación** | El sistema está en localhost sin múltiples ambientes ni servidor productivo; incluirla en este punto sería sobreingeniería |
| **Implicación IA** | La IA recomendó fuertemente incluirla por el valor de documentación de infraestructura |
| **Human check** | El diagrama de vista física no es necesario en la etapa actual, aunque no se descarta para cuando el sistema esté en un entorno real |

---

### Commit 5 — Investigación Redis vs SQL para cola FIFO

| Campo | Detalle |
|---|---|
| **Acción** | Investigación comparativa entre Redis y PostgreSQL como almacén de la Cola de Espera FIFO |
| **Implicación IA** | La IA recomendó fuertemente Redis para la cola FIFO por su baja latencia |
| **Decisión propia** | Se eligió PostgreSQL porque el contexto de la feature no es tiempo real: un usuario se registra en la lista por si acaso un boleto se libera, no como respuesta inmediata a un evento de sistema. Es un flujo reactivo y persistente, no de tiempo real. PostgreSQL ofrece garantías ACID y capacidad de auditoría sin infraestructura adicional |
| **Referencia** | [Message Queue in Redis](https://dev-to.translate.goog/lazypro/message-queue-in-redis-38dm?_x_tr_sl=en&_x_tr_tl=es&_x_tr_hl=es&_x_tr_pto=sge) |

---

### Commit 6 — Evaluación de triggers en base de datos

| Campo | Detalle |
|---|---|
| **Acción** | Evaluación de una opción arquitectónica propuesta por la IA: triggers en base de datos |
| **Implicación IA** | La IA sugirió usar triggers en DB para reaccionar a los cambios de estado de los asientos |
| **Decisión propia** | Los triggers cambian el enfoque orientado a eventos con Kafka y duplicarían lógica de negocio que ya reside en los servicios .NET, perjudicando la mantenibilidad del sistema |

---

### Commit 7 — Riesgos e impacto

| Campo | Detalle |
|---|---|
| **Acción** | Documentación de riesgos e impacto de la feature |
| **Descripción** | Se identificaron y documentaron los riesgos más comunes en sistemas de ticketing para definir estrategias de mitigación |

---

### Commit 8 — Riesgo de bots en la Lista de Espera

| Campo | Detalle |
|---|---|
| **Acción** | Documentación del riesgo de bots registrándose en la Lista de Espera y su mitigación |
| **Implicación IA** | La IA propuso rate limiting como única solución de mitigación |
| **Aporte propio** | Se complementó con Cloudflare Bot Management, el estándar de la industria para filtrar este tipo de ataques a nivel mundial |
| **Referencia** | [Cloudflare Bot Management](https://www.cloudflare.com/en-gb/application-services/products/bot-management/) |

---

### Commit 9 — Pruebas de regresión para servicios afectados

| Campo | Detalle |
|---|---|
| **Acción** | Investigación sobre pruebas de regresión para los servicios existentes modificados por la feature |
| **Origen de la pregunta** | Si se están modificando servicios existentes (Ordering, Inventory, Notification), ¿no deberían probarse también para validar que su comportamiento previo no cambió? |
| **Conclusión** | Las pruebas de regresión son el término correcto: validan que los sistemas afectados mantienen su comportamiento original tras los cambios introducidos por la nueva feature |
| **Referencia** | [IBM — Regression Testing](https://www.ibm.com/es-es/think/topics/regression-testing) |

---

### Commit 10 — Estimación de esfuerzo por Historia de Usuario

| Campo | Detalle |
|---|---|
| **Acción** | Investigación y aplicación de estimación de esfuerzo por Historia de Usuario |
| **Origen** | Introducido en el workshop de Santiago; se profundizó porque la IA generó confusión al recomendar estimación por componentes y tareas técnicas |
| **Implicación IA** | La IA recomendó estimar por componentes, tareas e infraestructura individualmente |
| **Decisión propia** | La estimación correcta en Scrum se hace por Historia de Usuario, no por tareas técnicas. Las tareas son derivadas de la HU pero no se estiman de forma independiente en el planning |
| **Referencia** | [Planning Poker explicado](https://www.youtube.com/watch?v=nERa84eHSRs&themeRefresh=1) |

**Tabla de equivalencia usada:**

| Talla | Puntos | Descripción |
|:---:|:---:|---|
| XS | 1 | Cambio trivial, sin lógica de negocio |
| S | 3 | Endpoint simple con validación y persistencia directa |
| M | 5 | Lógica de negocio moderada con una dependencia externa |
| L | 13 | Coordinación multi-servicio con procesamiento asíncrono |
| XL | 21 | Alta complejidad distribuida con dependencia bloqueante externa |
