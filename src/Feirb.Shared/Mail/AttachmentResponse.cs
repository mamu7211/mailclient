namespace Feirb.Shared.Mail;

public record AttachmentResponse(Guid Id, string Filename, long Size, string MimeType);
