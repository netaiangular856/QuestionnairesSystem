namespace QuestionnairesSystem.Application.Features.Notifications.DTOs;

/// <summary>Title and message in both UI languages for inbox rows.</summary>
public sealed record LocalizedInboxNotificationText(string TitleAr, string TitleEn, string MessageAr, string MessageEn);
