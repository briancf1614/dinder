namespace Dinder.Domain.ValueObjects;

public sealed record AIScanResult(
    float AdultScore,
    float RacyScore,
    float ViolenceScore,
    bool IsAdultContent,
    bool IsRacyContent,
    bool IsGoryContent
);
