using AuthenController.Models;
using AuthenServices.Dto;
using AuthenServices.Helpers;
using AuthenServices.Services;
using CacheLite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenServices.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthenController: BaseController
    {
        private readonly IUserServices _userServices;

        private readonly IConfiguration _configuration;

        private readonly Cache _jwtCache;

        public AuthenController(IUserServices userServices, IConfiguration configuration, [FromKeyedServices(Const.JWT_CACHE)] Cache jwtCache)
        {
            _userServices = userServices;
            _configuration = configuration;
            _jwtCache = jwtCache;
        }

        [HttpGet("getAllUser")]
        public async Task<IResult> Get()
        {
            var result = await _userServices.GetAllUsersAsync();
            return Ok(result) ;
        }

        [HttpPost("Login")]
        public async Task<IResult> Login(UserLogin userLogin)
        {
            var user = await _userServices.GetUserByNameAsync(userLogin.UserName);

            bool verify = false;
            try
            {
                verify = BCrypt.Net.BCrypt.EnhancedVerify(userLogin.Pwd, user.UserName, hashType: BCrypt.Net.HashType.SHA256);
            }
            catch { }
            if (verify && user != null)
            {
                var token = Utils.GenerateAccessToken(user.Id.ToString(), _configuration["Jwt:Key"]);

                await _jwtCache.UpdateAsync(token, user.Id.ToString(), 1);

                var CookieOpt = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                };

                HttpContext.Response.Cookies.Delete(Const.ACCESS_TOKEN, CookieOpt);
                HttpContext.Response.Cookies.Append(Const.ACCESS_TOKEN, token, CookieOpt);

                return OK;
            }
            else
            {
                return ErrorUnauthorized;
            }
        }

        public async Task<IResult> addUser(User user)
        {
            var result = _userServices.AddUser(user);
            return Ok(result);
        }
    }
}