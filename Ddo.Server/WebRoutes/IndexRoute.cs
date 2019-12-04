﻿using System.Threading.Tasks;
using Ddo.Server.Web;
using Ddo.Server.Web.Route;

namespace Ddo.Server.WebRoutes
{
    public class IndexRoute : WebRoute
    {
        public override string Route => "/";

        public override async Task<WebResponse> Get(WebRequest request)
        {
            WebResponse response = new WebResponse();
            response.StatusCode = 200;
            await response.WriteAsync("Welcome - Index Page!");
            return response;
        }
    }
}
