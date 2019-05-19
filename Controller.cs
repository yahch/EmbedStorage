using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;

namespace EmbedStorage
{
    public class Controller : WebApiController
    {
        public Controller(IHttpContext context) : base(context)
        {

        }

        public Task<bool> Error(Exception e = null, CancellationToken cancellationToken = default)
        {
            Terminal.Error(e, e.Source, e.Message);
            HttpContext.Response.StatusCode = 500;
            return Ok(e == null ? "内部服务器错误" : e.StackTrace, "text/plain", Encoding.UTF8, false, cancellationToken);
        }
    }
}
