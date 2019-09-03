using System;
using Microsoft.AspNetCore.Mvc;
using Bit.Core.Models.Api;
using Bit.Core.Utilities;
using Bit.Core;

namespace Bit.Api.Controllers
{
    public class MiscController : Controller
    {
        private readonly GlobalSettings _globalSettings;

        public MiscController(
            GlobalSettings globalSettings)
        {
            _globalSettings = globalSettings;
        }

        [HttpGet("~/alive")]
        [HttpGet("~/now")]
        public DateTime Get()
        {
            return DateTime.UtcNow;
        }

        [HttpGet("~/version")]
        public VersionResponseModel Version()
        {
            return new VersionResponseModel();
        }

        [HttpGet("~/ip")]
        public JsonResult Ip()
        {
            return new JsonResult(new
            {
                Ip = HttpContext.Connection?.RemoteIpAddress?.ToString(),
                Headers = HttpContext.Request?.Headers,
            });
        }
    }
}
