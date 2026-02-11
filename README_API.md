# 🎯 API Documentation Index

## 📚 Para el Equipo Frontend

Bienvenido al sistema de ticketing. Aquí encontrarás toda la documentación necesaria para construir el frontend.

### 🚀 Comienza Aquí

1. **Primero:** Lee [FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md)
   - Setup inicial
   - Service layer ejemplos
   - Patrones comunes
   - Manejo de errores

2. **Luego:** Consulta [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
   - Todos los endpoints con ejemplos
   - Request/Response schemas
   - Status codes y errores
   - Casos de uso completos

3. **Para Referencia Rápida:** Usa [API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md)
   - Tabla de endpoints
   - Ejemplos curl
   - Validación rules
   - Error messages

4. **Especificación Técnica:** [openapi.yaml](openapi.yaml)
   - Especificación OpenAPI 3.0
   - Importa en Swagger UI
   - Machine-readable

---

## 🎯 Arquitectura en 30 Segundos

```
Frontend (React/Vue/Angular)
    ↓
┌───────────────────────────────┐
│  CRUD Service (Puerto 8002)   │
│  - GET/POST/PUT/DELETE        │
│  - PostgreSQL                 │
│  - Sincrónico                 │
└───────────────────────────────┘
    ↓
┌───────────────────────────────┐
│ Producer Service (Puerto 8001)│
│  - POST /reserve (202)        │
│  - RabbitMQ                   │
│  - Asincrónico                │
└───────────────────────────────┘
```

---

## 📋 Endpoints por Funcionalidad

### Events (Crud Service)
```
GET    /api/events               → Listar eventos
POST   /api/events               → Crear evento
GET    /api/events/{id}          → Obtener evento
PUT    /api/events/{id}          → Actualizar evento
DELETE /api/events/{id}          → Eliminar evento
```

### Tickets (CRUD Service)
```
GET    /api/tickets/event/{id}   → Listar tickets del evento
GET    /api/tickets/{id}         → Obtener ticket
POST   /api/tickets/bulk         → Crear tickets en lote
PUT    /api/tickets/{id}         → Actualizar status
```

### Reservations (Producer Service)
```
POST   /api/tickets/reserve      → Reservar ticket (ASYNC)
```

### Health Checks
```
GET    /health (CRUD)            → Estado CRUD Service
GET    /health (Producer)        → Estado Producer Service
```

---

## 💡 Flujos Comunes

### 1. Ver Eventos Disponibles
```javascript
// 1. Obtener eventos
GET /api/events (CRUD)
// 2. Mostrar lista en UI
```

### 2. Comprar Tickets
```javascript
// 1. Crear tickets para evento
POST /api/tickets/bulk (CRUD)
// 2. Reservar ticket
POST /api/tickets/reserve (PRODUCER) // 202 Accepted
// 3. Esperar confirmación (polling)
GET /api/tickets/{id} (CRUD) // Cada 500ms
// 4. Cuando status = "reserved", compra confirmada
```

### 3. Cancelar Reserva
```javascript
// 1. Obtener ticket actual
GET /api/tickets/{id} (CRUD)
// 2. Si status = "reserved", cambiar a "released"
PUT /api/tickets/{id} (CRUD) // { "newStatus": "released" }
```

---

## 🔑 Conceptos Clave

### Status Transitions (Tickets)
```
available → reserved → paid → released
    ↓          ↓        ↓        ✓ Final
    └→ cancelled ←─────┘
         ✓ Final
```

### Async Reservation (202 Accepted)
El endpoint `/api/tickets/reserve` devuelve **202**, no 200:
- Significa: "Aceptado, procesándose en el servidor"
- Necesitas **polling** para confirmar la reserva
- Espera 10-20 intentos de 500ms cada uno

### Optimistic Locking (Version Field)
Cada ticket tiene un `version`:
- Incrementa cada vez que se actualiza
- Si dos usuarios editan simultáneamente → Error 409 Conflict
- Reload y reintenta

---

## 📊 Response Format

### Success (200/201)
```json
{
  "id": 1,
  "name": "Event Name",
  ...
}
```

### Array Response
```json
[
  { "id": 1, "name": "Event 1" },
  { "id": 2, "name": "Event 2" }
]
```

### Async Accepted (202)
```json
{
  "message": "Reserva procesada",
  "ticketId": 5
}
```

### Error (400/404/500)
```
"Mensaje descriptivo del error"
```

---

## ⚠️ Errores Comunes

### Error 400: Bad Request
```
"EventId debe ser mayor a 0"
→ Validación fallida, verifica los datos de entrada
```

### Error 404: Not Found
```
"Evento 999 no encontrado"
→ El recurso no existe
```

### Error 409: Conflict
```
"Conflicto de versión. El ticket fue modificado por otro usuario."
→ Alguien más modificó el ticket, recarga e intenta nuevamente
```

### Error 500: Server Error
```
"Error al procesar la solicitud"
→ Error del servidor, reintenta más tarde
```

---

## 🛠️ Setup para Desarrollo

### 1. Clone/Pull del Repo
```bash
cd ticketing_project_week0
```

### 2. Asegúrate que los servicios corran
```bash
# Ver estado
docker-compose ps

# Si algo falla:
docker-compose up -d --build
```

### 3. Test de Health Checks
```bash
curl http://localhost:8002/health  # CRUD
curl http://localhost:8001/health  # Producer
```

### 4. Importa Postman Collection
- Archivo: `postman_collection.json`
- En Postman: Import → From File
- Configura variables de entorno

---

## 📱 Frontend Stack Recommendations

### JavaScript/TypeScript
```javascript
// Simple (Fetch API)
const response = await fetch('http://localhost:8002/api/events');
const events = await response.json();

// Mejor (Service Pattern)
import { ticketingApi } from './services/ticketingApi';
const events = await ticketingApi.getEvents();

// Production (Axios + Interceptors)
import axios from 'axios';
const api = axios.create({ baseURL: 'http://localhost:8002' });
const events = await api.get('/api/events').then(r => r.data);
```

### React Hooks Pattern
```javascript
function useEvents() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    ticketingApi
      .getEvents()
      .then(setEvents)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  return { events, loading, error };
}
```

---

## 🔐 Security Notes

### CORS
- En desarrollo: Localhost permite acceso
- En producción: Configurar CORS headers en backend

### Input Validation
- Valida **antes** de enviar a la API
- Campos requeridos: `name`, `startsAt`, `eventId`, `quantity`
- Máximos: `name` 200 chars, `orderId` 80 chars

### No Hardcodear Credenciales
- URLs de API en variables de entorno
- Credenciales nunca en frontend code

---

## 📞 Troubleshooting

### "Cannot reach API"
```bash
# Verifica que los servicios corran
docker-compose ps

# Verifica health
curl http://localhost:8002/health
curl http://localhost:8001/health

# Ver logs
docker-compose logs crud-service -f
docker-compose logs producer -f
```

### "202 pero ticket no se reserva"
- Es normal, es async
- Haz polling (GET /api/tickets/{id}) cada 500ms
- Intenta máx 20 veces (10 segundos total)

### "Version conflict (409)"
- Ticket fue modificado por otro usuario
- Recarga el ticket: `GET /api/tickets/{id}`
- Intenta actualización de nuevo

### "Event not found (404)"
- El evento no existe o fue eliminado
- Verifica el ID
- Recarga la lista de eventos

---

## 📖 Documentación Relacionada

- **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)** - Documentación completa de endpoints
- **[FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md)** - Guía de integración frontend
- **[API_QUICK_REFERENCE.md](API_QUICK_REFERENCE.md)** - Referencia rápida
- **[openapi.yaml](openapi.yaml)** - Especificación OpenAPI
- **[postman_collection.json](postman_collection.json)** - Collection de Postman
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Guía de testing

---

## ✅ Checklist Antes de Empezar

- [ ] Leíste [FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md)
- [ ] Services corren: `docker-compose ps` (all healthy)
- [ ] Health checks OK: `curl http://localhost:8002/health`
- [ ] Postman importado: `postman_collection.json`
- [ ] Service layer implementado en tu código
- [ ] Entiendes patrones async (202 + polling)
- [ ] Entiendes version field (optimistic locking)
- [ ] Tienes preguntas → Lee [API_DOCUMENTATION.md](API_DOCUMENTATION.md)

---

## 🚀 Próximos Pasos

1. **Hoy:** Setup + Leer documentación
2. **Mañana:** Implementar lista de eventos + detalles
3. **Después:** Flujo de reserva + polling
4. **Final:** Integración completa + testing

---

**Última actualización:** Febrero 10, 2026

¿Preguntas? Revisa los docs o contacta al equipo backend.

---

## Index Rápido de Documentos

| Archivo | Para Quién | Propósito |
|---------|-----------|----------|
| **FRONTEND_INTEGRATION_GUIDE.md** | Frontend Dev | Guía paso a paso de integración |
| **API_DOCUMENTATION.md** | Todos | Documentación completa de todos los endpoints |
| **API_QUICK_REFERENCE.md** | Frontend Dev | Referencia rápida durante desarrollo |
| **openapi.yaml** | API Consumers | Especificación técnica OpenAPI |
| **postman_collection.json** | QA/Testing | Collection de Postman para testing |
| **TESTING_GUIDE.md** | QA/Testing | Guía de testing completa |
| **.github/copilot-instructions.md** | Backend Dev + AI | Instrucciones para copilot sobre la arquitectura |

