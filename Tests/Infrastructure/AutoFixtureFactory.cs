namespace Tests.Infrastructure;

public static class AutoFixtureFactory
{
    public static AutoFixture.Fixture Instance { get; }

    static AutoFixtureFactory()
    {
        Instance = new AutoFixture.Fixture();
        //Otherwise AutoFixture will try to generate DateOnly with invalid values
        Instance.Customize<DateOnly>(customization => customization.FromFactory<DateTime>(dateTime => DateOnly.FromDateTime(dateTime.Date)));
    }
}