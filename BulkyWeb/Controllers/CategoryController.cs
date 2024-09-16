﻿using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Controllers;

public class CategoryController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CategoryController> _logger;
    public CategoryController(ApplicationDbContext db, ILogger<CategoryController> logger)
    {
        _db = db;
        _logger = logger;
    }
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        List<Category> objCategoryList = await _db.Categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return View(objCategoryList);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        if (category.Name == category.DisplayOrder.ToString())
        {
            ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name!");
        }
        //if (category.Name is not null && category.Name.Equals("test", StringComparison.CurrentCultureIgnoreCase))
        //{
        //    ModelState.AddModelError("", "Test is an invalid value!");
        //}
        if (ModelState.IsValid)
        {
            await _db.Categories.AddAsync(category);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category created successfully!";
            return RedirectToAction("Index");
        }

        return View();
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }
        Category? categoryFromDb = await _db.Categories.FindAsync(id);
        //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id);
        //Category? categoryFromDb2 = _db.Categories.Where(u=>u.Id==id).FirstOrDefault();

        if (categoryFromDb == null)
        {
            return NotFound();
        }
        return View(categoryFromDb);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Category category)
    {
        if (category.Name == category.DisplayOrder.ToString())
        {
            ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name!");
        }
        //if (category.Name is not null && category.Name.Equals("test", StringComparison.CurrentCultureIgnoreCase))
        //{
        //    ModelState.AddModelError("", "Test is an invalid value!");
        //}
        if (ModelState.IsValid)
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category updated successfully!";
            return RedirectToAction("Index");
        }

        return View();
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }
        Category? categoryFromDb = await _db.Categories.FindAsync(id);
        //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id);
        //Category? categoryFromDb2 = _db.Categories.Where(u=>u.Id==id).FirstOrDefault();

        if (categoryFromDb == null)
        {
            return NotFound();
        }
        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeletePOST(int? id)
    {
        Category? categoryFromDb = await _db.Categories.FindAsync(id);
        if (categoryFromDb == null)
        {
            return NotFound();
        }

        _db.Categories.Remove(categoryFromDb);
        await _db.SaveChangesAsync();
        TempData["success"] = "Category deleted successfully";
        return RedirectToAction("Index");
    }
}
