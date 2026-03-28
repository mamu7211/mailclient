namespace Feirb.Web.Components.UI;

/// <summary>
/// Curated set of Bootstrap Icon names available in Feirb.
/// Use these constants with the <see cref="Icon"/> component.
/// </summary>
public static class FeirbIcons
{
    // ── Navigation & Layout ──
    public const string ArrowLeft = "arrow-left";
    public const string ArrowRight = "arrow-right";
    public const string ArrowUp = "arrow-up";
    public const string ArrowDown = "arrow-down";
    public const string BoxArrowRight = "box-arrow-right";
    public const string ChevronDown = "chevron-down";
    public const string ChevronLeft = "chevron-left";
    public const string ChevronRight = "chevron-right";
    public const string ChevronUp = "chevron-up";
    public const string House = "house";
    public const string List = "list";
    public const string Speedometer2 = "speedometer2";
    public const string ThreeDotsVertical = "three-dots-vertical";

    // ── Mail ──
    public const string Archive = "archive";
    public const string Envelope = "envelope";
    public const string EnvelopeArrowUp = "envelope-arrow-up";
    public const string EnvelopeFill = "envelope-fill";
    public const string EnvelopeOpen = "envelope-open";
    public const string EnvelopePaper = "envelope-paper";
    public const string Inbox = "inbox";
    public const string Paperclip = "paperclip";
    public const string Reply = "reply";
    public const string ReplyAll = "reply-all";
    public const string Forward = "forward";
    public const string Send = "send";
    public const string Trash = "trash";

    // ── Actions & Editing ──
    public const string Check = "check";
    public const string CheckLg = "check-lg";
    public const string Clipboard = "clipboard";
    public const string Copy = "copy";
    public const string Download = "download";
    public const string Pencil = "pencil";
    public const string PencilFill = "pencil-fill";
    public const string PencilSquare = "pencil-square";
    public const string PlusLg = "plus-lg";
    public const string Shuffle = "shuffle";
    public const string Upload = "upload";
    public const string X = "x";
    public const string XLg = "x-lg";

    // ── Status & Feedback ──
    public const string Bell = "bell";
    public const string BellFill = "bell-fill";
    public const string CheckCircle = "check-circle";
    public const string CheckCircleFill = "check-circle-fill";
    public const string ExclamationCircle = "exclamation-circle";
    public const string ExclamationTriangle = "exclamation-triangle";
    public const string InfoCircle = "info-circle";
    public const string XCircle = "x-circle";

    // ── People & Auth ──
    public const string Lock = "lock";
    public const string People = "people";
    public const string Person = "person";
    public const string PersonFill = "person-fill";
    public const string ShieldLock = "shield-lock";

    // ── Settings & System ──
    public const string Clock = "clock";
    public const string Gear = "gear";
    public const string GearFill = "gear-fill";
    public const string Sliders = "sliders";
    public const string Translate = "translate";
    public const string Wrench = "wrench";

    // ── Content & Data ──
    public const string BarChart = "bar-chart";
    public const string CalendarEvent = "calendar-event";
    public const string CardText = "card-text";
    public const string FileText = "file-text";
    public const string FolderOpen = "folder2-open";
    public const string Folder = "folder2";
    public const string Search = "search";
    public const string SortDown = "sort-down";
    public const string SortUp = "sort-up";
    public const string Tag = "tag";
    public const string TagFill = "tag-fill";

    // ── AI & Smart Features ──
    public const string Robot = "robot";
    public const string Stars = "stars";
    public const string Magic = "magic";
    public const string Lightning = "lightning";

    // ── Misc ──
    public const string Brush = "brush";
    public const string Circle = "circle";
    public const string Eye = "eye";
    public const string EyeSlash = "eye-slash";
    public const string Filter = "filter";
    public const string HandIndexThumb = "hand-index-thumb";
    public const string HeartFill = "heart-fill";
    public const string Image = "image";
    public const string LayoutSidebar = "layout-sidebar";
    public const string LayoutTextSidebar = "layout-text-sidebar";
    public const string Palette2 = "palette2";
    public const string Pin = "pin";
    public const string Printer = "printer";
    public const string StarFill = "star-fill";
    public const string TypeH1 = "type-h1";

    /// <summary>All available icon names, sorted alphabetically.</summary>
    public static IReadOnlyList<string> All { get; } = GetAll();

    private static List<string> GetAll()
    {
        var icons = typeof(FeirbIcons)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Order()
            .ToList();
        return icons;
    }
}
