﻿using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Bulky.Utility.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(RoleEnum.Admin))]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ProductController> _logger;
    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Starting product search all including category...");

        IEnumerable<Product> objProductList = await _unitOfWork.Product.GetAll(page: page, pageSize: pageSize, includeProperties: "Category");
        
        return View(objProductList);
    }

    public async Task<IActionResult> Upsert(int? id)
    {
        _logger.LogInformation("Starting product upsert form...");

        var categoryListAsync = await _unitOfWork.Category.GetAll();

        ProductVM productVM = new()
        {
            CategoryList = categoryListAsync.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
            Product = new Product()
        };

        // UPDATE
        if (id != null && id >= 0)
        {
            Product? productFromDb = await _unitOfWork.Product.Get(u => u.Id == id);
            //Product? productFromDb1 = _db.Products.FirstOrDefault(u=>u.Id==id);
            //Product? productFromDb2 = _db.Products.Where(u=>u.Id==id).FirstOrDefault();

            if (productFromDb == null)
            {
                _logger.LogError(message: LogExceptionMessages.ProductNotFoundException);
                return NotFound();
            }

            productVM.Product = productFromDb;
        }

        // CREATE
        return View(productVM);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(ProductVM productVM, IFormFile? file)
    {
        _logger.LogInformation("Starting product upsert...");

        if (ModelState.IsValid)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string successMessage;

            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string productPath = Path.Combine(wwwRootPath, @"images\product");

                if (!string.IsNullOrWhiteSpace(productVM.Product.ImageUrl))
                {
                    // delete the old image
                    var resizeOldImageUrlPath = productVM.Product.ImageUrl.TrimStart('\\');
                    var oldImagePath = Path.Combine(wwwRootPath, resizeOldImageUrlPath);

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileStreamPath = Path.Combine(productPath, fileName);
                using (var fileStream = new FileStream(fileStreamPath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVM.Product.ImageUrl = @$"\images\product\{fileName}";
            }

            if (productVM.Product.Id == 0)
            {
                await _unitOfWork.Product.Add(productVM.Product);
                successMessage = SuccessDataMessages.ProductCreatedSuccess;
            }
            else
            {
                _unitOfWork.Product.Update(productVM.Product);
                successMessage = SuccessDataMessages.ProductUpdatedSuccess;
            }
            
            await _unitOfWork.Save();
            TempData["success"] = successMessage;
            return RedirectToAction(actionName: nameof(Index));
        }

        var categoryListAsync = await _unitOfWork.Category.GetAll();
        productVM.CategoryList = categoryListAsync.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

        return View(productVM);
    }

    #region API CALLS

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Starting product search all...");

        IEnumerable<Product> objProductList = await _unitOfWork.Product.GetAll(page: page, pageSize: pageSize, includeProperties: "Category");

        return Json(new { data = objProductList });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int? id)
    {
        _logger.LogInformation("Starting product delete...");

        var productToBeDeleted = await _unitOfWork.Product.Get(u => u.Id == id);

        if (productToBeDeleted == null)
        {
            return Json(new { success = false, message = LogExceptionMessages.ProductNotFoundException });
        }

        // remove old image
        string wwwRootPath = _webHostEnvironment.WebRootPath;
        var resizeOldImageUrlPath = productToBeDeleted.ImageUrl.TrimStart('\\');
        var oldImagePath = Path.Combine(wwwRootPath, resizeOldImageUrlPath);

        if (System.IO.File.Exists(oldImagePath))
        {
            System.IO.File.Delete(oldImagePath);
        }

        _unitOfWork.Product.Remove(productToBeDeleted);
        await _unitOfWork.Save();

        return Json(new { success = true, message = SuccessDataMessages.ProductDeletedSuccess });
    }

    #endregion
}
