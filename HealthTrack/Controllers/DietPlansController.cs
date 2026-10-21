// =====================================================================
// FILE:    Controllers/DietPlansController.cs
// PURPOSE: Trainers create and manage diet plans for their clients (Task 5):
//          list, create, view details, edit and delete a plan, and add / edit /
//          remove the planned meals inside it.
// SECURITY: [Authorize(Roles = Trainer)]; every action first checks the trainer is
//           active (ActiveTrainerIdAsync), then that the plan belongs to them
//           (plan.TrainerId == trainerId, otherwise 404). The chosen client must
//           be actively assigned to the trainer (IsAssignedClientAsync). The trainer
//           id is always taken from the signed-in user, never from the form.
// PATTERN: GET shows a form; POST validates, saves inside try/catch
//          (DbUpdateException) and then redirects (Post-Redirect-Get) with a
//          TempData success message.
// VIEWS:   Views/DietPlans/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // DietPlan, DietPlanItem, TrainerAssignment, AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // DietPlanCreateViewModel, DietPlanItemCreateViewModel, ...
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpGet], [HttpPost]
using Microsoft.AspNetCore.Mvc.Rendering;  // SelectListItem (client drop-down)
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]                              // every action: trainers only
    public class DietPlansController : Controller
    {
        private readonly IRepository<DietPlan> _plans;                 // diet plans table
        private readonly IRepository<DietPlanItem> _items;             // planned meals table
        private readonly IRepository<TrainerAssignment> _assignments;  // trainer <-> client links
        private readonly UserManager<ApplicationUser> _users;          // users and roles
        private readonly ILogger<DietPlansController> _logger;         // writes errors to the log

        // Constructor: ASP.NET Core passes in every dependency
        public DietPlansController(IRepository<DietPlan> plans,
            IRepository<DietPlanItem> items,
            IRepository<TrainerAssignment> assignments,
            UserManager<ApplicationUser> users,
            ILogger<DietPlansController> logger)
        {
            _plans = plans;              // keep each dependency in a field
            _items = items;
            _assignments = assignments;
            _users = users;
            _logger = logger;
        }

        // GET: DietPlans  -  the trainer's own diet plans
        public async Task<IActionResult> Index()
        {
            var trainerId = await ActiveTrainerIdAsync();                         // null if not an active trainer
            if (trainerId is null) return Forbid();                               // refuse
            var plans = await _plans.FindAsync(p => p.TrainerId == trainerId);    // only this trainer's plans
            return View(plans.OrderByDescending(p => p.CreatedAt).ToList());      // newest first
        }

        // GET: DietPlans/Create  -  empty form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            await LoadClientsAsync(trainerId);                                    // fill the client drop-down
            return View(new DietPlanCreateViewModel());                           // empty form with defaults
        }

        // POST: DietPlans/Create  -  save a new plan
        [HttpPost, ValidateAntiForgeryToken]                                      // POST only, with a valid anti-forgery token
        public async Task<IActionResult> Create(DietPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            if (ModelState.IsValid && !await IsAssignedClientAsync(trainerId, input.ClientId))   // the client must be theirs
                ModelState.AddModelError(nameof(input.ClientId), "Select an active client assigned to you.");

            if (!ModelState.IsValid)                                              // any validation error?
            {
                await LoadClientsAsync(trainerId);                                // refill the drop-down
                return View(input);                                               // show the form again with errors
            }

            // Copy the validated form values into a new entity
            var plan = new DietPlan
            {
                Title = input.Title.Trim(),                                       // remove extra spaces
                Description = input.Description?.Trim(),                          // optional
                DailyCalorieTarget = input.DailyCalorieTarget,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                TrainerId = trainerId, // Never trust a trainer ID from a form.
                ClientId = input.ClientId                                         // already checked above
            };
            await _plans.AddAsync(plan);                                          // mark it to be inserted
            try
            {
                await _plans.SaveAsync();                                         // write it to the database
            }
            catch (DbUpdateException ex)                                          // the database save failed
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in DietPlansController.Create");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the diet plan could not be saved. Please try again.");
                await LoadClientsAsync(trainerId);
                return View(input);                                               // show the form again with the message
            }

            TempData["Success"] = "Diet plan created.";                           // green message on the next page
            return RedirectToAction(nameof(Details), new { id = plan.Id });       // go to the new plan
        }

        // GET: DietPlans/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);                             // load the plan
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // missing or another trainer's: 404

            var meals = await _items.FindAsync(x => x.DietPlanId == id);          // the plan's meals
            var client = await _users.FindByIdAsync(plan.ClientId);               // the client (for the name)
            return View(new DietPlanDetailsViewModel
            {
                Plan = plan,
                ClientName = client?.FullName ?? "Unknown client",                // fallback if the user is missing
                Meals = meals.OrderBy(m => m.MealType).ThenBy(m => m.MealTime).ToList()   // breakfast first, then by time
            });
        }

        // GET: DietPlans/Edit/5  -  form filled with the current values
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only their own plan
            await LoadClientsAsync(trainerId);                                    // fill the client drop-down
            ViewBag.PlanId = id;                                                  // used by the form's URL
            return View(new DietPlanCreateViewModel                              // copy the entity into the form model
            {
                Title = plan.Title,
                Description = plan.Description,
                DailyCalorieTarget = plan.DailyCalorieTarget,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                ClientId = plan.ClientId
            });
        }

        // POST: DietPlans/Edit/5  -  save the changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DietPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // check ownership again on POST

            if (ModelState.IsValid && !await IsAssignedClientAsync(trainerId, input.ClientId))   // the client must be theirs
                ModelState.AddModelError(nameof(input.ClientId), "Select an active client assigned to you.");
            if (!ModelState.IsValid)
            {
                await LoadClientsAsync(trainerId);
                ViewBag.PlanId = id;
                return View(input);                                               // show the form again with errors
            }

            // Copy the new values onto the existing entity
            plan.Title = input.Title.Trim();
            plan.Description = input.Description?.Trim();
            plan.DailyCalorieTarget = input.DailyCalorieTarget;
            plan.StartDate = input.StartDate;
            plan.EndDate = input.EndDate;
            plan.ClientId = input.ClientId;
            _plans.Update(plan);                                                  // mark it as changed
            try
            {
                await _plans.SaveAsync();                                         // write the changes
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
            return RedirectToAction(nameof(Details), new { id });                 // back to the plan
        }

        // GET: DietPlans/AddMeal/5  (5 = the plan id)
        [HttpGet]
        public async Task<IActionResult> AddMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only their own plan
            ViewBag.PlanId = id;                                                  // used by the form's URL
            ViewBag.PlanTitle = plan.Title;                                       // shown on the page
            return View(new DietPlanItemCreateViewModel());                       // empty meal form
        }

        // POST: DietPlans/AddMeal/5  -  save a new planned meal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(int id, DietPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // check ownership again on POST
            if (!ModelState.IsValid)
            {
                ViewBag.PlanId = id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);                                               // show the form again with errors
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
                await _items.SaveAsync();                                         // write it to the database
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
            return RedirectToAction(nameof(Details), new { id });                 // back to the plan
        }

        // GET: DietPlans/EditMeal/7  (7 = the meal id)
        [HttpGet]
        public async Task<IActionResult> EditMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);                             // load the meal
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);                // and its plan
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // the plan must be theirs
            ViewBag.MealId = id;                                                  // used by the form's URL
            ViewBag.PlanId = plan.Id;                                             // used by the Cancel link
            ViewBag.PlanTitle = plan.Title;                                       // shown on the page
            return View(new DietPlanItemCreateViewModel                           // copy the meal into the form model
            {
                MealType = meal.MealType,
                FoodItems = meal.FoodItems,
                Calories = meal.Calories,
                MealTime = meal.MealTime
            });
        }

        // POST: DietPlans/EditMeal/7  -  save the meal changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeal(int id, DietPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // check ownership again on POST
            if (!ModelState.IsValid)
            {
                ViewBag.MealId = id;
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);                                               // show the form again with errors
            }

            // Copy the new values onto the existing meal
            meal.MealType = input.MealType;
            meal.FoodItems = input.FoodItems.Trim();
            meal.Calories = input.Calories;
            meal.MealTime = input.MealTime;
            _items.Update(meal);                                                  // mark it as changed
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
            return RedirectToAction(nameof(Details), new { id = plan.Id });       // back to the plan
        }

        // POST: DietPlans/DeleteMeal/7  -  remove one planned meal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var meal = await _items.GetByIdAsync(id);
            if (meal is null) return NotFound();
            var plan = await _plans.GetByIdAsync(meal.DietPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only from their own plan
            _items.Delete(meal);                                                  // mark it for deletion
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
            return RedirectToAction(nameof(Details), new { id = plan.Id });       // back to the plan
        }

        // GET: DietPlans/Delete/5  -  "are you sure?" page
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only their own plan
            return View(plan);                                                    // show the confirmation page
        }

        // POST: DietPlans/Delete/5  -  really delete the plan
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]                // same URL as the GET above
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _plans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // check ownership again on POST
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
            return RedirectToAction(nameof(Index));                               // back to the list
        }

        // Returns the signed-in user's id if they are an ACTIVE trainer, otherwise null
        private async Task<string?> ActiveTrainerIdAsync()
        {
            var id = _users.GetUserId(User);                                      // the signed-in user's id
            if (id is null) return null;                                          // not signed in
            var trainer = await _users.FindByIdAsync(id);                         // load the account
            return trainer is { IsActive: true } ? id : null;                     // deactivated = null
        }

        // True only if the client is active, has the Client role and is actively assigned to this trainer
        private async Task<bool> IsAssignedClientAsync(string trainerId, string clientId)
        {
            var client = await _users.FindByIdAsync(clientId);                    // load the client
            if (client is null || !client.IsActive ||                             // missing or deactivated...
                !await _users.IsInRoleAsync(client, AppRoles.Client)) return false;   // ...or not a client
            var assigned = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.ClientId == clientId && a.IsActive);   // an active link?
            return assigned.Any();
        }

        // Fills ViewBag.Clients with the trainer's active clients for the drop-down
        private async Task LoadClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(a =>
                a.TrainerId == trainerId && a.IsActive);                          // this trainer's active links
            var clients = new List<SelectListItem>();                             // the drop-down options
            foreach (var id in assignments.Select(a => a.ClientId).Distinct())    // each client once
            {
                var user = await _users.FindByIdAsync(id);                        // load the user
                if (user is { IsActive: true } &&                                 // active...
                    await _users.IsInRoleAsync(user, AppRoles.Client))            // ...client
                    clients.Add(new SelectListItem(user.FullName, user.Id));      // text = name, value = id
            }
            ViewBag.Clients = clients.OrderBy(c => c.Text).ToList();              // alphabetical
        }
    }
}
