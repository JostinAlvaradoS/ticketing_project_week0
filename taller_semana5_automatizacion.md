# Taller Semana 5: Maestría en Automatización – Del Objeto al Actor

## Información General
**Tema:** Implementación de patrones POM y Screenplay, gestión de pruebas de interfaz de usuario (UI) y servicios REST (API) bajo el ecosistema de Serenity BDD.

## Contexto del Reto
En el mundo del QA moderno, no basta con "hacer que el script corra". El software evoluciona y, si la automatización no es robusta, se vuelve costosa de mantener. Tras haber analizado las historias de usuario con IA, el siguiente paso es traducir esos requisitos en scripts de alta calidad que sigan estándares de industria. El reto aquí es demostrar versatilidad técnica pasando de un modelo tradicional (POM) a uno más escalable (Screenplay), cerrando con la validación de la capa de servicios.

## Dinámica
Se trabajará de forma individual y deben entregar **tres (3) proyectos de automatización distintos** (3 repositorios) que cubran los escenarios Front-end y API solicitados.

## Misión
Como ingeniero de automatización, construir un set de pruebas automatizadas individual que valide tres frentes críticos de una aplicación, asegurando que el código sea limpio, los reportes sean legibles y la arquitectura soporte cambios futuros sin romperse.

## Stack y Herramientas
1. **IDE + AI:** VS Code ó IntelliJ IDEA con GitHub Copilot.
2. **Lenguaje:** Java.
3. **Patrones:** POM y Screenplay.
4. **Framework de Automatización:** Serenity BDD.
5. **Gestión de Dependencias:** Gradle.
6. **Test Runner:** Cucumber.

---

## Flujo de Trabajo

### 1. Automatización Front: Patrón POM + Page Factory
**Objetivo:** Implementar dos escenarios de prueba independientes en una de las aplicaciones web que hayas desarrollado previamente.

*   **Patrón:** POM utilizando la anotación `@FindBy` de Page Factory.
*   **Escenarios:**
    *   Los 2 escenarios deben ser independientes entre sí (ningún test puede depender del resultado de otro).
    *   Deben incluir al menos un **escenario de flujo positivo** y al menos un **escenario de flujo negativo**.

### 2. Automatización Front: Patrón Screenplay
**Objetivo:** Crear dos escenarios nuevos e independientes bajo el patrón de Screenplay, diferentes a los automatizados con POM.

*   **Patrón:** Screenplay (Actores, Tareas, Acciones, Preguntas).
*   **Requisito:** Aplicar el principio de responsabilidad única en cada Tarea (*Task*).
*   **Restricción:** Los escenarios no pueden ser los mismos que los automatizados con POM. No se permite migrar escenarios de POM a Screenplay; deben ser escenarios nuevos.
*   **Escenarios:**
    *   Los dos escenarios deben ser independientes entre sí.
    *   Deben incluir al menos un **escenario de flujo positivo** y al menos un **escenario de flujo negativo**.

### 3. Automatización de API: Ciclo completo (CRUD)
**Objetivo:** Validar la integridad de los servicios REST de una API construida en semanas anteriores.

*   **Patrón:** Screenplay con Serenity Rest.
*   **Escenario:** Crear un único flujo de prueba que incluya 4 verbos: **POST** (crear), **GET** (consultar), **PUT** (actualizar) y **DELETE** (eliminar).
    *   *Nota:* En caso de no tener implementadas peticiones con los 4 verbos, puede repetirlos (ejemplo: 2 POST y 2 GET).

---

## Resumen de Requerimientos

| # | Tipo de Automatización | Patrón Requerido | Aplicación / Endpoint |
| :--- | :--- | :--- | :--- |
| 1 | Front-End | POM + Page Factory | Aplicación propia (2 escenarios: 1 pos, 1 neg) |
| 2 | Front-End | Screenplay | Aplicación propia (2 nuevos: 1 pos, 1 neg) |
| 3 | API Testing (CRUD) | Screenplay (Rest) | API propia (1 flujo con 4 verbos) |

---

## Entregables Finales
Deberás entregar tres enlaces de GitHub. Cada repositorio debe contener el código fuente funcional y las instrucciones de ejecución en el `README.md`.

*   **Repositorio 1:** `AUTO_FRONT_POM_FACTORY`
*   **Repositorio 2:** `AUTO_FRONT_SCREENPLAY`
*   **Repositorio 3:** `AUTO_API_SCREENPLAY`

## Criterios de Evaluación
*   **Dominio técnico:** Implementación correcta de los patrones POM y Screenplay.
*   **Arquitectura:** Uso de Gradle para la gestión de dependencias y `serenity.conf` para la configuración del driver.
*   **Gherkin:** Escritura de escenarios declarativos, evitando antipatrones y enfocada en comportamiento de negocio.
*   **Buenas prácticas de programación:**
    *   **Código limpio:** Ausencia total de código comentado u otro tipo de comentarios dentro de las clases.
    *   **Nomenclatura Semántica:** Nombres de variables, métodos y clases claros y descriptivos.

## Pautas para la Sesión de Evaluación
*   **Tiempo estimado:** 20 minutos por persona. Se ejecutará en vivo los escenarios y se revisarán los reportes generados.
*   **Entrega:** Los tres enlaces de los repositorios deben ser entregados formalmente en la sesión del día viernes.
