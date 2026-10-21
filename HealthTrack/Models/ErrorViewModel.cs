// =====================================================================
// FILE:    Models/ErrorViewModel.cs
// PURPOSE: Data for the friendly error page (Views/Shared/Error.cshtml). It
//          carries the request id so a problem can be traced in the logs,
//          without showing any technical details to the user.
// USED BY: HomeController.Error (part of the original project template).
// =====================================================================

namespace HealthTrack.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }                                // id of the request that failed

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);        // only show the id when there is one
    }
}
