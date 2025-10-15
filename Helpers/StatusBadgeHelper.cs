using DmsProjeckt.Data;

namespace DmsProjeckt.Helpers
{
    public static class StatusBadgeHelper
    {
        public static string GetStatusBadgeClass(DokumentStatus status) => status switch
        {
            DokumentStatus.Neu => "bg-secondary",
            DokumentStatus.InBearbeitung => "bg-warning text-dark",
            DokumentStatus.Fertig => "bg-success",
            DokumentStatus.Fehlerhaft => "bg-danger",
            _ => "bg-dark"
        };

        // 🔥 Surcharge pour string
        public static string GetStatusBadgeClass(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "bg-dark";

            return Enum.TryParse<DokumentStatus>(status, out var parsed)
                ? GetStatusBadgeClass(parsed)
                : "bg-dark";
        }
    }

}
