# Resultados de Auditoría de Cumplimiento — Capstone Project
> **Fecha:** 2026-04-06 | **Auditor:** Claude Code (análisis automático del repositorio)
> **Método:** Inspección directa de archivos, git log, Dockerfiles, workflows y código de tests

---

## Estado General

| Semana | Tema | Estado | Riesgo |
|--------|------|--------|--------|
| Semana 2 | TDD & Quality | 🟡 En progreso | Bajo — casi completo |
| Semana 3 | DevOps, CI/CD & Testing Multinivel | 🟡 En progreso | Medio — 2 gaps concretos |
| Semana 5 | Maestría en Automatización (POM + Screenplay) | ❌ No evaluado | **CRÍTICO** — repos externos no encontrados |
| Semana 7-8 | Examen Final: Feature + Calidad Total | 🟡 En progreso | Bajo — feature sólida, QA automation pendiente |

---

## Semana 2 — TDD & IA-Native Quality

### Entregables

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Repositorio con pruebas y código en Hexagonal | ✅ | 44 archivos de test en 8 servicios, estructura hex confirmada |
| `TESTING_STRATEGY.md` con Verificar vs. Validar | ✅ | `/docs/week3/TESTING_STRATEGY.md` — pirámide 70/15/10/5, clasificación shallow/deep, sección Verificar vs. Validar |
| Reporte automatizado de pruebas | ✅ | `coverlet.runsettings` + `.github/workflows/dotnet-test.yml` genera artefactos de cobertura en cada PR |
| Evidencia de suite al 90%+ en verde | ⚠️ | CI configurado con meta 85%, pero no hay captura de pantalla en el repo |

### Checklist Técnico

#### Integridad de la Suite

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| 100% de pruebas en verde | ⚠️ | Pipeline verde en PRs mergeados; sin captura explícita para examen |
| Mocks aíslan infraestructura correctamente | ✅ | `Moq` en todos los servicios. Ejemplo: `Mock<IWaitlistRepository>`, `Mock<ICatalogClient>` en JoinWaitlistHandlerTests — nunca toca BD real |
| Suite detectaría borrado de lógica de producción | ✅ | Tests verifican comportamiento: `entry.Status.Should().Be(WaitlistEntry.StatusAssigned)` + `_repoMock.Verify(UpdateAsync, Times.Once)` |

#### Ciclo TDD IA-Native

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Tests escritos ANTES de implementación (commits en rojo → verde) | ✅ | Comentarios explícitos `// STATUS: 🔴 RED — WaitlistEntry does not exist yet` en `WaitlistEntryTests.cs` y `JoinWaitlistHandlerTests.cs`. Ciclos 1–19 documentados. |
| No hay tests agregados al final como afterthought | ✅ | Commits de test preceden commits de implementación (`6ce31bc Add unit tests...` antes de handlers) |

#### Reportes Automatizados

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Reporte con un solo comando | ✅ | `dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"` en workflow |
| Muestra métricas de cobertura por branches | ✅ | `coverlet.runsettings` con `<Format>cobertura</Format>`, subido como artefacto y enviado a SonarCloud |

#### Verificar vs. Validar

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Al menos un test que Verifica (puerto/interfaz llamado) | ✅ | `_repoMock.Verify(x => x.UpdateAsync(entry, ...), Times.Once)` en AssignNextHandlerTests |
| Al menos un test que Valida una regla de negocio | ✅ | `JoinWaitlistHandlerTests`: stock > 0 → `WaitlistConflictException`. RN-06 validada explícitamente. |

#### Human Check

| Ítem | Estado | Nota |
|------|--------|------|
| Puedes explicar línea por línea cualquier test | 🟡 | Preparar: leer AssignNextHandlerTests y JoinWaitlistHandlerTests antes del examen |
| Puedes explicar qué dependencias se mockean y por qué | 🟡 | Respuesta clave: `ICatalogClient` se mockea porque es HTTP externo; `IWaitlistRepository` porque es BD; `IEmailService` porque es SMTP — todos son side effects fuera del dominio |

**Semana 2 — Gaps accionables:**
1. Agregar captura de pantalla del pipeline en verde al repo (evidencia visual para examen)
2. Preparar explicación oral de cada mock antes de la sustentación

---

## Semana 3 — DevOps, CI/CD y Testing Multinivel

### Entregables

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| `Dockerfile` optimizado y seguro | ⚠️ | 9 Dockerfiles encontrados. Catalog y Frontend correctos. **Inventory y Payment corren como root.** |
| YAML en `.github/workflows/` con jobs diferenciados | ✅ | 6 archivos: `orchestrator.yml`, `dotnet-build.yml`, `dotnet-test.yml`, `sonar-analysis.yml`, `trivy-scan.yml`, `system-verification.yml` |
| `TEST_PLAN.md` con 7 Principios y estructura de informe | ✅ | `/docs/week3/TEST_PLAN.md` — niveles, técnicas, matriz de HU, QA gates |
| Evidencia de pipeline en verde | ⚠️ | PRs mergeados como evidencia indirecta; no hay link explícito guardado en el repo |
| Evidencia de GitFlow con PRs y tags semánticos | ⚠️ | 8+ PRs documentados en git log, 18 branches, **PERO no hay tags de versión semántica** |

### Checklist Técnico

#### Infraestructura Inmutable y CI/CD

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Dockerfile usa imagen ligera (alpine/slim) | ✅ | `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` en Catalog, Inventory, Identity; `node:20-alpine` en Frontend |
| Dockerfile no copia `node_modules` local | ✅ | Multi-stage build: `COPY src/ .` → `dotnet restore` → `dotnet publish` |
| Dockerfile NO corre como root | ⚠️ | **Catalog ✅, Frontend ✅. Inventory ❌, Payment ❌ — sin directiva `USER`, corren como root** |
| Escaneo de vulnerabilidades de imagen | ✅ | `trivy-scan.yml` — exit code 1 en CRITICAL/HIGH; integrado en pipeline |
| Jobs separados: Componente vs. Integración | ✅ | `orchestrator.yml` define steps separados: Unit Tests → Integration Tests → Architecture Tests |
| Pipeline bloquea merge si hay fallos | ✅ | Branch Protection en GitHub: `dotnet-build.yml` y `dotnet-test.yml` como required checks |

#### Testing Multinivel y Técnicas

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Al menos una prueba de Caja Negra | ✅ | `system-e2e-test.sh` en `system-verification.yml` — prueba desde HTTP sin conocer internos |
| Evidencia de Caja Blanca (cobertura de ramas) | ✅ | Coverlet con formato cobertura + SonarCloud mide branch coverage |
| Pruebas de Componente físicamente separadas de Integración | ✅ | Directorios `/tests/unit/` y `/tests/integration/` separados por servicio |

#### TEST_PLAN.md como Informe

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Estructura de informe técnico | ✅ | Secciones formales: Alcance, Niveles, Técnicas, Matriz de Casos, QA Gates |
| Test Suites claramente definidos | ✅ | Unit / Integration-Shallow / Integration-Deep / Contract / E2E/Smoke |
| Test Cases detallados | ✅ | Matriz HU-P1, HU-P2, HU-P3 con inputs, precondiciones, resultados esperados |
| Argumenta los 7 Principios del Testing | ✅ | Sección explícita con los 7 principios aplicados al proyecto |
| Justifica Principio 6 con ejemplo propio | ✅ | "pruebas de concurrencia son críticas en Inventory pero irrelevantes en Notification" |

#### GitFlow y Release

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Features en ramas `feature/*` | ✅ | `Feature/Jostin/frontend-implement`, `Feature/Jostin/waitlist_autoassign`, `001-ticketing-microservices`, etc. |
| PR formal `develop` → `main` documentado | ✅ | `Merge pull request #81`, `#80`, `#79`... en git log |
| Release con versionado semántico (tag) | ❌ | **`git tag -l` devuelve vacío. No hay ningún tag de versión en el repositorio.** |

**Semana 3 — Gaps accionables:**
1. **Crítico:** Agregar `USER` en Inventory y Payment Dockerfiles antes del examen
2. **Crítico:** Crear tag semántico: `git tag -a v1.0.0 -m "Release: Waitlist feature" && git push origin v1.0.0`
3. Guardar link de pipeline verde como evidencia (captura o enlace en el README)

---

## Semana 5 — Maestría en Automatización (POM + Screenplay)

### Estado: NO EVALUABLE DESDE ESTE REPOSITORIO

Los 3 repositorios de automatización son **repositorios independientes** — no están dentro de este repo. La auditoría de este repo no puede confirmar su estado.

| Repo | Estado | Acción requerida |
|------|--------|-----------------|
| `AUTO_FRONT_POM_FACTORY` | ❓ Desconocido | Verificar que compila y corre 2 escenarios (positivo + negativo) |
| `AUTO_FRONT_SCREENPLAY` | ❓ Desconocido | Verificar 2 escenarios DISTINTOS a POM; estructura Actor/Task/Action/Question |
| `AUTO_API_SCREENPLAY` | ❓ Desconocido | Verificar ciclo completo POST/GET/PUT/DELETE con Serenity Rest |

### Lo que SÍ se puede confirmar desde este repo:

Este repositorio **NO referencia** los repos de automatización desde:
- README.md
- Documentación
- Workflows de CI

> ⚠️ **Riesgo CRÍTICO:** Si los 3 repos de automatización no están actualizados con la feature de Waitlist, el criterio "Maestría en Automatización" del examen final es automáticamente **Nivel Insuficiente (2.0)**.

**Semana 5 — Acciones urgentes:**
1. Verificar que los 3 repos compilan localmente HOY
2. Agregar escenarios de Waitlist a `AUTO_FRONT_POM_FACTORY` (POST /join, escenario positivo + negativo)
3. Agregar escenarios nuevos (no copias del POM) a `AUTO_FRONT_SCREENPLAY`
4. Agregar automatización de `POST /api/v1/waitlist/join` y `GET /has-pending` a `AUTO_API_SCREENPLAY`
5. Ejecutar `mvn verify serenity:aggregate` en cada repo y confirmar que el reporte HTML se genera

---

## Semana 7-8 — Examen Final: Feature + Calidad Total

### Entregables

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Feature mergeada o en PR con Arquitectura Hexagonal | ✅ | `005-waitlist-autoassign` → `develop` mergeado en PR #79 |
| Pruebas unitarias con Mocks/Stubs adecuados | ✅ | 4 archivos de test en `/services/waitlist/tests/unit/` |
| `AUTO_FRONT_POM_FACTORY` actualizado | ❓ | No evaluable — repo externo |
| `AUTO_FRONT_SCREENPLAY` actualizado | ❓ | No evaluable — repo externo |
| `AUTO_API_SCREENPLAY` actualizado | ❓ | No evaluable — repo externo |

### Checklist Técnico

#### Implementación y Arquitectura

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Feature refleja diseño de Semana 6 | ✅ | HU-01, HU-02, HU-03 y RN-01 a RN-06 implementadas. Ver `docs/general/11-waitlist-feature.md` §7 para diferencias documentadas |
| Arquitectura Hexagonal intacta | ✅ | Controllers → MediatR → Handlers → Ports → Infrastructure. Sin lógica de negocio en controllers. |
| Sin dependencias circulares | ✅ | Domain sin dependencias externas; Application solo depende de Domain; Infrastructure depende de Application |
| Principios SOLID evidentes | ✅ | DIP: 5 interfaces en Ports. SRP: cada Handler hace una cosa. ISP: interfaces específicas (no fat interfaces). |
| Cero código basura de la IA | ✅ | Ciclos TDD documentados; commits de revisión humana (`humanchcks.md`) |

#### Pruebas Unitarias de la Feature

| Ítem | Estado | Evidencia |
|------|--------|-----------|
| Alta cobertura lógica de la nueva feature | ✅ | Ciclos 1–19 cubren Domain + 3 Handlers + Worker + Consumer |
| Mocks aíslan correctamente infraestructura | ✅ | `IWaitlistRepository`, `ICatalogClient`, `IOrderingClient`, `IEmailService` → todos mockeados |
| Cubre más que Happy Paths | ✅ | Excepciones de dominio, estado inválido, cola vacía, idempotencia, servicio caído, payload v2 legacy |
| Tests como documentación viva | ✅ | Nombres descriptivos: `Handle_StockAvailable_ThrowsConflict`, `Create_WithBlankEmail_ThrowsArgumentException` |
| No hay pruebas de integración disfrazadas de unitarias | ✅ | Unit tests usan solo Mocks/Stubs; no hay `WebApplicationFactory` ni DB real en tests de waitlist |

#### Simbiosis Humano-IA — Sustentación

| Ítem | Estado | Nota para preparación |
|------|--------|----------------------|
| Decisión arquitectónica corregida sobre sugerencia IA | ✅ | La rotación cambia de `order-payment-timeout` (Kafka desde Ordering) a `WaitlistExpiryWorker` interno — decisión de bounded context |
| Puedes explicar por qué rechazaste una sugerencia de IA | ✅ | `Priority: int` eliminado → `RegisteredAt ASC` es la fuente de verdad FIFO. Documentado en `11-waitlist-feature.md §7` |
| Conectas decisiones técnicas con reglas de negocio | ✅ | RN-05 (asiento no liberado durante rotación) → `WaitlistExpiryWorker` mantiene SeatId y rota directamente |

### Formato del Examen — Preparación

#### A. Auditoría de Implementación y Unit Tests (7 min)

| Ítem | Estado | Qué navegar |
|------|--------|-------------|
| Repo listo para navegar a la feature | ✅ | `services/waitlist/` → `Application/UseCases/AssignNext/AssignNextHandler.cs` |
| Puedes mostrar un test con Mocks y explicar | ✅ | `AssignNextHandlerTests.cs` — mockea repo, ordering y email. Explicar: son side effects externos |
| Suite corre en verde desde terminal | 🟡 | Ejecutar `dotnet test services/waitlist/` antes del examen para confirmar |

#### B. Demostración de Calidad Automatizada (7 min)

| Ítem | Estado | Nota |
|------|--------|------|
| 3 reportes Serenity en vivo | ❓ | Depende de repos externos — verificar URGENTE |
| Defensa POM vs. Screenplay en tus repos | 🟡 | Preparar respuesta: POM = páginas reutilizables; Screenplay = actores/tareas/acciones, más testeables |
| SRP en una Task de Screenplay | 🟡 | Señalar en el código una Task que hace exactamente una operación |

#### C. Defensa Final de Simbiosis Humano-IA (6 min)

| Ítem | Estado | Respuesta preparada |
|------|--------|---------------------|
| Ejemplo concreto de corrección humana sobre código IA | ✅ | Cambio de `order-payment-timeout` (Kafka) a `WaitlistExpiryWorker` interno; eliminación del campo `Priority` |
| Cierre ejecutivo | ✅ | "Construí una plataforma de ticketing con 8 microservicios. Protegí la concurrencia con Redis locks. Garanticé la calidad con TDD ciclo a ciclo. La lista de espera es justa por diseño: FIFO, rotación automática, sin carreras." |

---

## Resumen Ejecutivo de Gaps

### Riesgos CRÍTICOS (pueden bajar nota a 2.0)

| # | Riesgo | Semana | Tiempo estimado para resolver |
|---|--------|--------|-------------------------------|
| 1 | **Repos de automatización (Semana 5) no verificados** — si no compilan o no tienen los nuevos escenarios, el criterio QA del examen final es 2.0 automático | 5 + 7-8 | 4–8 horas |
| 2 | **No hay tag de versión semántica** — el ítem de GitFlow está incompleto | 3 | 5 minutos |

### Riesgos MEDIOS (pueden bajar 0.5–1.0 puntos)

| # | Riesgo | Semana | Tiempo estimado |
|---|--------|--------|-----------------|
| 3 | Inventory y Payment Dockerfiles corren como root | 3 | 30 minutos |
| 4 | Sin captura de pantalla de suite en verde | 2 | 10 minutos |
| 5 | Sin link de pipeline verde guardado en repo | 3 | 5 minutos |

### Fortalezas confirmadas

| Fortaleza | Evidencia |
|-----------|-----------|
| TDD riguroso con ciclos documentados | Ciclos 1–19 en waitlist; comentarios RED/GREEN explícitos |
| CI/CD multi-capa | 6 workflows: build → test → sonar → trivy → e2e |
| Arquitectura Hexagonal mantenida en toda la feature | 5 ports, 0 lógica de negocio en controllers |
| Tests más allá de happy paths | Idempotencia, guard clauses, servicio caído, payloads legacy |
| Documentación viva | `11-waitlist-feature.md` conecta negocio → QA → DEV |
| Git workflow ordenado | 18 branches, 8+ PRs, feature branches nombradas por ticket |

---

## Plan de Acción — Ordenado por Urgencia

```
HOY (antes de 24h):
  □ 1. Verificar que los 3 repos de automatización compilan y corren
  □ 2. Agregar escenarios de Waitlist a los 3 repos
  □ 3. git tag -a v1.0.0 -m "Release Waitlist feature" && git push origin v1.0.0

ESTA SEMANA:
  □ 4. Agregar USER en Inventory/Payment Dockerfiles
  □ 5. Tomar captura del pipeline verde y guardarla en el repo
  □ 6. Ejecutar dotnet test waitlist en local y confirmar verde

ANTES DEL EXAMEN:
  □ 7. Practicar la explicación oral de AssignNextHandlerTests (7 min)
  □ 8. Ensayar el cierre ejecutivo de 2 minutos
  □ 9. Ejecutar los 3 reportes de Serenity en simulacro en vivo
```
