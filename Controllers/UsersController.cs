//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Identity;
//using BackEnd_WebSocket.Models;

//namespace BackEnd_WebSocket.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class UsersController : ControllerBase
//    {
//        private readonly UserManager<ApplicationUser> _userManager;

//        public UsersController(UserManager<ApplicationUser> userManager)
//        {
//            _userManager = userManager;
//        }

//        [HttpGet]
//        public IActionResult GetAllUsers()
//        {
//            var users = _userManager.Users.Select(u => new {
//                u.Id,
//                u.UserName,
//                u.Email
//            });

//            return Ok(users);
//        }

//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetUserById(string id)
//        {
//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null) return NotFound();

//            return Ok(new
//            {
//                user.Id,
//                user.UserName,
//                user.Email
//            });
//        }

//        [HttpPost]
//        public async Task<IActionResult> CreateUser([FromBody] RegisterDto dto)
//        {
//            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
//            var result = await _userManager.CreateAsync(user, dto.Password);

//            if (!result.Succeeded)
//                return BadRequest(result.Errors);

//            return Ok(new { message = "Usuario creado", user.Id });
//        }

//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateUser(string id, [FromBody] RegisterDto dto)
//        {
//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null) return NotFound();

//            user.Email = dto.Email;
//            user.UserName = dto.Email;
//            var result = await _userManager.UpdateAsync(user);

//            if (!result.Succeeded)
//                return BadRequest(result.Errors);

//            return Ok(new { message = "Usuario actualizado" });
//        }

//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteUser(string id)
//        {
//            var user = await _userManager.FindByIdAsync(id);
//            if (user == null) return NotFound();

//            var result = await _userManager.DeleteAsync(user);
//            if (!result.Succeeded)
//                return BadRequest(result.Errors);

//            return Ok(new { message = "Usuario eliminado" });
//        }
//    }
//}

// UsersController.cs documentado con Swagger
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BackEnd_WebSocket.Models;

namespace BackEnd_WebSocket.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Obtiene todos los usuarios registrados.
        /// </summary>
        /// <returns>Lista de usuarios</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.Select(u => new {
                u.Id,
                u.UserName,
                u.Email
            });

            return Ok(users);
        }

        /// <summary>
        /// Obtiene un usuario por ID.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>Usuario encontrado</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email
            });
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        /// <param name="dto">Datos de registro</param>
        /// <returns>Usuario creado</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { message = "Usuario creado", user.Id });
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <param name="dto">Datos actualizados</param>
        /// <returns>Resultado de la actualización</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] RegisterDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Email = dto.Email;
            user.UserName = dto.Email;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario actualizado" });
        }

        /// <summary>
        /// Elimina un usuario por ID.
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario eliminado" });
        }
    }
}
