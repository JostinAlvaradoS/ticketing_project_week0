# 📋 FEEDBACK — Equipo 2 (TicketRush)

**Auditor**: Kelvin Vargas  
**Fecha**: 13 de febrero de 2026  
**Contexto**: Primer entregable del taller de microservicios (MVP académico).  
**Alcance**: Evaluación AI-First + estado técnico/documental con evidencia disponible en este repositorio.

---

## 1) Puntuación global (recalculada)

| Criterio | Puntaje | Peso | Ponderado |
|---|---:|---:|---:|
| 1. Estrategia de IA | 5.0 / 5 | 20% | 1.00 |
| 2. Calidad del Código & HUMAN CHECK | 2.0 / 5 | 20% | 0.40 |
| 3. Transparencia | 3.5 / 5 | 20% | 0.70 |
| 4. Arquitectura & Docker | 3.0 / 5 | 20% | 0.60 |
| 5. Git Flow & Colaboración | 5 / 5 | 20% | 1.00 |
| **TOTAL** | **3.70 / 5** | **100%** | **3.70** |

---

## 2) Evidencia y razonamiento por criterio

### 2.1 Estrategia de IA — **5.0 / 5**

**Evidencia principal**: `AI_WORKFLOW.md`.

**Por qué puntúa alto (cultura AI-First documentada)**
- Define roles IA vs Humano, reglas de oro y ciclo de trabajo.
- Documenta interacciones clave (generación, debugging iterativo, testing) con ejemplos concretos.
- Incluye un “prompt de contextualización” y convenciones (`HUMAN CHECK`, `AI-GENERATED`).

**Limitación**
- No hay registro sistemático de prompts reales por sesión (mejora de auditabilidad).

---

### 2.2 Calidad del Código & HUMAN CHECK — **2.0 / 5**

**Evidencia**: `HUMAN_CHECK_AUDIT.md`, `FEEDBACK_JHONATHAN_FRONTEND.md`, `FEEDBACK_BACKEND.md` + verificación del estado del código.

**Hallazgos que bajan el puntaje (estado actual del repo)**
- HUMAN CHECK: 7 instancias; solo 2 clasifican como “buenas” según `HUMAN_CHECK_AUDIT.md`.
- Frontend: TypeScript está deshabilitado (`frontend/next.config.mjs` mantiene `ignoreBuildErrors: true`).
- Frontend: polling de pago consulta un endpoint inexistente de Next (`frontend/hooks/use-payment-status.ts` usa `fetch(/api/tickets/{id})` en lugar de usar `frontend/lib/api.ts`).
- Backend (CRUD): creación de tickets en loop (patrón N llamadas) en `crud_service/Services/TicketService.cs`.
- Backend (CRUD): mapeo de enums a string en `crud_service/Data/TicketingDbContext.cs` (`HasConversion<string>()`), riesgoso si la BD usa enums nativos (riesgo descrito en `FEEDBACK_BACKEND.md`).

**Por qué no es 1/5**
- Hay arquitectura por capas y existe intención explícita de revisión humana (HUMAN CHECK + auditorías), aunque su calidad es inconsistente.

---

### 2.3 Transparencia (“Lo que la IA hizo mal”) — **3.5 / 5**

**Evidencia**: `AI_WORKFLOW.md`, `HUMAN_CHECK_AUDIT.md`, `FEEDBACK_BACKEND.md`, `FEEDBACK_JHONATHAN_FRONTEND.md`.

**Señales positivas**
- `AI_WORKFLOW.md` documenta debugging iterativo real y lecciones aprendidas.
- `HUMAN_CHECK_AUDIT.md` reconoce explícitamente debilidades (HUMAN CHECK débiles y faltantes).
- Auditorías de backend y frontend describen fallos con impacto y solución sugerida.

---

### 2.4 Arquitectura & Docker — **3.0 / 5**

**Evidencia**: `TECHNICAL_AUDIT.md`, `compose.yml`, `.env.example`, `README.md`.

**Lo que suma (MVP con base razonable)**
- `compose.yml` usa redes, volúmenes y variables de entorno; define healthchecks y `restart: unless-stopped`.
- Existe automatización de setup de RabbitMQ vía contenedor `rabbitmq-setup`.
- `TECHNICAL_AUDIT.md` distingue MVP vs producción, evitando exigir hardening fuera de contexto.

**Lo que resta (fragilidad / gaps de consistencia)**
- `compose.yml` referencia `RABBITMQ_HOST` para el servicio `payment`, pero `.env.example` no lo define.
- `README.md` describe versiones/stack que no coinciden con `frontend/package.json`.

---

### 2.5 Git Flow & Colaboración — **5.0 / 5**

**Evidencia**: historial git del repositorio.

**Lo que suma**
- Uso de ramas y merges vía PR (“Merge pull request #...” ).
- Commits semánticos con scope (ej.: `docs(ai-workflow): ...`, `fix(readme): ...`).



---
## 3) Conclusión

El proyecto destaca por una estrategia AI-First sólida y bien documentada. Para el **primer entregable**, el mayor riesgo es la brecha entre documentación y estado real del código (especialmente en frontend y CRUD), además de la calidad inconsistente de HUMAN CHECK.

**Puntaje final recalculado**: **3.70 / 5**.
