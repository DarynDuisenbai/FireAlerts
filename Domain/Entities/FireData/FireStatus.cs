namespace Domain.Entities.FireData
{
    public enum FireStatus
    {
        New = 0,           // Новый пожар
        SentToMCHS = 1,    // Передан в МЧС
        SentToVolunteers = 2 // Передан волонтерам
    }
}
