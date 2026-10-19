using HealthTrack.Models;
using HealthTrack.Repositories.Interfaces;
using HealthTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthTrack.Controllers
{
    [Authorize(Roles = AppRoles.Trainer)]
    public class WorkoutPlansController : Controller
    {
        private readonly IRepository<WorkoutPlan> _workoutPlans;
        private readonly IRepository<TrainerAssignment> _assignments;
        private readonly IRepository<WorkoutPlanItem> _workoutPlanItems;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorkoutPlansController(
            IRepository<WorkoutPlan> workoutPlans,
            IRepository<TrainerAssignment> assignments,
            IRepository<WorkoutPlanItem> workoutPlanItems,
            UserManager<ApplicationUser> userManager)
        {
            _workoutPlans = workoutPlans;
            _assignments = assignments;
            _workoutPlanItems = workoutPlanItems;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Show only plans created by the signed-in trainer.
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

            var plans = await _workoutPlans.FindAsync(
                plan => plan.TrainerId == trainerId);

            return View(plans
                .OrderByDescending(plan => plan.CreatedAt)
                .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

            // Supply the form with clients assigned to this trainer.
            await LoadAssignedClientsAsync(trainerId);

            return View(new WorkoutPlanCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkoutPlanCreateViewModel input)
        {
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

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
            await _workoutPlans.SaveAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

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
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

            var plan = await _workoutPlans.GetByIdAsync(id);

            // Check ownership on submission too; the URL can be changed.
            if (plan is null || plan.TrainerId != trainerId)
                return NotFound();

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
            await _workoutPlans.SaveAsync();

            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

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
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

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
            var trainerId = _userManager.GetUserId(User);
            if (trainerId is null) return Challenge();

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
            await _workoutPlanItems.SaveAsync();

            // Show the updated exercise list after saving.
            return RedirectToAction(nameof(Details), new { id = plan.Id });
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