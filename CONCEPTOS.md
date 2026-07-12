# 🧠 Conceptos Aprendidos — Dinder

> Cada vez que aprendemos algo nuevo, lo anotamos acá con una explicación corta
> y un ejemplo de NUESTRO código. Así cuando te olvides, leés esto y listo.

---

## Módulo 1 — Clean Architecture

### Qué significa "arquitectura" en este proyecto

La solución tiene 7 proyectos, pero es **UN SOLO servicio** (UN solo proceso corriendo).
Los proyectos están separados para organizar el código, no para hacer microservicios.

```
Dinder.slnx
├── Dinder.Domain          "El diccionario": entidades, sin dependencias
├── Dinder.Application     "El cerebro": lógica de negocio, solo conoce Domain
├── Dinder.Infrastructure  "La plomería": DB, servicios externos
├── Dinder.Api             "La puerta": HTTP, solo expone endpoints
├── Dinder.Contracts       "El catálogo": tipos compartidos
├── Dinder.UnitTests       Tests rápidos con mocks
└── Dinder.IntegrationTests Tests lentos con DB real
```

### Regla de dependencias (la flecha siempre va hacia abajo)

```
Api → Application → Domain
Api → Infrastructure → Domain
Application NUNCA depende de Infrastructure
Domain NO depende de nadie
```

### D de SOLID — Dependency Inversion (Inversión de Dependencias)

**Problema**: Si Application usa `DinderDbContext` directo, Application depende de Infrastructure.
Eso rompe la regla de arriba.

**Solución**: Application define una **interfaz** (`IApplicationDbContext`). Infrastructure la **implementa**.
Application solo conoce la interfaz, nunca la implementación concreta.

```csharp
// Application define QUÉ necesita (la interfaz):
public interface IApplicationDbContext { ... }

// Infrastructure dice CÓMO lo hace (la implementación):
public class DinderDbContext : DbContext, IApplicationDbContext { ... }
```

**Analogía**: Tu celular necesita electricidad (interfaz = enchufe). No le importa si viene
de una represa, paneles solares o nuclear. Solo necesita el enchufe.

### Dependency Injection (DI — Inyección de Dependencias)

.NET tiene un "almacén" de servicios. Vos registrás "cuando pidan X, dales Y".
Después, cualquier clase puede pedir X en su constructor y .NET le da Y automáticamente.

```csharp
// Program.cs — REGISTRAR:
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<DinderDbContext>());
//                         ↑ interfaz                          ↑ implementación concreta

// En cualquier handler — USAR:
public RegisterCommandHandler(IApplicationDbContext dbContext)
//                             ↑ .NET inyecta DinderDbContext automáticamente
```

---

## Módulo 2 — CQRS + MediatR

### MediatR — El "correo interno"

MediatR es una librería que hace de intermediario: vos mandás un "mensaje" (Command o Query)
y MediatR busca automáticamente quién sabe manejarlo.

```csharp
// En Program.cs — Activar MediatR:
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>());

// En un endpoint — Mandar un mensaje:
app.MapGet("/health", async (IMediator mediator) => {
    var result = await mediator.Send(new HealthCheckQuery()); // MediatR busca el handler solo
    return Results.Ok(result);
});
```

### CQRS — Command Query Responsibility Segregation

Separar operaciones que **CAMBIAN** datos de las que solo **LEEN**:

| Tipo | Propósito | ¿Modifica datos? | Ejemplo |
|------|-----------|-------------------|---------|
| **Command** | Hacer algo (escribir) | SÍ | `RegisterCommand` |
| **Query** | Pedir algo (leer) | NO | `MeQuery` |

```csharp
// Command (escribe):
public record RegisterCommand(...) : IRequest<AuthResponse>;

// Query (lee):
public record MeQuery : IRequest<MeResponse>;
```

### El patrón: Command/Query → Validator → Handler

Cada operación compleja se divide en 3 piezas:

1. **Command/Query**: el "mensaje" con los datos necesarios (inmutable, es un `record`)
2. **Validator**: revisa que los datos sean válidos ANTES de procesar (FluentValidation)
3. **Handler**: contiene la lógica real (qué hacer con esos datos)

```
HTTP Request
  │
  ▼
mediator.Send(RegisterCommand {email, password})
  │
  ├── 1. Validator: ¿email válido? ¿password >= 8 chars?
  │      └── Si NO → 400 Bad Request automático
  │
  └── 2. Handler: buscar duplicado, hashear, guardar, devolver tokens
```

### ¿Cómo se activa el Validator automáticamente?

Hay **3 piezas** que tienen que estar en `Program.cs` para que funcione:

```csharp
// 1. MediatR + ValidationBehavior — intercepta CADA request
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<HealthCheckQuery>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));  // ← EL PEGAMENTO
});

// 2. FluentValidation — escanea y registra TODOS los validators
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
```

**Sin `ValidationBehavior`**, el validator EXISTE pero NADIE lo ejecuta. Es como tener un extintor en la pared sin que nadie lo use cuando hay fuego.

**Regla de 3 archivos por Command:**

```
RegisterCommand.cs          ← Datos (record, 1 línea)
RegisterCommandValidator.cs ← Validación (reglas FluentValidation)
RegisterCommandHandler.cs   ← Lógica de negocio
```

**Nunca más tocás Program.cs** — las 3 líneas de arriba cubren cualquier Command/Validator que crees en el futuro.

### Organización de carpetas: por FEATURE, no por tipo

```
Auth/
├── Register/
│   ├── RegisterCommand.cs
│   ├── RegisterCommandValidator.cs
│   └── RegisterCommandHandler.cs
├── Login/
│   ├── LoginCommand.cs
│   ├── LoginCommandValidator.cs
│   └── LoginCommandHandler.cs
└── Refresh/
    ├── RefreshCommand.cs
    └── RefreshCommandHandler.cs

Queries/
├── HealthCheck/
│   ├── HealthCheckQuery.cs
│   └── HealthCheckQueryHandler.cs
└── Me/
    ├── MeQuery.cs
    └── MeQueryHandler.cs
```

Todo lo de "Register" vive junto. Si mañana borrás Register, borrás UNA carpeta.

---

## Módulo 3 — EF Core + PostgreSQL

### Entity (Entidad)

Una clase que representa una fila de la base de datos:

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }   // NUNCA guardamos la password en texto plano
    public DateTime CreatedAt { get; set; }
    public string? RefreshToken { get; set; }  // string? = puede ser null
    public DateTime? RefreshTokenExpiry { get; set; }
    public string Role { get; set; } = "user"; // valor por defecto
}
```

### DbContext — La conexión con la base de datos

```csharp
public class DinderDbContext : DbContext, IApplicationDbContext
{
    public DbSet<User> Users { get; set; } // mapea a tabla "Users"
    // EF Core convierte automáticamente DbSet<User> en SQL
}
```

### Migraciones

Cada vez que cambiás una entidad (ej: agregar `RefreshToken` a User), creás una migración.
La migración es un archivo que describe cómo modificar la DB:

```bash
dotnet ef migrations add AddRefreshTokenAndRole
```

---

## Módulo 4 — Testing

### Unit Tests vs Integration Tests

| | Unit Test | Integration Test |
|---|---|---|
| ¿Usa DB real? | ❌ InMemory o Mock | ✅ PostgreSQL (TestContainer) |
| ¿Usa HTTP real? | ❌ | ✅ WebApplicationFactory |
| Velocidad | ~100ms | ~3s |
| ¿Prueba qué? | Lógica de UNA clase | El sistema ENTERO funcionando |

### xUnit — El framework de testing

```csharp
public class MiTest
{
    [Fact] // Un test individual
    public async Task NombreDescriptivo_DelTest()
    {
        // Arrange: preparar
        // Act: ejecutar
        // Assert: verificar
        Assert.Equal(valorEsperado, valorObtenido);
    }
}
```

### Moq — Crear objetos falsos para tests

```csharp
var mock = new Mock<ITokenService>();
mock.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("fake-token");
// Ahora cada vez que alguien llame a GenerateToken, devuelve "fake-token"
```

### InMemory Database — DB falsa para unit tests

```csharp
var options = new DbContextOptionsBuilder<DinderDbContext>()
    .UseInMemoryDatabase("nombre-unico")
    .Options;
var db = new DinderDbContext(options);
// Funciona como una DB real, pero en memoria. Se destruye al terminar el test.
```

### TestContainers + WebApplicationFactory — Integration tests

```csharp
// 1. TestContainer levanta un PostgreSQL REAL en Docker
var container = new PostgreSqlBuilder().Build();
await container.StartAsync();

// 2. WebApplicationFactory levanta tu API EN MEMORIA
var factory = new CustomWebApplicationFactory(container.GetConnectionString());
var client = factory.CreateClient();

// 3. Hacés requests HTTP REALES contra tu API
var response = await client.PostAsJsonAsync("/auth/register", payload);
```

---

## Módulo 5 — JWT Identity

### JWT (JSON Web Token)

Un token que contiene información del usuario firmada digitalmente. El servidor puede
verificar que es auténtico sin consultar la base de datos.

Estructura: `header.payload.signature`

### Flujo de autenticación

```
1. Register → guarda usuario con password hasheada → devuelve JWT + refresh token
2. Login → verifica password → devuelve JWT + refresh token
3. Requests autenticados → cliente manda JWT en header Authorization
4. /me → extrae email del JWT → busca en DB → devuelve datos
```

### BCrypt — Hashear passwords

**NUNCA guardes passwords en texto plano.** BCrypt las transforma en un hash irreversible:

```csharp
string hash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!");
// hash = "$2a$12$K8xY..." (esto es lo que se guarda en la DB)

bool esCorrecta = BCrypt.Net.BCrypt.Verify("MiPassword123!", hash);
// esCorrecta = true
```

### [Authorize] — Proteger endpoints

```csharp
// Público (cualquiera puede llamarlo):
app.MapPost("/auth/login", ...);

// Protegido (solo con JWT válido):
app.MapGet("/me", [Authorize] async (IMediator mediator) => { ... });
```

### Middleware — El orden importa

```csharp
app.UseAuthentication();  // 1°: ¿quién sos? (valida el JWT)
app.UseAuthorization();   // 2°: ¿tenés permiso? (respeta [Authorize])
```

### Refresh Token

El JWT expira (por seguridad). En vez de pedir la password de nuevo, el cliente
manda el refresh token para obtener un JWT nuevo sin molestar al usuario:

```
POST /auth/refresh {refreshToken: "abc"}
  → Busca usuario con ese refresh token (no expirado)
  → Genera NUEVO JWT + NUEVO refresh token (rotación)
  → Devuelve AuthResponse
```

---

## Conceptos de C#

### Record — Objeto inmutable

```csharp
public record AuthResponse(string Token, string RefreshToken);

var r = new AuthResponse("abc", "xyz");
// r.Token = "otro"; ← ❌ NO COMPILA. Los records son inmutables.
```

### Interface — Contrato

```csharp
public interface ITokenService
{
    string GenerateToken(User user);      // QUÉ hace (no CÓMO)
    string GenerateRefreshToken();
}
```

### `string?` — Nullable reference type

```csharp
public string? RefreshToken { get; set; } // Puede ser null
public string Email { get; set; }          // NO puede ser null (el compilador avisa)
```

---

## Glosario rápido

| Término | Qué significa |
|---------|---------------|
| **Command** | Mensaje que representa una operación de ESCRITURA |
| **Query** | Mensaje que representa una operación de LECTURA |
| **Handler** | La clase que contiene la lógica real de un Command o Query |
| **Validator** | La clase que revisa que los datos sean válidos antes de procesar |
| **DTO** | Data Transfer Object — objeto que solo transporta datos (ej: `AuthResponse`) |
| **DI** | Dependency Injection — .NET te da las dependencias automáticamente |
| **Middleware** | Código que se ejecuta en CADA request (ej: autenticación) |
| **Migration** | Archivo que describe cómo modificar la estructura de la DB |
| **Scaffold** | Crear la estructura inicial (lo opuesto a construir feature por feature) |

---

## Módulo 6 — API Gateway (nginx)

### Reverse Proxy

Un reverse proxy se pone **delante** de tus servicios. El cliente habla con el proxy, y el proxy redirige al servicio correcto:

```
Cliente → nginx (:80) → /api/*  → Dinder API (:5022)
                       → /health → Health Service (:5001)
```

**¿Para qué sirve?**
- Un solo punto de entrada (los clientes no saben cuántos servicios hay)
- Load balancing (repartir tráfico entre varias instancias)
- SSL termination (HTTPS en el proxy, HTTP interno)
- Rate limiting, caching, security headers

```nginx
# nginx.conf
server {
    listen 80;
    location /api/ {
        proxy_pass http://api:5022;
    }
    location /health {
        proxy_pass http://health:5001;
    }
}
```

### Docker Networking

Los containers en la misma red Docker se **ven por nombre de servicio**, no por IP:

```
docker-compose.yml:
  networks:
    - dinder-net     ← Todos los servicios comparten esta red

nginx → http://api:5022     ← "api" = nombre del servicio en docker-compose
nginx → http://health:5001  ← Docker resuelve el nombre a la IP interna
```

---

## Módulo 7 — Deploy (CI/CD + Oracle ARM)

### CI/CD Pipeline

Automatización que ejecuta tareas cuando pusheás código. Dos pipelines típicos:

| Pipeline | Cuándo | Qué hace |
|----------|--------|----------|
| **PR Check** | Creás un Pull Request | Corre tests → si fallan, no se puede mergear |
| **Deploy** | Merge/push a main | Corre tests → si pasan, deploya a producción |

### GitHub Actions

Archivos YAML en `.github/workflows/` que definen qué pasa en cada evento:

```yaml
name: Deploy
on:
  push:
    branches: [main]        # "Cuando alguien pushee a main..."

jobs:
  deploy:
    runs-on: ubuntu-latest  # "En una VM de GitHub..."
    steps:
      - name: Run tests
        run: dotnet test    # "Corré los tests..."
      - name: SSH deploy
        uses: appleboy/ssh-action@v1  # "Conectate al server..."
```

### Secrets vs Variables de Entorno

- **GitHub Secrets**: contraseñas cifradas que solo GitHub Actions puede leer. **Nunca** en el código.
- **.env**: archivo local (gitignored) con variables de entorno para desarrollo.
- **Flujo**: GitHub Secrets → workflow → `.env` en el server → `docker compose` las lee.

```bash
# El workflow crea .env en el server desde Secrets:
printf 'POSTGRES_PASSWORD=%s\n' "$POSTGRES_PASSWORD" > .env
```

### Build en Server vs Registry

| | Build en server | Container Registry |
|---|:--:|:--:|
| Cómo | SSH → git pull → docker build | CI build → push imagen → server pull |
| Velocidad ARM | ✅ Rápido (nativo) | ❌ Lento (QEMU emula ARM) |
| Rollback | git checkout commit anterior | docker pull imagen vieja |
| Cuándo usar | 1 server, proyecto chico | Múltiples servers, Kubernetes |

### SSH Key para CI/CD

Una llave SSH permite que GitHub Actions se conecte al server sin password:

```
Tu PC: ssh-keygen → genera llave privada + pública
Server: llave pública en ~/.ssh/authorized_keys
GitHub: llave privada en Settings → Secrets → SSH_KEY
```

---

*Última actualización: Módulo 7 completo*
