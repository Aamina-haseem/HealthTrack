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
    [Authorize(Roles = AppRoles.Client)]
    public class LogsController : Controller
    {
        private readonly IRepository<WorkoutLog> _workouts;
        private readonly IRepository<MealLog> _meals;
        private readonly IRepository<HealthMeasurement> _measurements;
        private readonly IRepository<WorkoutPlan> _plans;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<LogsController> _logger;

        public LogsController(IRepository<WorkoutLog> workouts,
            IRepository<MealLog> meals,
            IRepository<HealthMeasurement> measurements,
            IRepository<WorkoutPlan> plans,
            UserManager<ApplicationUser> users,
            ILogger<LogsController> logger)
        {
            _workouts = workouts;
            _meals = meals;
            _measurements = measurements;
            _plans = plans;
            _users = users;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            return View(new ClientLogsViewModel
            {
                Workouts = (await _workouts.FindAsync(x => x.ClientId == clientId))
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList(),
                Meals = (await _meals.FindAsync(x => x.ClientId == clientId))
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList(),
                Measurements = (await _measurements.FindAsync(x => x.ClientId == clientId))
                    .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> AddWorkout()
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            await LoadPlansAsync(clientId);
            return View(new WorkoutLogCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkout(WorkoutLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            await CheckOwnPlanAsync(clientId, input.WorkoutPlanId);
            if (!ModelState.IsValid)
            {
                await LoadPlansAsync(clientId);
                return View(input);
            }
            await _workouts.AddAsync(new WorkoutLog
            {
                ClientId = clientId,
                WorkoutPlanId = input.WorkoutPlanId,
                ExerciseName = input.ExerciseName.Trim(),
                Date = input.Date.Date,
                DurationMinutes = input.DurationMinutes,
                CaloriesBurned = input.CaloriesBurned,
                Notes = input.Notes?.Trim()
            });
            try
            {
                await _workouts.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in LogsController.AddWorkout");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout could not be saved. Please try again.");
                await LoadPlansAsync(clientId);
                return View(input);
            }

            TempData["Success"] = "Workout logged.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditWorkout(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            await LoadPlansAsync(clientId);
            ViewBag.LogId = id;
            return View(new WorkoutLogCreateViewModel
            {
                WorkoutPlanId = log.WorkoutPlanId,
                ExerciseName = log.ExerciseName,
                Date = log.Date,
                DurationMinutes = log.DurationMinutes,
                CaloriesBurned = log.CaloriesBurned,
                Notes = log.Notes
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorkout(int id, WorkoutLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            await CheckOwnPlanAsync(clientId, input.WorkoutPlanId);
            if (!ModelState.IsValid)
            {
                await LoadPlansAsync(clientId);
                ViewBag.LogId = id;
                return View(input);
            }
            log.WorkoutPlanId = input.WorkoutPlanId;
            log.ExerciseName = input.ExerciseName.Trim();
            log.Date = input.Date.Date;
            log.DurationMinutes = input.DurationMinutes;
            log.CaloriesBurned = input.CaloriesBurned;
            log.Notes = input.Notes?.Trim();
            _workouts.Update(log);
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkout(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _workouts.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            _workouts.Delete(log);
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

        [HttpGet]
        public async Task<IActionResult> AddMeal()
        {
            if (await ActiveClientIdAsync() is null) return Forbid();
            return View(new MealLogCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(MealLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            if (!ModelState.IsValid) return View(input);
            await _meals.AddAsync(new MealLog
            {
                ClientId = clientId,
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

        [HttpGet]
        public async Task<IActionResult> EditMeal(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            ViewBag.LogId = id;
            return View(new MealLogCreateViewModel
            {
                Date = log.Date, MealType = log.MealType,
                FoodItems = log.FoodItems, Calories = log.Calories
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeal(int id, MealLogCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.LogId = id;
                return View(input);
            }
            log.Date = input.Date.Date;
            log.MealType = input.MealType;
            log.FoodItems = input.FoodItems.Trim();
            log.Calories = input.Calories;
            _meals.Update(log);
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _meals.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
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

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            if (await ActiveClientIdAsync() is null) return Forbid();
            return View(new HealthMeasurementCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeasurement(HealthMeasurementCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            if (!ModelState.IsValid) return View(input);
            await _measurements.AddAsync(new HealthMeasurement
            {
                ClientId = clientId,
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

        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            ViewBag.LogId = id;
            return View(new HealthMeasurementCreateViewModel
            {
                Date = log.Date, WeightKg = log.WeightKg,
                HeightCm = log.HeightCm, RestingHeartRate = log.RestingHeartRate
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeasurement(int id, HealthMeasurementCreateViewModel input)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.LogId = id;
                return View(input);
            }
            log.Date = input.Date.Date;
            log.WeightKg = input.WeightKg;
            log.HeightCm = input.HeightCm;
            log.RestingHeartRate = input.RestingHeartRate;
            _measurements.Update(log);
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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(int id)
        {
            var clientId = await ActiveClientIdAsync();
            if (clientId is null) return Forbid();
            var log = await _measurements.GetByIdAsync(id);
            if (log is null || log.ClientId != clientId) return NotFound();
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

        private async Task<string?> ActiveClientIdAsync()
        {
            var id = _users.GetUserId(User);
            if (id is null) return null;
            var user = await _users.FindByIdAsync(id);
            return user is { IsActive: true } ? id : null;
        }

        private async Task CheckOwnPlanAsync(string clientId, int? planId)
        {
            if (!planId.HasValue) return;
            var plan = await _plans.GetByIdAsync(planId.Value);
            if (plan is null || plan.ClientId != clientId)
                ModelState.AddModelError(nameof(WorkoutLogCreateViewModel.WorkoutPlanId),
                    "Choose one of your workout plans.");
        }

        private async Task LoadPlansAsync(string clientId)
        {
            var plans = await _plans.FindAsync(p => p.ClientId == clientId);
            ViewBag.Plans = plans.OrderByDescending(p => p.CreatedAt)
                .Select(p => new SelectListItem(p.Title, p.Id.ToString())).ToList();
        }
    }
}
