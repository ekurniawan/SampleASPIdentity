using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleASPIdentity.Models;

namespace SampleASPIdentity.Controllers
{
    [Authorize(Roles ="Administrator")]
    [Authorize(Roles ="Member")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ILogger<AdminController> logger,
        UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if(ModelState.IsValid)
            {
                IdentityRole identityRole = new IdentityRole{
                    Name = model.RoleName
                };

                IdentityResult result = await _roleManager.CreateAsync(identityRole);
                if(result.Succeeded)
                {
                    ViewData["pesan"] = $"<span class='alert alert-success'>Berhasil menambahkan role {model.RoleName}</span>";
                    return View();
                }

                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("",error.Description);
                }
            }
            return View();
        }

        public IActionResult ListRoles()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }

        public async Task<IActionResult> EditRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if(role==null)
            {
                ViewData["PesanError"] = $"<span class='alert alert-danger'>Role dengan id {id} tidak ditemukan.</span>";
                return View("NotFound");
            }

            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name
            };

            foreach(var user in _userManager.Users)
            {
                if(await _userManager.IsInRoleAsync(user,role.Name))
                {
                    model.Users.Add(user.UserName);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.Id);
            if(role==null)
            {
                ViewData["PesanError"] = $"<span class='alert alert-danger'>Role dengan id {model.Id} tidak ditemukan.</span>";
                return View("NotFound");
            }
            else
            {
                role.Name = model.RoleName;
                var result = await _roleManager.UpdateAsync(role);
                if(result.Succeeded)
                {
                    return RedirectToAction("ListRoles");
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("",error.Description);
                }
                return View(model);
            }

        }

        public async Task<IActionResult> EditUsersInRole(string roleId)
        {
            ViewData["roleId"] = roleId;
            var role = await _roleManager.FindByIdAsync(roleId);
            if(role==null)
            {
                ViewData["PesanError"] = $"<span class='alert alert-danger'>Role dengan Id {roleId} tidak ditemukan</span>";
                return View("NotFound");
            }

            var model = new List<UserRoleViewModel>();
            foreach(var user in _userManager.Users)
            {
                var userRoleViewModel = new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName
                };
                if(await _userManager.IsInRoleAsync(user,role.Name))
                {
                    userRoleViewModel.IsSelected = true;
                }
                else
                {
                    userRoleViewModel.IsSelected = false;
                }
                model.Add(userRoleViewModel);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUsersInRole(List<UserRoleViewModel> model, string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if(role==null)
            {
                ViewData["PesanError"] = $"<span class='alert alert-danger'>Role dengan id {roleId} tidak ditemukan.</span>";
                return View("NotFound");
            }
            for(int i=0;i<model.Count;i++)
            {
                var user = await _userManager.FindByIdAsync(model[i].UserId);
                IdentityResult result = null;
                if(model[i].IsSelected && !(await _userManager.IsInRoleAsync(user,role.Name)))
                {
                    result = await _userManager.AddToRoleAsync(user,role.Name);
                }
                else if(!model[i].IsSelected && await _userManager.IsInRoleAsync(user,role.Name))
                {
                    result = await _userManager.RemoveFromRoleAsync(user,role.Name);
                }
                else
                {
                    continue;
                }

                if(result.Succeeded)
                {
                    if(i<(model.Count-1))
                        continue;
                    else
                        return RedirectToAction("EditRole",new {Id=roleId});
                }
            }
            return RedirectToAction("EditRole",new {Id=roleId});
        }

       
        public IActionResult Index()
        {
            var username = _userManager.GetUserName(User);
            ViewBag.username = username;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}