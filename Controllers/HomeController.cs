﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebServiceCosmetics.Models;

namespace WebServiceCosmetics.Controllers
{
    public class HomeController : Controller
    {


        public IActionResult Index()
        {

            return View();
        }
    }
}