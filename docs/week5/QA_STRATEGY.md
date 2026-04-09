# Estrategia de Transición SDD Multirepositorio

Este documento define el estándar para desacoplar las pruebas automatizadas (Java/Serenity) del repositorio de arquitectura (.NET), manteniendo el enfoque **Spec-Driven Development (SDD)** con GitHub Speckit.

## 1. El Portal de Negocio (Git Submodules)
Para asegurar que los repositorios de automatización siempre prueben la versión correcta del negocio, se utilizarán **Git Submodules**.

**Acción en cada repo de pruebas (Java):**
```bash
# Vincular el repositorio de arquitectura
git submodule add https://github.com/tu-usuario/ticketing_project_week0.git shared-specs
```

**Beneficios:**
- **Sincronización:** `git submodule update --remote` trae los últimos cambios en `spec.md` y `contracts/`.
- **Integridad:** Los scripts de prueba en Java referencian físicamente el Markdown original de arquitectura.

---

## 2. Flujo de Trabajo en los Repositorios de QA

Cada repositorio de automatización (`AUTO_FRONT_POM_FACTORY`, `AUTO_FRONT_SCREENPLAY`, `AUTO_API_SCREENPLAY`) seguirá este ciclo independiente:

### Paso A: Sincronización de Contexto
Antes de generar código, asegurar que la base de conocimiento de la IA esté actualizada.
- Archivo fuente: `shared-specs/specs/001-ticketing-mvp/spec.md`.
- Contratos: `shared-specs/contracts/openapi/*.yaml`.

### Paso B: Planificación de Automatización (`/speckit.plan`)
Se debe ejecutar el comando de planeación en el repo de Java con un prompt especializado:
> *"Basado en la especificación en `shared-specs/specs/001-ticketing-mvp/spec.md`, diseña un plan de pruebas bajo el patrón [POM/Screenplay] usando Serenity BDD y Java 17+."*

### Paso C: Generación de Tareas y Código
- `/speckit.tasks`: Generará el backlog de Step Definitions y Actors.
- `/speckit.implement`: Generará las clases Java respetando la Arquitectura Hexagonal del proyecto principal pero adaptada a patrones de QA.

---

## 3. Matriz de Trazabilidad
| Componente | Origen (Repo Arquitectura) | Destino (Repo QA) | Validado por |
| :--- | :--- | :--- | :--- |
| **Lógica de Negocio** | `specs/001-ticketing-mvp/spec.md` | `shared-specs/` | Todos los repos |
| **Contratos API** | `contracts/openapi/fulfillment.yaml` | `shared-specs/` | `AUTO_API_SCREENPLAY` |
| **Eventos Kafka** | `contracts/kafka/*.json` | `shared-specs/` | Pruebas de integración |

---

## 4. Estándar de Reportabilidad
Los reportes de **Serenity BDD** deben configurarse para que el nombre de los test coincida exactamente con los escenarios de aceptación definidos en el `spec.md` original, permitiendo una validación 1-a-1 entre lo diseñado y lo automatizado.
