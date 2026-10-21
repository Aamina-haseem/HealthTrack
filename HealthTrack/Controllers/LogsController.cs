// =====================================================================
// FILE:    Controllers/LogsController.cs
// PURPOSE: The client's "Log Activity" area (Task 5): list, add, edit and delete
//          their own workout logs, meal logs and health measurements.
// SECURITY: [Authorize(Roles = Client)]; every action first checks the client is
//           active (ActiveClientIdAsync); every edit/delete checks the record
//           belongs to the signed-in client (otherwise 404); the client id is
//           always taken from the signed-in user, never from the form; a chosen
//           workout plan must be the client's own (CheckOwnPlanAsync).
// PATTERN: GET shows a form; POST validates, saves inside try/catch
//          (DbUpdateException) and redirects back to the list (Post-Redirect-Get)
//          with a TempData success message.
// VIEWS:   Views/Logs/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // WorkoutLog, MealLog, HealthMeasurement, WorkoutPlan, AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // the Create view models and ClientLogsViewModel
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpGet], [HttpPost]
using Microsoft.AspNetCore.Mvc.Rendering;  // SelectListItem (plan drop-down)
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Client)]                               // every action: clients only
    public class LogsController : Controller
    {
        private readonly IRepository<WorkoutLog> _workouts;            // workout logs table
        private readonly IRepository<MealLog> _meals;                  // meal logs table
        private readonly IRepository<HealthMeasurement> _measurements; // measurements table
        private readonly IRepository<WorkoutPlan> _plans;              // workout plans (for the drop-down)
        private readonly UserManager<ApplicationUser> _users;          // users
        private readonly ILogger<LogsController> _logger;              // writes errors to the log

        // Constructor: ASP.NET Core passes in every dependency
        public LogsController(IRepository<WorkoutLog> workouts,
            IRepository<MealLog> meals,
            IRepository<HealthMeasurement> measurements,
            IRepository<WorkoutPlan> plans,
            UserManager<ApplicationUser> users,
            ILogger<LogsController> logger)
        {
            _workouts = workouts;            // keep each dependency in a field
            _meals = meals;
            _measurements = measurements;
            _plans = plans;
            _users = users;
            _logger = logger;
        }

        // GET: Logs  -  the client's three lists (workouts, meals, measurements)
        public async Task<IActionResult> Index()
        {
            var clientId = await ActiveClientIdAsync();                            // null if not an active client
            if (clientId is null) return Forbid();                                 // refuse
            return View(new ClientLogsViewModel
            {
                Workouts = (await _workouts.FindAsync(x => x.ClientId == clientId))          // only this client's workouts
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList(),   // newest first
                Meals = (await _meals.FindAsync(x => x.ClientId == clientId))                // only this client's meals
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList(),
                Measurements = (await _measurements.FindAsync(x => x.ClientId == clientId))  // only this client's measurements
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList()
            });
        }

        // ===================== WORKOUTS =====================

        // GET: Logs/AddWorkout  -  empty form
        [HttpGet]
        public async Task<IActionResult> AddWorkout()
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            await LoadPlansAsync(clientId);                                        // fill the "Workout plan" drop-down
            return View(new WorkoutLogCreateViewModel());                          // empty form (date = today)
        }

        // POST: Logs/AddWorkout  -  save a new workout
        [HttpPost, ValidateAntiForgeryToken]                                       // POST only, with a valid anti-forgery token
        public async Task<IActionResult> AddWorkout(WorkoutLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            await CheckOwnPlanAsync(clientId, input.WorkoutPlanId);                // a chosen plan must be their own
            if (!ModelState.IsValid)                                               // any validation error?
            {
                await LoadPlansAsync(clientId);
                return View(input);                                                // show the form again with errors
            }
            await _workouts.AddAsync(new WorkoutLog
            {
                ClientId = clientId,                                               // from the signed-in user
                WorkoutPlanId = input.WorkoutPlanId,                               // null = no plan
                ExerciseName = input.ExerciseName.Trim(),                          // remove extra spaces
                Date = input.Date.Date,                                            // date only, no time
                DurationMinutes = input.DurationMinutes,
                CaloriesBurned = input.CaloriesBurned,
                Notes = input.Notes?.Trim()                                        // optional
            });
            try
            {
                await _workouts.SaveAsync();                                       // write it to the database
            }
            catch (DbUpdateException ex)                                           // the database save failed
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.AddWorkout");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout could not be saved. Please try again.");
                await LoadPlansAsync(clientId);
                return View(input);
            }

            TempData["Success"] = "Workout logged.";                               // green message on the next page
            return RedirectToAction(nameof(Index));                                // back to the lists
        }

        // GET: Logs/EditWorkout/5  -  form filled with the current values
        [HttpGet]
        public async Task<IActionResult> EditWorkout(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);                            // load the workout
            if (log is null || log.ClientId != clientId) return NotFound();        // missing or someone else's: 404
            await LoadPlansAsync(clientId);
            ViewBag.LogId = id;                                                    // used by the form's URL
            return View(new WorkoutLogCreateViewModel                              // copy the entity into the form model
            {
                WorkoutPlanId = log.WorkoutPlanId,
                ExerciseName = log.ExerciseName,
                Date = log.Date,
                DurationMinutes = log.DurationMinutes,
                CaloriesBurned = log.CaloriesBurned,
                Notes = log.Notes
            });
        }

        // POST: Logs/EditWorkout/5  -  save the changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorkout(int id, WorkoutLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // check ownership again on POST
            await CheckOwnPlanAsync(clientId, input.WorkoutPlanId);
            if (!ModelState.IsValid)
            {
                await LoadPlansAsync(clientId);
                ViewBag.LogId = id;
                return View(input);                                                // show the form again with errors
            }
            // Copy the new values onto the existing entity
            log.WorkoutPlanId = input.WorkoutPlanId;
            log.ExerciseName = input.ExerciseName.Trim();
            log.Date = input.Date.Date;
            log.DurationMinutes = input.DurationMinutes;
            log.CaloriesBurned = input.CaloriesBurned;
            log.Notes = input.Notes?.Trim();
            _workouts.Update(log);                                                 // mark it as changed
            try
            {
                await _workouts.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.EditWorkout");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout could not be saved. Please try again.");
                await LoadPlansAsync(clientId);
                ViewBag.LogId = id;
                return View(input);
            }

            TempData["Success"] = "Workout updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Logs/DeleteWorkout/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkout(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // only their own workout
            _workouts.Delete(log);                                                 // mark it for deletion
            try
            {
                await _workouts.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.DeleteWorkout");
                TempData["Error"] = "Sorry, the workout could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Workout deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== MEALS =====================

        // GET: Logs/AddMeal  -  empty form
        [HttpGet]
        public async Task<IActionResult> AddMeal()
        {
            if (await ActiveClientIdAsync() is null) return Forbid();             // active clients only
            return View(new MealLogCreateViewModel());                             // empty form (date = today)
        }

        // POST: Logs/AddMeal  -  save a new meal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(MealLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            if (!ModelState.IsValid) return View(input);                           // show the form again with errors
            await _meals.AddAsync(new MealLog
            {
                ClientId = clientId,                                               // from the signed-in user
                Date = input.Date.Date,
                MealType = input.MealType,
                FoodItems = input.FoodItems.Trim(),
                Calories = input.Calories
            });
            try
            {
                await _meals.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.AddMeal");
                TempData["Error"] = "Sorry, the meal could not be changed. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Meal logged.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Logs/EditMeal/5  -  form filled with the current values
        [HttpGet]
        public async Task<IActionResult> EditMeal(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // only their own meal
            ViewBag.LogId = id;
            return View(new MealLogCreateViewModel
            {
                Date = log.Date, MealType = log.MealType,                          // copy the entity...
                FoodItems = log.FoodItems, Calories = log.Calories                 // ...into the form model
            });
        }

        // POST: Logs/EditMeal/5  -  save the changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeal(int id, MealLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // check ownership again on POST
            if (!ModelState.IsValid)
            {
                ViewBag.LogId = id;
                return View(input);
            }
            log.Date = input.Date.Date;                                            // copy the new values
            log.MealType = input.MealType;
            log.FoodItems = input.FoodItems.Trim();
            log.Calories = input.Calories;
            _meals.Update(log);                                                    // mark it as changed
            try
            {
                await _meals.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.EditMeal");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the meal could not be saved. Please try again.");
                ViewBag.LogId = id;
                return View(input);
            }

            TempData["Success"] = "Meal updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Logs/DeleteMeal/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // only their own meal
            _meals.Delete(log);
            try
            {
                await _meals.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.DeleteMeal");
                TempData["Error"] = "Sorry, the meal could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Meal deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== MEASUREMENTS =====================

        // GET: Logs/AddMeasurement  -  empty form
        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            if (await ActiveClientIdAsync() is null) return Forbid();             // active clients only
            return View(new HealthMeasurementCreateViewModel());
        }

        // POST: Logs/AddMeasurement  -  save a new measurement (BMI is calculated, not stored)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeasurement(HealthMeasurementCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            if (!ModelState.IsValid) return View(input);
            await _measurements.AddAsync(new HealthMeasurement
            {
                ClientId = clientId,                                               // from the signed-in user
                Date = input.Date.Date,
                WeightKg = input.WeightKg,
                HeightCm = input.HeightCm,
                RestingHeartRate = input.RestingHeartRate
                // Sensitive notes are deliberately not stored without encryption.
            });
            try
            {
                await _measurements.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.AddMeasurement");
                TempData["Error"] = "Sorry, the measurement could not be changed. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Measurement saved.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Logs/EditMeasurement/5  -  form filled with the current values
        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // only their own measurement
            ViewBag.LogId = id;
            return View(new HealthMeasurementCreateViewModel
            {
                Date = log.Date, WeightKg = log.WeightKg,                          // copy the entity...
                HeightCm = log.HeightCm, RestingHeartRate = log.RestingHeartRate   // ...into the form model
            });
        }

        // POST: Logs/EditMeasurement/5  -  save the changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeasurement(int id, HealthMeasurementCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // check ownership again on POST
            if (!ModelState.IsValid)
            {
                ViewBag.LogId = id;
                return View(input);
            }
            log.Date = input.Date.Date;                                            // copy the new values
            log.WeightKg = input.WeightKg;
            log.HeightCm = input.HeightCm;
            log.RestingHeartRate = input.RestingHeartRate;
            _measurements.Update(log);                                             // mark it as changed
            try
            {
                await _measurements.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.EditMeasurement");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the measurement could not be saved. Please try again.");
                ViewBag.LogId = id;
                return View(input);
            }

            TempData["Success"] = "Measurement updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Logs/DeleteMeasurement/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();        // only their own measurement
            _measurements.Delete(log);
            try
            {
                await _measurements.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.DeleteMeasurement");
                TempData["Error"] = "Sorry, the measurement could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Measurement deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== HELPERS =====================

        // Returns the signed-in user's id if their account is active, otherwise null
        private async Task<string?> ActiveClientIdAsync()
        {
            var id = _users.GetUserId(User);                                       // the signed-in user's id
            if (id is null) return null;                                           // not signed in
            var user = await _users.FindByIdAsync(id);                             // load the account
            return user is { IsActive: true } ? id : null;                         // deactivated = null
        }

        // Adds a form error if the chosen plan is not one of this client's plans
        private async Task CheckOwnPlanAsync(string clientId, int? planId)
        {
            if (!planId.HasValue) return;                                          // "No specific plan" is always fine
            var plan = await _plans.GetByIdAsync(planId.Value);                    // load the plan
            if (plan is null || plan.ClientId != clientId)                         // missing or someone else's
                ModelState.AddModelError(nameof(WorkoutLogCreateViewModel.WorkoutPlanId),
                    "Choose one of your workout plans.");
        }

        // Fills ViewBag.Plans with this client's plans for the drop-down
        private async Task LoadPlansAsync(string clientId)
        {
            var plans = await _plans.FindAsync(p => p.ClientId == clientId);      // only their own plans
            ViewBag.Plans = plans.OrderByDescending(p => p.CreatedAt)              // newest first
                .Select(p => new SelectListItem(p.Title, p.Id.ToString())).ToList();   // text = title, value = id
        }
    }
}
