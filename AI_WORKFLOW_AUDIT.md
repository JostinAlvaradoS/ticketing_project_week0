# 📋 Auditoría de AI_WORKFLOW.md — Cultura AI-First

**Archivo evaluado**: `AI_WORKFLOW.md`  
**Fecha**: 12 de febrero de 2026  

---

## Calificación Global: 3.9 / 5

| Criterio | Peso | Nota | Ponderado |
|----------|------|------|-----------|
| Rol de la IA definido | 25% | 4.0 | 1.00 |
| Iteración de prompting | 25% | 3.5 | 0.88 |
| Errores de IA documentados | 25% | 4.5 | 1.13 |
| Protocolos/metodología | 25% | 3.5 | 0.88 |
| **TOTAL** | | | **3.9 / 5** |

---

## 1. ¿Define claramente el rol de la IA? — 4/5

### Lo que hace bien:
- Tabla explícita IA=Developer vs Humano=Arquitecto (sección 1.1)
- 5 reglas de oro con límites claros
- Capacidades del agente documentadas (Docker, psql, git)
- Scope definido ("alcance MVP, sin idempotencia, sin health checks propios")

### Lo que falta:
- No define **cuándo NO usar IA**. ¿Schema SQL? ¿Decisiones de exchange type? ¿compose.yml? El documento implica que todo pasa por IA, pero la tabla de decisiones (sección 7) muestra que el humano definió enums nativos, exchange topic, etc. sin la IA.
- No distingue entre "IA genera desde cero" vs "IA refactoriza código existente" vs "IA debuggea". Son interacciones muy distintas con resultados predecibles diferentes.

---

## 2. ¿Describe iteración de prompting? — 3.5/5

### Lo que hace bien:
- Ciclo visual: `Definir → Prompt → Revisar → Probar → Corregir → Commit`
- Sección 3.1 documenta fragmentación del trabajo (Consumer → Service → Repository → Tests)
- Regla "un objetivo por prompt"
- Prompt de contextualización inicial completo (sección 4.1)

### Lo que falta:
- **No hay prompts reales**. El documento tiene UN prompt ejemplo (el de contextualización), pero no muestra los prompts iterativos que causaron los fixes. ¿Cómo se le pidió a la IA que corrigiera el error `25P02`? ¿Qué prompt generó el dispatcher con `EndsWith`?
- No documenta **técnicas de prompting**: ¿se usó few-shot? ¿Se pegó el stack trace literal? ¿Se daba contexto del error o solo "arréglalo"?
- La sección 5.2 dice "pedir explicaciones" pero no muestra ejemplos de cuándo eso cambió el resultado.

---

## 3. ¿Documenta errores de la IA? — 4.5/5

### Lo que hace bien:
- Tabla de 6 bugs encadenados en Payment Service (sección 3.2) — **excelente**, con causa raíz y fix específico
- Error del `SectionId` inexistente documentado (sección 3.1)
- Admite que la IA no detectó problemas de raw SQL + change tracker
- Reconoce que la IA proponía funcionalidades fuera de scope
- Regla 4 documenta rechazo de credenciales hardcodeadas

### Lo que falta:
- Los errores documentados son solo del ReservationService y Payment Service. ¿El Producer y el CRUD Service no tuvieron errores? Si no, eso también es información relevante.
- No categoriza los errores: ¿fueron de modelo (schema incorrecto)? ¿De lógica? ¿De infraestructura? Un patrón ayudaría a predecir dónde fallará la IA en el futuro.

---

## 4. ¿Define protocolos o metodología? — 3.5/5

### Lo que hace bien:
- Workflow pre/durante/post sesión (secciones 5.1-5.3)
- Convenciones de comentarios HUMAN CHECK y AI-GENERATED (sección 6)
- Tabla de decisiones con fecha y responsable (sección 7)
- Documentos que se comparten al inicio (sección 4)

### Lo que falta:
- **No hay criterios de aceptación cuantificables**. "Revisar output" no es un protocolo. ¿Qué se revisa? ¿Compilación? ¿Tests pasan? ¿Query plan? ¿Memory leaks?
- **No define rollback**. ¿Qué pasa cuando la IA genera 3 iteraciones incorrectas seguidas? ¿Se descarta todo y se reescribe a mano? ¿Se cambia de herramienta? La sección 3.2 muestra que se iteró 6 veces, pero no dice si hubo un límite.
- **No define ownership de archivos compartidos**. `schema.sql` y `compose.yml` los mencionan como "fuente de verdad" pero ¿quién los edita? ¿La IA puede proponer cambios al schema?
- La convención `// AI-GENERATED` aparece definida pero **no existe una sola instancia** en el código real (solo hay `HUMAN CHECK`).

---

## Justificación Técnica del 3.9

**Por qué no es 5**: Un flujo AI-First maduro requiere **trazabilidad completa del prompting** — sin los prompts reales, no se puede reproducir ni auditar el proceso. El documento describe *qué se hizo* pero no *cómo se le pidió a la IA*. También falta la convención `AI-GENERATED` aplicada en el código (definida pero no usada), y no hay criterios cuantificables de aceptación.

**Por qué no es 3**: La tabla de 6 bugs encadenados es evidencia concreta de iteración humano-IA que pocos equipos documentan. El prompt de contextualización, las capacidades del agente, y el registro de decisiones muestran un proceso intencionado, no improvisado. La regla de scope (indicarle al agente qué microservicio le corresponde) es una práctica madura.

**Lo más fuerte**: Sección 3.2 (6 bugs encadenados con causa raíz y fix). Es la evidencia más honesta de cómo trabaja realmente un equipo con IA — no "la IA lo generó perfecto", sino "iteramos 6 veces hasta que funcionó".

**Lo más débil**: Ausencia total de prompts reales. Para un evaluador externo, es imposible saber si los prompts fueron sofisticados ("aquí está el stack trace, el schema y el compose; analiza la incompatibilidad de tipos entre Npgsql y PostgreSQL enum") o genéricos ("fix this error").

---

## Recomendaciones para subir a 5/5

### R1: Agregar prompts reales (impacto alto)
Incluir al menos 3 prompts textuales de sesiones de debugging reales. Ejemplo:

```
PROMPT REAL (Bug #4 - error 25P02):
"El payment consumer falla con PostgreSQL error 25P02 'current transaction is aborted'.
El stack trace apunta a TicketStateService.TransitionToPaidAsync.
Adjunto: schema.sql (enums nativos), el código del service, y el log completo.
¿El problema es que .ToString().ToLower() convierte el enum a texto plano
en vez de usar el tipo nativo de PostgreSQL?"

RESPUESTA IA: [resumen de lo que sugirió]
RESULTADO: Fix correcto, se integró.
```

### R2: Categorizar errores de la IA
Agregar una tabla resumen:

| Categoría | Errores | Ejemplo |
|-----------|---------|---------|
| Schema mismatch | 2 | SectionId inexistente, campos de DTO |
| Tipo de dato | 1 | ToString() en enum nativo |
| Concurrencia | 2 | Version pre-increment, change tracker |
| Arquitectura | 1 | Dispatcher match exacto vs EndsWith |

### R3: Definir criterios de aceptación
```
Antes de integrar código generado por IA:
□ Compila sin warnings
□ Docker Compose up exitoso
□ Flujo completo funciona (reserva → pago → verificación en BD)
□ No hay queries N+1 visibles en logs
□ HUMAN CHECK en toda lógica de concurrencia y mensajería
```

### R4: Agregar protocolo de rollback
```
Si la IA falla 3 iteraciones consecutivas en el mismo bug:
1. Parar y analizar el problema manualmente
2. Escribir el fix humano como pseudo-código
3. Pedir a la IA que implemente el pseudo-código (no que diagnostique)
```

### R5: Usar la convención AI-GENERATED que definieron
La sección 6.2 define `// AI-GENERATED` pero no hay una sola instancia en el código. Esto debilita el protocolo porque sugiere que se definió pero no se cumplió.

---

**Auditor**: Evaluación AI_WORKFLOW  
**Fecha**: 12 de febrero de 2026
