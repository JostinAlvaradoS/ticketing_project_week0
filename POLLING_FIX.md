# Fix: Frontend Polling Infinite Loop

## Problem
El hook `useReservationStatus` estaba en un bucle infinito cuando el sistema de reservas no estaba disponible:
- Iniciaba polling cada 500ms
- A los 8 segundos debería llamar `onQueued()` y parar
- Pero continuaba haciendo polling indefinidamente

## Root Cause
La variable `queuedCalled` era local al effect, lo que causaba:
1. Cada re-render creaba una nueva variable `queuedCalled = false`
2. El flag nunca se persistía entre renders
3. El polling nunca se detenía realmente cuando alcanzaba `queueTime`
4. El componente seguía re-renderizando porque `isPolling` se mantenía en `true`

## Solution

### 1. **useReservationStatus Hook** (`/frontend/hooks/use-reservation-status.ts`)

**Cambio clave**: Mover `queuedCalled` a estado React:
```typescript
const [queuedCalled, setQueuedCalled] = useState(false)
```

**Lógica mejorada**:
- Cuando se alcanza `queueTime` (8 segundos):
  - `setQueuedCalled(true)` → persiste entre renders
  - `setIsPolling(false)` → DETIENE EL POLLING INMEDIATAMENTE
  - `clearInterval(pollInterval)` → limpia el intervalo
  - `onQueued()` → notifica al componente
  - `return` → sale de la función

```typescript
if (!queuedCalled && elapsed > queueTime) {
  setQueuedCalled(true)
  setIsPolling(false) // ← Detiene aquí mismo
  if (pollInterval) clearInterval(pollInterval)
  onQueued?.()
  return
}
```

**Reset de estado**:
- `startPolling()` ahora reseta `queuedCalled` para permitir reintentos:
```typescript
startPolling: () => {
  setQueuedCalled(false) // Reset para poder intentar de nuevo
  setIsPolling(true)
}
```

### 2. **Buy Page** (`/frontend/app/buy/[id]/page.tsx`)

**Capturar y usar `stopPolling()`**:
```typescript
const { 
  isPolling: isValidatingReservation, 
  startPolling: startValidatingReservation, 
  stopPolling: stopValidatingReservation  // ← Nueva función
} = useReservationStatus({...})
```

**En los callbacks, detener explícitamente**:
```typescript
onQueued: () => {
  stopValidatingReservation() // ← Asegura que se detiene
  setStep("error")
  setErrorMsg("Sistema temporalmente indisponible...")
}

onReservationFailed: (reason) => {
  stopValidatingReservation() // ← Asegura que se detiene
  setStep("error")
  setErrorMsg(reason)
}
```

## Resultado

**Flujo de ejecución ahora**:
1. Usuario hace clic en "Comprar"
2. Inicia validación: `startValidatingReservation()`
3. Hook empieza polling cada 500ms
4. **A los 8 segundos** (si no está confirmada):
   - `queuedCalled` = `true` (persistente)
   - `isPolling` = `false` (detiene el hook)
   - Intervalo se limpia
   - `onQueued()` se ejecuta
   - Página cambia a step="error"
   - Usuario ve mensaje claro
   - **Polling DETIENE**

5. Usuario puede hacer clic en "Intentar de nuevo":
   - `startPolling()` reseta `queuedCalled`
   - Ciclo puede volver a empezar

## Testing

Para validar:
1. Apagar el Producer service o CRUD service
2. Hacer clic en "Comprar"
3. Verificar que en 8 segundos:
   - Aparece mensaje "Sistema temporalmente indisponible"
   - **No hay más peticiones al backend** (ver Network tab)
   - Polling realmente detiene
4. Volver a encender el servicio
5. Hacer clic en "Intentar de nuevo"
6. Verificar que polling reinicia correctamente

## Benefits

✅ No más bucle infinito
✅ Mejor UX: usuario sabe qué pasó en 8 segundos
✅ Menor carga en el servidor: no hace polling innecesario
✅ Permite reintentos limpiamente
✅ Código más fácil de debuggear
