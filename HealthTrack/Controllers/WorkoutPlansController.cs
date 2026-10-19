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
    public class WorkoutPlansController : Controller
    {
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<TrainerAssignment> _assignments;
        private readonly IRepository<WorkoutPlanItem> _workoutPlanItems;
        private readonly IRepository<WorkoutLog> _workoutLogs;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WorkoutPlansController> _logger;

        public WorkoutPlansController(
            IRepository<WorkoutPlan> workoutPlans,
            IRepository<TrainerAssignment> assignments,
            IRepository<WorkoutPlanItem> workoutPlanItems,
            IRepository<WorkoutLog> workoutLogs,
            UserManager<ApplicationUser> userManager,
            ILogger<WorkoutPlansController> logger)
        {
            _workoutPlans = workoutPlans;
            _assignments = assignments;
            _workoutPlanItems = workoutPlanItems;
            _workoutLogs = workoutLogs;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Show only plans created by the signed-in trainer.
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plans = await _workoutPlans.FindAsync(
                plan => plan.TrainerId == trainerId);

            return View(plans
                .OrderByDescending(plan => plan.CreatedAt)
                .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            // Supply the form with clients assigned to this trainer.
            await LoadAssignedClientsAsync(trainerId);

            return View(new WorkoutPlanCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkoutPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            if (ModelState.IsValid)
            {
                // Verify the selected client belongs to this trainer.
                // Changing the dropdown value in the browser must not bypass this rule.
                var assignments = await _assignments.FindAsync(
                    assignment =>
                        assignment.TrainerId == trainerId &&
                        assignment.ClientId == input.ClientId &&
                        assignment.IsActive);

                var client = await _userManager.FindByIdAsync(input.ClientId);

                if (!assignments.Any() ||
                    client is null ||
                    !client.IsActive ||
                    !await _userManager.IsInRoleAsync(client, AppRoles.Client))
                {
                    ModelState.AddModelError(
                        nameof(input.ClientId),
                        "Select a client assigned to you.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Refill the dropdown when returning to the form with errors.
                await LoadAssignedClientsAsync(trainerId);
                return View(input);
            }

            var plan = new WorkoutPlan
            {
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                Goal = input.Goal?.Trim(),
                Difficulty = input.Difficulty,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                ClientId = input.ClientId,

                // Get the trainer from Identity, never from submitted form data.
                TrainerId = trainerId
            };

            await _workoutPlans.AddAsync(plan);
            try
            {
                await _workoutPlans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.Create");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout plan could not be saved. Please try again.");
                // Refill the dropdown when returning to the form with errors.
                await LoadAssignedClientsAsync(trainerId);
                return View(input);
            }

            TempData["Success"] = "Workout plan created.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Trainers may edit only plans they created.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            await LoadAssignedClientsAsync(trainerId);
            ViewBag.PlanId = plan.Id;

            // Use the existing form model so the same validation rules apply.
            var input = new WorkoutPlanCreateViewModel
            {
                ClientId = plan.ClientId,
                Title = plan.Title,
                Description = plan.Description,
                Goal = plan.Goal,
                Difficulty = plan.Difficulty,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate
            };

            return View(input);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            WorkoutPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Check ownership on submission too; the URL can be changed.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            // Existing workout logs refer to this plan. Keep them with their client.
            if (input.ClientId != plan.ClientId &&
                (await _workoutLogs.FindAsync(log => log.WorkoutPlanId == id)).Any())
            {
                ModelState.AddModelError(nameof(input.ClientId),
                    "This plan has workout logs. Create a new plan for another client.");
            }

            if (ModelState.IsValid)
            {
                // A trainer may assign a plan only to their active client.
                var assignments = await _assignments.FindAsync(
                    assignment =>
                        assignment.TrainerId == trainerId &&
                        assignment.ClientId == input.ClientId &&
                        assignment.IsActive);

                var client = await _userManager.FindByIdAsync(input.ClientId);

                if (!assignments.Any() ||
                    client is null ||
                    !client.IsActive ||
                    !await _userManager.IsInRoleAsync(client, AppRoles.Client))
                {
                    ModelState.AddModelError(
                        nameof(input.ClientId),
                        "Select a client assigned to you.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadAssignedClientsAsync(trainerId);
                ViewBag.PlanId = plan.Id;
                return View(input);
            }

            // Update permitted fields; keep Id, TrainerId, and CreatedAt intact.
            plan.ClientId = input.ClientId;
            plan.Title = input.Title.Trim();
            plan.Description = input.Description?.Trim();
            plan.Goal = input.Goal?.Trim();
            plan.Difficulty = input.Difficulty;
            plan.StartDate = input.StartDate;
            plan.EndDate = input.EndDate;

            _workoutPlans.Update(plan);
            try
            {
                await _workoutPlans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.Edit");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout plan could not be saved. Please try again.");
                await LoadAssignedClientsAsync(trainerId);
                ViewBag.PlanId = plan.Id;
                return View(input);
            }

            TempData["Success"] = "Workout plan updated.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // A trainer can view only a plan they created.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            // The generic repository does not automatically load the Items list,
            // so fetch this plan's exercises separately.
            var exercises = await _workoutPlanItems.FindAsync(
                item => item.WorkoutPlanId == plan.Id);

            var client = await _userManager.FindByIdAsync(plan.ClientId);

            var model = new WorkoutPlanDetailsViewModel
            {
                Plan = plan,
                ClientName = client?.FullName ?? "Unknown client",
                Exercises = exercises
                    .OrderBy(item => item.DayOfWeek)
                    .ThenBy(item => item.Id)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Do not display another trainer's plan.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            ViewBag.PlanId = plan.Id;
            ViewBag.PlanTitle = plan.Title;

            return View(new WorkoutPlanItemCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExercise(
            int id,
            WorkoutPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Check ownership again when the trainer submits the form.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            var exercise = new WorkoutPlanItem
            {
                // Use the plan ID we verified above.
                WorkoutPlanId = plan.Id,
                ExerciseName = input.ExerciseName.Trim(),
                DayOfWeek = input.DayOfWeek,
                Sets = input.Sets,
                Reps = input.Reps,
                DurationMinutes = input.DurationMinutes,
                Notes = input.Notes?.Trim()
            };

            await _workoutPlanItems.AddAsync(exercise);
            try
            {
                await _workoutPlanItems.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.AddExercise");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the exercise could not be saved. Please try again.");
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            // Show the updated exercise list after saving.
            TempData["Success"] = "Exercise added.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var exercise = await _workoutPlanItems.GetByIdAsync(id);
            if (exercise is null) return NotFound();

            var plan = await _workoutPlans.GetByIdAsync(exercise.WorkoutPlanId);

            // Only the trainer who owns this plan may edit its exercises.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            ViewBag.ExerciseId = exercise.Id;
            ViewBag.PlanId = plan.Id;
            ViewBag.PlanTitle = plan.Title;

            // Reuse the exercise form model and its validation rules.
            return View(new WorkoutPlanItemCreateViewModel
            {
                ExerciseName = exercise.ExerciseName,
                DayOfWeek = exercise.DayOfWeek,
                Sets = exercise.Sets,
                Reps = exercise.Reps,
                DurationMinutes = exercise.DurationMinutes,
                Notes = exercise.Notes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExercise(
            int id,
            WorkoutPlanItemCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var exercise = await _workoutPlanItems.GetByIdAsync(id);
            if (exercise is null) return NotFound();

            var plan = await _workoutPlans.GetByIdAsync(exercise.WorkoutPlanId);

            // Check ownership again when the form is submitted.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ExerciseId = exercise.Id;
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            // Update only the fields the trainer is allowed to edit.
            exercise.ExerciseName = input.ExerciseName.Trim();
            exercise.DayOfWeek = input.DayOfWeek;
            exercise.Sets = input.Sets;
            exercise.Reps = input.Reps;
            exercise.DurationMinutes = input.DurationMinutes;
            exercise.Notes = input.Notes?.Trim();

            _workoutPlanItems.Update(exercise);
            try
            {
                await _workoutPlanItems.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.EditExercise");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the exercise could not be saved. Please try again.");
                ViewBag.ExerciseId = exercise.Id;
                ViewBag.PlanId = plan.Id;
                ViewBag.PlanTitle = plan.Title;
                return View(input);
            }

            TempData["Success"] = "Exercise updated.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var exercise = await _workoutPlanItems.GetByIdAsync(id);
            if (exercise is null) return NotFound();
            var plan = await _workoutPlans.GetByIdAsync(exercise.WorkoutPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();

            _workoutPlanItems.Delete(exercise);
            try
            {
                await _workoutPlanItems.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.DeleteExercise");
                TempData["Error"] = "Sorry, the exercise could not be deleted. Please try again.";
                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }

            TempData["Success"] = "Exercise removed.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _workoutPlans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();
            return View(plan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _workoutPlans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();

            // Keep a client's historical workout log when its plan is removed.
            foreach (var log in await _workoutLogs.FindAsync(x => x.WorkoutPlanId == id))
            {
                log.WorkoutPlanId = null;
                _workoutLogs.Update(log);
            }
            _workoutPlans.Delete(plan);
            try
            {
                await _workoutPlans.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.DeleteConfirmed");
                TempData["Error"] = "Sorry, the workout plan could not be deleted. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Workout plan deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> ActiveTrainerIdAsync()
        {
            var id = _userManager.GetUserId(User);
            if (id is null) return null;
            var trainer = await _userManager.FindByIdAsync(id);
            return trainer is { IsActive: true } ? id : null;
        }

        private async Task LoadAssignedClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(
                assignment =>
                    assignment.TrainerId == trainerId &&
                    assignment.IsActive);

            var clients = new List<SelectListItem>();

            foreach (var clientId in assignments
                .Select(assignment => assignment.ClientId)
                .Distinct())
            {
                var client = await _userManager.FindByIdAsync(clientId);

                if (client is not null &&
                    client.IsActive &&
                    await _userManager.IsInRoleAsync(client, AppRoles.Client))
                {
                    clients.Add(new SelectListItem
                    {
                        Value = client.Id,
                        Text = client.FullName
                    });
                }
            }

            ViewBag.Clients = clients
                .OrderBy(client => client.Text)
                .ToList();
        }
    }
}
