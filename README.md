Challenge .NET Developer

Descripción

Aplicación Web en Angular + Backend en .NET que permite sincronizar la posición, tamaño y estado de ventanas (Notepad.exe) del sistema operativo Windows a través de WebSockets. El sistema cuenta con:

Autenticación por JWT (Json Web Token)

ABM de usuarios usando Identity + Entity Framework

Comunicación bidireccional por WebSocket para mover, redimensionar y cerrar ventanas

Persistencia en base de datos SQL Server de la posición y tamaño de cada ventana

Tecnologías utilizadas

Backend (.NET 8)

ASP.NET Core

Entity Framework Core

Identity (usuarios)

JWT

WebSocket nativo

Win32 API (para manipular ventanas)

SQL Server Express

Swagger para documentación

Frontend (Angular 17 standalone)

WebSocket nativo (desde Angular)

Escalado visual según resolución

Drag & Drop + Resize personalizado

Prevención de colisiones entre ventanas

Requisitos

Windows (obligatorio por uso de Notepad.exe y Win32 APIs)

.NET 8 SDK

Node.js y Angular CLI

SQL Server Express (con instancia local localhost\SQLEXPRESS)

Ejecución

Paso 1: Backend

Clonar el proyecto y abrir en Visual Studio o VS Code

Crear la base de datos (se hace automáticamente con la migración inicial):

dotnet ef database update

Ejecutar el proyecto:

dotnet run

Esto levantará el backend y exponerá Swagger en:

https://localhost:5197/swagger

Paso 2: Frontend

Desde carpeta FrontEnd ejecutar:

ng serve

Acceder a:

http://localhost:4200

Crear un usuario desde Swagger (/api/auth/register) o directamente desde Angular

Iniciar sesión en Angular. Se guardará el token en localStorage y se establecerá la conexión por WebSocket.

Características implementadas



Estructura

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

Consideraciones adicionales

El sistema está preparado para crecer (CQRS y módulos separados)

Se podría agregar captura de imagen de la ventana en miniatura como bonus

Se recomienda ejecutar como administrador si hay problemas con permisos en la API de ventanas

Autor

Pablo Gianfortone

Proyecto realizado como parte del Challenge .NET Developer para Sia Interactive, Proyecto C-Control

© 2025
