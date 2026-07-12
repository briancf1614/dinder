# 🧠 Learning Path — Dating App

> **Objetivo:** Construir una app de citas profesional **entendiendo cada línea de código**,
> usando un proceso confiable (SDD) y aprendiendo tecnologías reales de backend moderno.

---

## 📍 Dónde estamos ahora

**Estado actual:** Módulos 1-7 completados ✅. Seguimos con **Módulo 8 — Perfiles de Usuario**.

---

## 🗺️ Mapa de módulos

Cada módulo = un concepto nuevo + una feature chiquita que lo aplica.

| N°  | Módulo | Conceptos nuevos | Feature que construimos |
|-----|--------|------------------|------------------------|
| 1   | **Fundación** ✅ | Clean Architecture, estructura .NET, Docker | Esqueleto de la solución |
| 2   | **CQRS Core** ✅ | CQRS, MediatR, Command/Query pattern | Health check endpoint |
| 3   | **Persistencia** ✅ | EF Core, migraciones, PostgreSQL, Repository | Entidad User + migración |
| 4   | **Testing** ✅ | Unit tests, integration tests, TDD | Tests de todo lo anterior |
| 5   | **Identity** ✅ | JWT, bcrypt, FluentValidation | Register + Login + Refresh |
| 6   | **API Gateway** ✅ | nginx, reverse proxy, routing | Gateway expuesto |
| 7   | **Deploy** ✅ | GitHub Actions, CI/CD, SSH deploy, Secrets | App en producción (Oracle ARM) |
| 🔲  | ... | Progresivamente: Profiles, Discovery, Chat, etc. | ... |

### Qué significa cada columna

- **Conceptos nuevos:** Lo que vas a aprender en este módulo. Te lo explico antes de codear.
- **Feature que construimos:** Algo chico y concreto que funciona al final del módulo.

### Cómo trabaja cada módulo

1. **Charla:** Te explico el concepto (10-15 min). Por qué existe, cuándo se usa, cómo funciona.
2. **Spec:** Escribimos juntos qué tiene que hacer la feature.
3. **Design:** Te muestro el diseño técnico (qué archivos, qué patrones, por qué).
4. **Tasks:** Te paso tasks chiquitas. Vos las implementás.
5. **Review:** Reviso tu código, corrijo, explico.
6. **Verify:** Testeamos que todo funcione y que entendiste.

---

## 📝 Reglas

1. **Vos escribís el código.** Yo te digo qué y por qué, pero el teclado es tuyo.
2. **Una cosa a la vez.** No pasamos al siguiente concepto hasta que el actual esté sólido.
3. **Preguntá todo.** No hay preguntas tontas. Si no entendés algo, paramos y lo desmenuzamos.
4. **El código anterior está en `old/`.** Si querés ver cómo hicimos algo antes, lo mirás. Sirve como referencia.
5. **Sin IA generando código en masa.** Si hay que escribir una clase, la escribís vos.

---

## 📚 Conceptos que vamos a cubrir (lista completa)

### Arquitectura
- Clean Architecture (capas: Domain, Application, Infrastructure, Presentation)
- CQRS (Command Query Responsibility Segregation)
- MediatR (mediator pattern)
- API Gateway (YARP)
- Event-driven (RabbitMQ / MassTransit)
- SignalR (real-time)

### Backend .NET
- Minimal APIs vs Controllers
- Entity Framework Core + PostgreSQL
- FluentValidation
- JWT + Refresh Tokens
- Background Jobs
- Rate Limiting

### DevOps
- Docker + Docker Compose
- GitHub Actions (CI/CD)
- Environment management

### Testing
- Unit Testing (xUnit / NUnit)
- Integration Testing (TestContainers)
- TDD workflow
- Test coverage

### Frontend (más adelante)
- Angular (componentes, servicios, routing)
- HTTP Interceptors
- State management

### Mobile (más adelante)
- Kotlin / Jetpack Compose
- Ktor (HTTP client)

---

*Última actualización: empezando Módulo 1*
