using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]
    public class DietPlansController : Controller
    {
        private readonly IRepository<DietPlan> _plans;
        private readonly IRepository<DietPlanItem> _items;
        private readonly IRepository<TrainerAssignment> _assignments;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<DietPlansController> _logger;

        public DietPlansController(IRepository<DietPlan> plans,
            IRepository<DietPlanItem> items,
            IRepository<TrainerAssignment> assignments,
            UserManager<ApplicationUser> users,
            ILogger<DietPlansController> logger)
        {
            _plans = plans;
            _items = items;
            _assignments = assignments;
            _users = users;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plans = await _plans.FindAsync(p => p.TrainerId == trainerId);
            return View(plans.OrderByDescending(p => p.CreatedAt).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            await LoadClientsAsync(trainerId);
            return View(new DietPlanCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DietPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            if (ModelState.IsValid && !await IsAssignedClientAsync(trainerId, input.ClientId))
                ModelState.AddModelError(nameof(input.ClientId), "Select an active client assigned to you.");

            if (!ModelState.IsValid)
            {
                await LoadClientsAsync(trainerId);
                return View(input);
            }

            var plan = new DietPlan
            {
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                DailyCalorieTarget = input.DailyCalorieTarget,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                TrainerId = trainerId, // Never trust a trainer ID from a form.
                ClientId = input.ClientId
            };
            await _plans.AddAsync(plan);
            try
            {
                await _plans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.Create");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the diet plan could not be saved. Please try again.");
                await LoadClientsAsync(trainerId);
                return View(input);
            }

            TempData["Success"] = "Diet plan created.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();

            var meals = await _items.FindAsync(x => x.DietPlanId == id);
            var client = await _users.FindByIdAsync(plan.ClientId);
            return View(new DietPlanDetailsViewModel
            {
                Plan = plan,
                ClientName = client?.FullName ?? "Unknown client",
                Meals = meals.OrderBy(m => m.MealType).ThenBy(m => m.MealTime).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            await LoadClientsAsync(trainerId);
            ViewBag.PlanId = id;
            return View(new DietPlanCreateViewModel
            {
                Title = plan.Title,
                Description = plan.Description,
                DailyCalorieTarget = plan.DailyCalorieTarget,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                ClientId = plan.ClientId
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DietPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();

            if (ModelState.IsValid && !await IsAssignedClientAsync(trainerId, input.ClientId))
                ModelState.AddModelError(nameof(input.ClientId), "Select an active client assigned to you.");
            if (!ModelState.IsValid)
            {
                await LoadClientsAsync(trainerId);
                ViewBag.PlanId = id;
                return View(input);
            }

            plan.Title = input.Title.Trim();
            plan.Description = input.Description?.Trim();
            plan.DailyCalorieTarget = input.DailyCalorieTarget;
            plan.StartDate = input.StartDate;
            plan.EndDate = input.EndDate;
            plan.ClientId = input.ClientId;
            _plans.Update(plan);
            try
            {
                await _plans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.Edit");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the diet plan could not be saved. Please try again.");
                await LoadClientsAsync(trainerId);
                ViewBag.PlanId = id;
                return View(input);
            }

            TempData["Success"] = "Diet plan updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> AddMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            ViewBag.PlanId = id;
            ViewBag.PlanTitle = plan.Title;
            return View(new DietPlanItemCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(int id, DietPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.PlanId = id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            await _items.AddAsync(new DietPlanItem
            {
                DietPlanId = id, // The checked route supplies the parent ID.
                MealType = input.MealType,
                FoodItems = input.FoodItems.Trim(),
                Calories = input.Calories,
                MealTime = input.MealTime
            });
            try
            {
                await _items.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.AddMeal");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the meal could not be saved. Please try again.");
                ViewBag.PlanId = id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            TempData["Success"] = "Meal added to the plan.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> EditMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            ViewBag.MealId = id;
            ViewBag.PlanId = plan.Id;
            ViewBag.PlanTitle = plan.Title;
            return View(new DietPlanItemCreateViewModel
            {
                MealType = meal.MealType,
                FoodItems = meal.FoodItems,
                Calories = meal.Calories,
                MealTime = meal.MealTime
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeal(int id, DietPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.MealId = id;
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            meal.MealType = input.MealType;
            meal.FoodItems = input.FoodItems.Trim();
            meal.Calories = input.Calories;
            meal.MealTime = input.MealTime;
            _items.Update(meal);
            try
            {
                await _items.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.EditMeal");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the meal could not be saved. Please try again.");
                ViewBag.MealId = id;
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            TempData["Success"] = "Meal updated.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            _items.Delete(meal);
            try
            {
                await _items.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.DeleteMeal");
                TempData["Error"] = "Sorry, the meal could not be deleted. Please try again.";
                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }

            TempData["Success"] = "Meal removed from the plan.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            return View(plan);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            // EF cascades to the plan's meals; the client's actual meal logs remain.
            _plans.Delete(plan);
            try
            {
                await _plans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.DeleteConfirmed");
                TempData["Error"] = "Sorry, the diet plan could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Diet plan deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> ActiveTrainerIdAsync()
        {
            var id = _users.GetUserId(User);
            if (id is null) return null;
            var trainer = await _users.FindByIdAsync(id);
            return trainer is { IsActive: true } ? id : null;
        }

        private async Task<bool> IsAssignedClientAsync(string trainerId, string clientId)
        {
            var client = await _users.FindByIdAsync(clientId);
            if (client is null || !client.IsActive ||
                !await _users.IsInRoleAsync(client, AppRoles.Client)) return false;
            var assigned = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.ClientId == clientId && a.IsActive);
            return assigned.Any();
        }

        private async Task LoadClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.IsActive);
            var clients = new List<SelectListItem>();
            foreach (var id in assignments.Select(a => a.ClientId).Distinct())
            {
                var user = await _users.FindByIdAsync(id);
                if (user is { IsActive: true } &&
                    await _users.IsInRoleAsync(user, AppRoles.Client))
                    clients.Add(new SelectListItem(user.FullName, user.Id));
            }
            ViewBag.Clients = clients.OrderBy(c => c.Text).ToList();
        }
    }
}
