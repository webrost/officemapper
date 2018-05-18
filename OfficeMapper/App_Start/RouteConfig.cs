using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OfficeMapper
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Users",
                url: "users/{guid}",
                defaults: new { controller = "Apps", action = "Users", guid = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "ImportAD",
                url: "importad",
                defaults: new { controller = "Apps", action = "ImportAD", guid = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Online",
                url: "online/{sortBy}",
                defaults: new { controller = "Apps", action = "OnlineUsers", sortBy="", guid = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Invent",
                url: "invent/{sortBy}",
                defaults: new { controller = "Apps", action = "Invent", sortBy = "", guid = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Apps",
                url: "Apps/{id}",
                defaults: new { controller = "Apps", action = "Index", guid = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );


        }
    }
}
