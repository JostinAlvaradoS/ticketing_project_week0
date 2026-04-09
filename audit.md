# 🔍 Auditoría Consolidada de Cumplimiento — Capstone Project
> **Propósito:** Este documento es tu checklist de supervivencia para el examen final.
> Cada ítem viene directamente de las rúbricas oficiales de las semanas 2, 3, 5 y 7-8.
> Si un ítem no tiene ✅, **es un riesgo real de descuento en nota**.

---

## 📊 Estado General del Proyecto

| Semana | Tema | Estado | Nota estimada |
|--------|------|--------|----------------|
| Semana 2 | TDD & Quality | ⬜ Pendiente / 🟡 En progreso / ✅ Listo | — |
| Semana 3 | DevOps, CI/CD & Testing Multinivel | ⬜ Pendiente / 🟡 En progreso / ✅ Listo | — |
| Semana 5 | Maestría en Automatización (POM + Screenplay) | ⬜ Pendiente / 🟡 En progreso / ✅ Listo | — |
| Semana 7-8 | Examen Final: Feature + Calidad Total | ⬜ Pendiente / 🟡 En progreso / ✅ Listo | — |

> ✏️ **Instrucciones:** Reemplaza cada ⬜ con 🟡 (en progreso) o ✅ (completado). Actualiza la nota estimada según la rúbrica correspondiente.

---

## 📌 Semana 2 — TDD & IA-Native Quality

> **Criterio de aprobación mínimo (3.5):** Suite en verde, tests funcionales, reporte generado, comprensión básica de Verificar vs. Validar.

### Entregables

- [ ] **Repositorio actualizado** — Pruebas y código de producción conviven respetando Arquitectura Hexagonal.
- [ ] **`TESTING_STRATEGY.md`** — Documento que diferencia explícitamente qué se prueba para *verificar* (lógica técnica/arquitectura) y qué para *validar* (reglas de negocio).
- [ ] **Reporte automatizado de pruebas** — Generado con Jest/Mocha, JUnit/Jacoco, PyTest o xUnit. Exportado o integrado en pipeline.
- [ ] **Evidencia de suite al 90%+ en verde** — Captura de pantalla en el repositorio.

### Checklist técnico (Rúbrica)

#### Integridad de la Suite
- [ ] 100% de pruebas en verde (sin fallos, sin rojo).
- [ ] Los mocks aíslan perfectamente la infraestructura (no se llama a BD real en unit tests).
- [ ] La suite detectaría un error si se borrara lógica de producción (no es "teatro de calidad").

#### Ciclo TDD IA-Native
- [ ] Evidencia en Git de que los tests se escribieron **antes** de la implementación (commits con test en rojo → verde → refactor).
- [ ] No hay tests agregados al final como afterthought.

#### Reportes Automatizados
- [ ] El reporte se genera con **un solo comando**.
- [ ] Muestra métricas de cobertura por branches (no solo líneas).

#### Verificar vs. Validar
- [ ] Existe al menos **un test que Verifica** (ej. que un puerto/interfaz sea llamado correctamente).
- [ ] Existe al menos **un test que Valida** una regla de negocio crítica (ej. que no se permita saldo negativo, estado inválido, etc.).
- [ ] Puedes explicar la diferencia en voz alta sin dudar.

#### Human Check
- [ ] Puedes explicar línea por línea cualquier test generado por la IA en tu repositorio.
- [ ] Puedes explicar qué dependencias se mockean y **por qué**.

---

## 📌 Semana 3 — DevOps, CI/CD y Testing Multinivel

> **Criterio de aprobación mínimo (3.5):** Dockerfile funcional, pipeline que corre en PRs, TEST_PLAN.md con estructura básica, ramas de feature presentes.

### Entregables

- [ ] **`Dockerfile`** optimizado y seguro en la raíz del repositorio.
- [ ] **Archivo YAML en `.github/workflows/`** con jobs diferenciados para pruebas de Componente y de Integración.
- [ ] **`TEST_PLAN.md`** — Informe técnico formal con Test Suites, Test Plan, Test Cases, justificación de los 7 Principios del Testing y estrategia multinivel.
- [ ] **Evidencia de pipeline en verde** — Enlace directo o captura que muestre: Caja Negra, Caja Blanca, linter, análisis de vulnerabilidades de imagen y build.
- [ ] **Evidencia de GitFlow** — PRs documentados de `develop` → `main` (Release), con tag de versión semántica.

### Checklist técnico (Rúbrica)

#### Infraestructura Inmutable y CI/CD
- [ ] Dockerfile usa imagen base **ligera** (ej. alpine, slim). No copia `node_modules` local.
- [ ] Dockerfile **no corre como root**.
- [ ] Se ejecuta escaneo de vulnerabilidades de imagen (ej. `docker scan`, Trivy).
- [ ] El pipeline tiene **jobs separados** para pruebas de Componente y de Integración (no un solo step mezclado).
- [ ] El pipeline **bloquea el merge** si hay fallos (Branch Protection configurado).

#### Testing Multinivel y Técnicas
- [ ] Existe al menos **una prueba de Caja Negra** (flujo real desde API, sin conocer internos).
- [ ] Existe evidencia de **Caja Blanca** (cobertura de ramas/condiciones internas).
- [ ] Las pruebas de Componente están físicamente separadas de las de Integración (distintos archivos/folders/jobs).

#### TEST_PLAN.md como Informe
- [ ] Contiene estructura de informe técnico (no notas sueltas).
- [ ] Incluye Test Suites claramente definidos.
- [ ] Incluye Test Cases detallados.
- [ ] Argumenta los **7 Principios del Testing** aplicados al proyecto (no solo los lista).
- [ ] Justifica el Principio 6 (las pruebas dependen del contexto) con un ejemplo propio.

#### GitFlow y Release
- [ ] Todas las features viven en ramas `feature/*`.
- [ ] Existe un PR formal `develop` → `main` documentado.
- [ ] El release incluye **versionado semántico** (tag en el repositorio).

#### Human Check HITL
- [ ] Puedes distinguir oralmente cuáles de tus pruebas son de integración real vs. unitarias con dependencias mockeadas.
- [ ] Puedes explicar cómo detectas vulnerabilidades en tu imagen Docker.
- [ ] Puedes justificar cada línea del YAML que la IA generó.

---

## 📌 Semana 5 — Maestría en Automatización (POM + Screenplay)

> **Criterio de aprobación mínimo:** Los 3 repositorios entregan código funcional, los escenarios corren y los reportes de Serenity se generan.

### Entregables (3 repositorios obligatorios)

- [ ] **`AUTO_FRONT_POM_FACTORY`** — Repositorio con automatización Front usando POM + Page Factory (`@FindBy`). README con instrucciones de ejecución.
- [ ] **`AUTO_FRONT_SCREENPLAY`** — Repositorio con automatización Front usando patrón Screenplay. README con instrucciones de ejecución.
- [ ] **`AUTO_API_SCREENPLAY`** — Repositorio con automatización API CRUD usando Screenplay + Serenity Rest. README con instrucciones de ejecución.

### Checklist técnico por repositorio

#### AUTO_FRONT_POM_FACTORY
- [ ] Implementa exactamente **2 escenarios independientes** (ninguno depende del resultado del otro).
- [ ] Al menos **1 escenario de flujo positivo** y **1 de flujo negativo**.
- [ ] Usa `@FindBy` de Page Factory correctamente.
- [ ] Cero código comentado en las clases.
- [ ] Nomenclatura semántica en variables, métodos y clases.
- [ ] Gestión de dependencias con **Gradle**.
- [ ] Configuración del driver en `serenity.conf`.
- [ ] Escenarios Gherkin declarativos (comportamiento de negocio, sin detalles de implementación).

#### AUTO_FRONT_SCREENPLAY
- [ ] Implementa exactamente **2 escenarios nuevos** (distintos a los del POM, no migraciones).
- [ ] Al menos **1 escenario positivo** y **1 negativo**, independientes entre sí.
- [ ] Estructura correcta: **Actores, Tareas, Acciones, Preguntas**.
- [ ] Cada **Task aplica el Principio de Responsabilidad Única** (hace una sola cosa).
- [ ] Cero código comentado.
- [ ] Nomenclatura semántica.
- [ ] Gestión con Gradle + `serenity.conf`.

#### AUTO_API_SCREENPLAY
- [ ] Implementa **1 escenario de ciclo completo** con los 4 verbos: `POST`, `GET`, `PUT`, `DELETE`.
- [ ] Usa **Screenplay con Serenity Rest**.
- [ ] Si el backend no tiene los 4 verbos, los verbos se repiten justificadamente (ej. 2 POST + 2 GET).
- [ ] Cero código comentado.
- [ ] Nomenclatura semántica.
- [ ] README con instrucciones claras de ejecución.

#### Criterios transversales (aplican a los 3 repos)
- [ ] Los reportes de Serenity se generan correctamente y son legibles.
- [ ] Puedes ejecutar los escenarios **en vivo** sin errores (simulacro antes del examen).
- [ ] Puedes explicar en vivo el patrón Screenplay vs. POM y cuándo usar cada uno.

---

## 📌 Semanas 7-8 — Examen Final: Feature + Calidad Total

> **Criterio de aprobación mínimo (3.5):** Feature funcional adaptada al diseño de Semana 6, pruebas unitarias en verde, 3 repos de QA actualizados y funcionando, sustentación básica.

### Entregables

- [ ] **Repositorio Core** (`feature/*` mergeada o en PR abierto) — Feature implementada en Arquitectura Hexagonal con principios SOLID.
- [ ] **Pruebas Unitarias y de Integración** dentro del repositorio Core, con Mocks/Stubs adecuados.
- [ ] **`AUTO_FRONT_POM_FACTORY` actualizado** — Nuevos escenarios E2E cubriendo la nueva feature.
- [ ] **`AUTO_FRONT_SCREENPLAY` actualizado** — Nuevos escenarios E2E cubriendo la nueva feature con Screenplay.
- [ ] **`AUTO_API_SCREENPLAY` actualizado** — Automatización de los nuevos endpoints de la feature.
- [ ] **Sustentación oral lista** — Sin archivo MD adicional requerido; la evaluación es en vivo.

### Checklist técnico (Rúbrica)

#### Implementación y Arquitectura (Dev)
- [ ] La feature implementada **refleja el diseño de la Semana 6** (no una solución inventada de cero).
- [ ] La Arquitectura Hexagonal se mantiene intacta (sin acoplamiento directo a BD ni romper puertos/adaptadores).
- [ ] Sin dependencias circulares.
- [ ] Principios SOLID evidentes y explicables.
- [ ] Cero "código basura" de la IA pegado sin revisión.

#### Pruebas Unitarias (Dev)
- [ ] Alta cobertura lógica de la **nueva feature específicamente** (no solo del código existente).
- [ ] Los Mocks aíslan correctamente la infraestructura (sin llamadas reales a BD en unit tests).
- [ ] Las pruebas cubren **más que solo Happy Paths**: incluyen validaciones de reglas de negocio y casos de error.
- [ ] Las pruebas sirven como documentación viva (nombres descriptivos, estructura clara).
- [ ] No hay pruebas de integración disfrazadas de unitarias por mal uso de Mocks.

#### Maestría en Automatización QA (los 3 repos)
- [ ] Los 3 repositorios compilan sin errores.
- [ ] Las pruebas existentes siguen pasando (no se rompió nada al agregar la nueva feature).
- [ ] Las nuevas automatizaciones cubren específicamente la nueva feature.
- [ ] Las tareas y páginas nuevas son reusables y declarativas (no código duplicado para la transición).
- [ ] Los reportes de Serenity se generan correctamente con los nuevos escenarios.

#### Simbiosis Humano-IA — Sustentación (6 min)
- [ ] Puedes mostrar **una decisión arquitectónica que corregiste sobre lo que sugirió la IA** (con evidencia en el código o en Git).
- [ ] Puedes explicar por qué rechazaste o modificaste una sugerencia específica de la IA.
- [ ] Puedes conectar las decisiones técnicas con las reglas de negocio (visión Dev + QA + Negocio integrada).
- [ ] Durante el interrogatorio, no dependes de releer el código para explicarlo.

### Formato del Examen — Preparación por bloque

#### A. Auditoría de Implementación y Unit Tests (7 min)
- [ ] Tienes preparado el repositorio Core para navegar directamente a la nueva feature.
- [ ] Puedes mostrar un test con Mocks y explicar qué se mockea y por qué.
- [ ] Puedes demostrar que la suite corre en verde desde la terminal en vivo.

#### B. Demostración de Calidad Automatizada (7 min)
- [ ] Los 3 reportes de Serenity se generan en vivo sin errores.
- [ ] Puedes defender la diferencia entre POM y Screenplay en tus propios repositorios.
- [ ] Puedes señalar en el código dónde se aplica el Principio de Responsabilidad Única en una Task.

#### C. Defensa Final de Simbiosis Humano-IA (6 min)
- [ ] Tienes preparado **un ejemplo concreto** de corrección humana sobre código IA.
- [ ] Puedes dar un cierre ejecutivo: qué construiste, cómo lo protegiste, qué garantiza su calidad.

---

## 🔗 Mapa de Dependencias — Qué alimenta qué

```
Semana 2 (TDD + Unit Tests)
    └──► Semana 7-8: Las pruebas unitarias del examen final deben seguir
         el mismo estándar (Mocks correctos, Verificar vs. Validar,
         cobertura de reglas de negocio).

Semana 3 (Docker + CI/CD + GitFlow)
    └──► Semana 7-8: El pipeline existente debe correr la nueva feature.
         GitFlow sigue siendo obligatorio (feature/* → PR).

Semana 5 (3 Repositorios de Automatización)
    └──► Semana 7-8: Los MISMOS 3 repos deben actualizarse para cubrir
         la nueva feature. Si los repos base están rotos, el examen
         final se cae entero.
```

> ⚠️ **Riesgo crítico:** Si los repositorios de la Semana 5 no compilan o tienen arquitectura rota, el criterio de "Maestría en Automatización" del examen final es automáticamente **Nivel Insuficiente (2.0)**, sin importar qué tan bien implementes la feature.

---

## 🚨 Ítems de Alto Riesgo — Revisa esto primero

Estos son los errores más comunes que bajan la nota de 3.5 a 2.0:

| Riesgo | Semana | Señal de alerta |
|--------|--------|-----------------|
| Tests que siempre pasan sin evaluar nada ("teatro de calidad") | 2 | `expect(true).toBe(true)` o asserts vacíos |
| Dockerfile corriendo como root | 3 | `USER root` o ausencia de instrucción `USER` |
| Escenarios de Screenplay copiados del POM | 5 | Mismos nombres/flujos en ambos repos |
| Tasks de Screenplay que hacen más de una cosa | 5 | Tasks con 5+ acciones no relacionadas |
| Feature que rompe la Arquitectura Hexagonal | 7-8 | Lógica de negocio en controladores o acceso a BD directo |
| No saber explicar un Mock durante la sustentación | 7-8 | Revisar cada test antes del examen |
| Código de la IA pegado sin revisión | 7-8 | Buscar en el código patrones que no entiendes |

---

*Documento generado para auditoría personal del Capstone Project — Mid Level Track.*
*Actualiza los checkboxes conforme avances. Un ítem sin marcar el día del examen es un riesgo conocido.*