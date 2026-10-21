// =====================================================================
// FILE:    Controllers/WorkoutPlansController.cs
// PURPOSE: Trainers create and manage workout plans for their clients (Task 5):
//          list, create, view details, edit and delete a plan, and add / edit /
//          remove the exercises inside it.
// SECURITY: [Authorize(Roles = Trainer)]; every action checks the trainer is
//           active (ActiveTrainerIdAsync) and that the plan belongs to them
//           (plan.TrainerId == trainerId, otherwise 404 - checked again on every
//           POST because a URL can be edited). The chosen client must be actively
//           assigned to the trainer, and the trainer id always comes from Identity.
// DATA RULES: a plan that already has workout logs cannot be moved to another
//             client; deleting a plan keeps the client's workout logs (they are
//             simply unlinked from the plan).
// PATTERN: GET shows a form; POST validates, saves inside try/catch
//          (DbUpdateException) and redirects (Post-Redirect-Get) with TempData.
// VIEWS:   Views/WorkoutPlans/*.cshtml
// =====================================================================

using HealthTrack.Models;                  // WorkoutPlan, WorkoutPlanItem, WorkoutLog, TrainerAssignment, AppRoles
using HealthTrack.Repositories.Interfaces; // IRepository<T>
using HealthTrack.ViewModels;              // WorkoutPlanCreateViewModel, WorkoutPlanItemCreateViewModel, ...
using Microsoft.AspNetCore.Authorization;  // [Authorize]
using Microsoft.AspNetCore.Identity;       // UserManager
using Microsoft.AspNetCore.Mvc;            // Controller, IActionResult, [HttpGet], [HttpPost]
using Microsoft.AspNetCore.Mvc.Rendering;  // SelectListItem (client drop-down)
using Microsoft.EntityFrameworkCore;       // DbUpdateException

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]                                  // every action: trainers only
    public class WorkoutPlansController : Controller
    {
        private readonly IRepository<WorkoutPlan> _workoutPlans;           // workout plans table
        private readonly IRepository<TrainerAssignment> _assignments;      // trainer <-> client links
        private readonly IRepository<WorkoutPlanItem> _workoutPlanItems;   // exercises table
        private readonly IRepository<WorkoutLog> _workoutLogs;             // client workout logs (linked to plans)
        private readonly UserManager<ApplicationUser> _userManager;        // users and roles
        private readonly ILogger<WorkoutPlansController> _logger;          // writes errors to the log

        // Constructor: ASP.NET Core passes in every dependency
        public WorkoutPlansController(
            IRepository<WorkoutPlan> workoutPlans,
            IRepository<TrainerAssignment> assignments,
            IRepository<WorkoutPlanItem> workoutPlanItems,
            IRepository<WorkoutLog> workoutLogs,
            UserManager<ApplicationUser> userManager,
            ILogger<WorkoutPlansController> logger)
        {
            _workoutPlans = workoutPlans;            // keep each dependency in a field
            _assignments = assignments;
            _workoutPlanItems = workoutPlanItems;
            _workoutLogs = workoutLogs;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: WorkoutPlans  -  the trainer's own plans
        public async Task<IActionResult> Index()
        {
            // Show only plans created by the signed-in trainer.
            var trainerId = await ActiveTrainerIdAsync();                  // null if not an active trainer
            if (trainerId is null) return Forbid();                        // refuse

            var plans = await _workoutPlans.FindAsync(
                plan => plan.TrainerId == trainerId);                      // only this trainer's plans

            return View(plans
                .OrderByDescending(plan => plan.CreatedAt)                 // newest first
                .ToList());
        }

        // GET: WorkoutPlans/Create  -  empty form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            // Supply the form with clients assigned to this trainer.
            await LoadAssignedClientsAsync(trainerId);

            return View(new WorkoutPlanCreateViewModel());                 // empty form with defaults
        }

        // POST: WorkoutPlans/Create  -  save a new plan
        [HttpPost]
        [ValidateAntiForgeryToken]                                         // must carry a valid anti-forgery token
        public async Task<IActionResult> Create(WorkoutPlanCreateViewModel input)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            if (ModelState.IsValid)                                        // only check the client if the form is otherwise valid
            {
                // Verify the selected client belongs to this trainer.
                // Changing the dropdown value in the browser must not bypass this rule.
                var assignments = await _assignments.FindAsync(
                    assignment =>
                        assignment.TrainerId == trainerId &&
                        assignment.ClientId == input.ClientId &&
                        assignment.IsActive);                              // an active link to this client?

                var client = await _userManager.FindByIdAsync(input.ClientId);   // load the client

                if (!assignments.Any() ||                                  // not assigned to this trainer...
                    client is null ||                                      // ...or no such user...
                    !client.IsActive ||                                    // ...or deactivated...
                    !await _userManager.IsInRoleAsync(client, AppRoles.Client))   // ...or not a client
                {
                    ModelState.AddModelError(
                        nameof(input.ClientId),
                        "Select a client assigned to you.");               // shown under the Client field
                }
            }

            if (!ModelState.IsValid)
            {
                // Refill the dropdown when returning to the form with errors.
                await LoadAssignedClientsAsync(trainerId);
                return View(input);
            }

            // Copy the validated form values into a new entity
            var plan = new WorkoutPlan
            {
                Title = input.Title.Trim(),                                // remove extra spaces
                Description = input.Description?.Trim(),                   // optional
                Goal = input.Goal?.Trim(),                                 // optional
                Difficulty = input.Difficulty,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                ClientId = input.ClientId,                                 // already checked above

                // Get the trainer from Identity, never from submitted form data.
                TrainerId = trainerId
            };

            await _workoutPlans.AddAsync(plan);                            // mark it to be inserted
            try
            {
                await _workoutPlans.SaveAsync();                           // write it to the database
            }
            catch (DbUpdateException ex)                                   // the database save failed
            {
                // Log the technical details for the developer; the user only sees a simple message.
                _logger.LogError(ex, "Database save failed in WorkoutPlansController.Create");
                ModelState.AddModelError(string.Empty,
                    "Sorry, the workout plan could not be saved. Please try again.");
                // Refill the dropdown when returning to the form with errors.
                await LoadAssignedClientsAsync(trainerId);
                return View(input);
            }

            TempData["Success"] = "Workout plan created.";                 // green message on the next page
            return RedirectToAction(nameof(Index));                        // back to the list
        }
        // GET: WorkoutPlans/Edit/5  -  form filled with the current values
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);               // load the plan

            // Trainers may edit only plans they created.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            await LoadAssignedClientsAsync(trainerId);                     // fill the client drop-down
            ViewBag.PlanId = plan.Id;                                      // used by the form's URL

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

        // POST: WorkoutPlans/Edit/5  -  save the changes
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
            if (input.ClientId != plan.ClientId &&                         // the client is being changed...
                (await _workoutLogs.FindAsync(log => log.WorkoutPlanId == id)).Any())   // ...but logs use this plan
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

                if (!assignments.Any() ||                                  // same checks as in Create
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
                return View(input);                                        // show the form again with errors
            }

            // Update permitted fields; keep Id, TrainerId, and CreatedAt intact.
            plan.ClientId = input.ClientId;
            plan.Title = input.Title.Trim();
            plan.Description = input.Description?.Trim();
            plan.Goal = input.Goal?.Trim();
            plan.Difficulty = input.Difficulty;
            plan.StartDate = input.StartDate;
            plan.EndDate = input.EndDate;

            _workoutPlans.Update(plan);                                    // mark it as changed
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
            return RedirectToAction(nameof(Details), new { id = plan.Id });   // back to the plan
        }

        // GET: WorkoutPlans/Details/5
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

            var client = await _userManager.FindByIdAsync(plan.ClientId);  // for the client's name

            var model = new WorkoutPlanDetailsViewModel
            {
                Plan = plan,
                ClientName = client?.FullName ?? "Unknown client",         // fallback if the user is missing
                Exercises = exercises
                    .OrderBy(item => item.DayOfWeek)                       // Sunday to Saturday...
                    .ThenBy(item => item.Id)                               // ...then in the order they were added
                    .ToList()
            };

            return View(model);
        }

        // GET: WorkoutPlans/AddExercise/5  (5 = the plan id)
        [HttpGet]
        public async Task<IActionResult> AddExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Do not display another trainer's plan.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            ViewBag.PlanId = plan.Id;                                      // used by the form's URL
            ViewBag.PlanTitle = plan.Title;                                // shown on the page

            return View(new WorkoutPlanItemCreateViewModel());             // empty exercise form
        }

        // POST: WorkoutPlans/AddExercise/5  -  save a new exercise
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

            if (!ModelState.IsValid)                                       // e.g. no sets/reps and no duration
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

            await _workoutPlanItems.AddAsync(exercise);                    // mark it to be inserted
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

        // GET: WorkoutPlans/EditExercise/7  (7 = the exercise id)
        [HttpGet]
        public async Task<IActionResult> EditExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var exercise = await _workoutPlanItems.GetByIdAsync(id);       // load the exercise
            if (exercise is null) return NotFound();

            var plan = await _workoutPlans.GetByIdAsync(exercise.WorkoutPlanId);   // and its plan

            // Only the trainer who owns this plan may edit its exercises.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

            ViewBag.ExerciseId = exercise.Id;                              // used by the form's URL
            ViewBag.PlanId = plan.Id;                                      // used by the "Back to Plan" link
            ViewBag.PlanTitle = plan.Title;                                // shown on the page

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

        // POST: WorkoutPlans/EditExercise/7  -  save the changes
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

            _workoutPlanItems.Update(exercise);                            // mark it as changed
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

        // POST: WorkoutPlans/DeleteExercise/7  -  remove one exercise
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();

            var exercise = await _workoutPlanItems.GetByIdAsync(id);
            if (exercise is null) return NotFound();
            var plan = await _workoutPlans.GetByIdAsync(exercise.WorkoutPlanId);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only from their own plan

            _workoutPlanItems.Delete(exercise);                            // mark it for deletion
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

        // GET: WorkoutPlans/Delete/5  -  "are you sure?" page
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _workoutPlans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // only their own plan
            return View(plan);                                             // show the confirmation page
        }

        // POST: WorkoutPlans/Delete/5  -  really delete the plan
        [HttpPost, ActionName("Delete")]                                   // same URL as the GET above
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainerId = await ActiveTrainerIdAsync();
            if (trainerId is null) return Forbid();
            var plan = await _workoutPlans.GetByIdAsync(id);
            if (plan is null || plan.TrainerId != trainerId) return NotFound();   // check ownership again on POST

            // Keep a client's historical workout log when its plan is removed.
            foreach (var log in await _workoutLogs.FindAsync(x => x.WorkoutPlanId == id))
            {
                log.WorkoutPlanId = null;                                  // unlink the log from the plan
                _workoutLogs.Update(log);
            }
            _workoutPlans.Delete(plan);                                    // its exercises are deleted with it (cascade)
            try
            {
                await _workoutPlans.SaveAsync();                           // one save for the unlinks and the delete
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

        // Returns the signed-in user's id if they are an ACTIVE trainer, otherwise null
        private async Task<string?> ActiveTrainerIdAsync()
        {
            var id = _userManager.GetUserId(User);                         // the signed-in user's id
            if (id is null) return null;                                   // not signed in
            var trainer = await _userManager.FindByIdAsync(id);            // load the account
            return trainer is { IsActive: true } ? id : null;              // deactivated = null
        }

        // Fills ViewBag.Clients with the trainer's active clients for the drop-down
        private async Task LoadAssignedClientsAsync(string trainerId)
        {
            var assignments = await _assignments.FindAsync(
                assignment =>
                    assignment.TrainerId == trainerId &&
                    assignment.IsActive);                                  // this trainer's active links

            var clients = new List<SelectListItem>();                      // the drop-down options

            foreach (var clientId in assignments
                .Select(assignment => assignment.ClientId)
                .Distinct())                                               // each client once
            {
                var client = await _userManager.FindByIdAsync(clientId);   // load the user

                if (client is not null &&
                    client.IsActive &&
                    await _userManager.IsInRoleAsync(client, AppRoles.Client))   // active client only
                {
                    clients.Add(new SelectListItem
                    {
                        Value = client.Id,                                 // what the form posts
                        Text = client.FullName                             // what the trainer sees
                    });
                }
            }

            ViewBag.Clients = clients
                .OrderBy(client => client.Text)                            // alphabetical
                .ToList();
        }
    }
}
