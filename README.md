# Challenge .NET Developer

## Descripción

Aplicación Web en **Angular 17** + **Backend en .NET 8** que permite sincronizar la posición, tamaño y estado de ventanas (Notepad.exe) del sistema operativo Windows mediante WebSocket. Incluye:

- Autenticación JWT (Json Web Token)
- ABM de usuarios usando **Identity + Entity Framework Core**
- Comunicación bidireccional en tiempo real por WebSocket
- Manipulación de ventanas con Win32 API (mover, redimensionar, cerrar)
- Persistencia en SQL Server de estado de cada ventana

---

## Tecnologías utilizadas

### Backend (.NET 8)
- ASP.NET Core
- Entity Framework Core
- Identity
- JWT Authentication
- WebSocket nativo
- Win32 API (P/Invoke)
- SQL Server Express
- Swagger para documentación

### Frontend (Angular 17 standalone)
- WebSocket nativo (desde Angular)
- Escalado visual en función de la resolución
- Drag & Drop + Resize personalizado
- Prevención de colisiones entre ventanas

---

## Requisitos del sistema

- Windows (requerido por uso de Win32 API y Notepad.exe)
- .NET 8 SDK
- Node.js + Angular CLI
- SQL Server Express (instancia local `localhost\SQLEXPRESS`)

---

## Ejecución

### Paso 1: Backend

1. Clonar el repositorio
2. Abrir el proyecto en Visual Studio o VS Code
3. Ejecutar la migración inicial para crear la base de datos:

   ```bash
   dotnet ef database update
   ```

4. Levantar el servidor:

   ```bash
   dotnet run
   ```

5. Acceder a Swagger:

   ```
   https://localhost:5197/swagger
   ```

### Paso 2: Frontend

1. Navegar a la carpeta `FrontEnd`
2. Ejecutar el servidor Angular:

   ```bash
   ng serve
   ```

3. Acceder en el navegador a:

   ```
   http://localhost:4200
   ```

4. Crear un usuario desde Swagger o usar el registro en Angular
5. Iniciar sesión, lo que guardará el JWT en localStorage
6. Al autenticarse se establece la conexión WebSocket

---

## Características implementadas

- Login y registro de usuarios con Identity y JWT
- WebSocket seguro con autenticación
- Representación visual de ventanas del sistema
- Drag & Drop y Resize reflejado en el sistema operativo
- Prevención de superposiciones entre ventanas
- Guardado de posición y tamaño en base de datos
- Recuperación del estado guardado al iniciar

---

## Estructura del proyecto

```
BackEnd_WebSocket
├── Controllers
│   ├── AuthController.cs
│   └── UsersController.cs
├── Data
│   └── AppDbContext.cs
├── Models
│   ├── ApplicationUser.cs
│   └── VentanaDb.cs
├── Services
│   └── WindowHelper.cs
├── Program.cs

FrontEnd
├── app
│   ├── components
│   │   ├── login
│   │   └── canvas
│   └── services
│       └── web-socket.service.ts
```

---

## Consideraciones adicionales

- Se recomienda ejecutar como **Administrador** para evitar restricciones del sistema operativo
- La arquitectura es escalable y preparada para CQRS y separación por módulos
- El token JWT se transfiere por query string al conectar el WebSocket
- Se podría extender para capturar miniaturas (bonus no implementado)

---

## Autor

**Pablo Gianfortone**  
Proyecto desarrollado como parte del challenge .NET Developer para **Sia Interactive** (Proyecto C-Control)

2025

---

