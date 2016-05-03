using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    public class HomeController : ControllerBase
    {
        private static readonly byte[] _bytes = Encoding.UTF8.GetBytes("Hello, World!");

        public Task Index()
        {
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/plain";
            Response.ContentLength = _bytes.Length;
            return Response.Body.WriteAsync(_bytes, 0, _bytes.Length);
        }
    }
}
