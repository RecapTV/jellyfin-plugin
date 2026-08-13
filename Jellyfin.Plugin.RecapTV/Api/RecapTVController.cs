using System;
using System.Reflection;
using Jellyfin.Plugin.RecapTV.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.RecapTV.Api
{
    [ApiController]
    [Authorize]
    [Route("RecapTV")]
    public class RecapTVController : ControllerBase
    {
        private readonly IUserManager _userManager;
        private readonly TokenStore _tokenStore;

        public RecapTVController(IUserManager userManager, TokenStore tokenStore)
        {
            _userManager = userManager;
            _tokenStore = tokenStore;
        }

        [HttpGet("Status")]
        public ActionResult<StatusResponse> GetStatus()
        {
            var user = ResolveUser();
            if (user is null)
            {
                return Unauthorized();
            }

            var record = _tokenStore.Get(user.Id);
            return Ok(new StatusResponse(record is not null, record?.LastError));
        }

        [HttpPost("Token")]
        public ActionResult<StatusResponse> SaveToken([FromBody] SaveTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest();
            }

            var user = ResolveUser();
            if (user is null)
            {
                return Unauthorized();
            }

            _tokenStore.Save(user.Id, request.Token.Trim());
            return Ok(new StatusResponse(true, null));
        }

        [HttpDelete("Token")]
        public ActionResult<StatusResponse> DeleteToken()
        {
            var user = ResolveUser();
            if (user is null)
            {
                return Unauthorized();
            }

            _tokenStore.Remove(user.Id);
            return Ok(new StatusResponse(false, null));
        }

        [AllowAnonymous]
        [HttpGet("ClientScript.js")]
        public ActionResult GetClientScript()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jellyfin.Plugin.RecapTV.Web.RecapTV-client.js");
            if (stream is null)
            {
                return NotFound();
            }

            return File(stream, "application/javascript");
        }

        private Jellyfin.Database.Implementations.Entities.User? ResolveUser()
        {
            var name = User.Identity?.Name;
            return string.IsNullOrEmpty(name) ? null : _userManager.GetUserByName(name);
        }
    }

    public record SaveTokenRequest(string Token);

    public record StatusResponse(bool Connected, string? LastError);
}
