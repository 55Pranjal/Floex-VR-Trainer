using NUnit.Framework;
using Floex.Physiology;

/// <summary>
/// Unit tests for PatientState (Phase 3B, Week 4). Pure C# — no Unity required.
/// Put under Assets/Tests/EditMode/ with an EditMode asmdef referencing
/// nunit.framework. Run via Window > General > Test Runner > EditMode.
/// </summary>
public class PatientStateTests
{
    [Test]
    public void Defaults_AreRestingValues()
    {
        var p = new PatientState();
        Assert.AreEqual(70,   p.HeartRate);
        Assert.AreEqual(37.0, p.Temperature, 1e-9);
        Assert.AreEqual(0.21, p.FiO2, 1e-9);
        Assert.AreEqual(0,    p.TimeOnBypassSeconds, 1e-9);
        Assert.IsFalse(p.OnBypass);
    }

    [Test]
    public void Tick_WhenNotOnBypass_DoesNotAdvanceTime()
    {
        var p = new PatientState { OnBypass = false };
        p.Tick(0.05);
        p.Tick(0.05);
        Assert.AreEqual(0, p.TimeOnBypassSeconds, 1e-9);
    }

    [Test]
    public void Tick_WhenOnBypass_AdvancesTime()
    {
        var p = new PatientState { OnBypass = true };
        p.Tick(0.05);
        Assert.AreEqual(0.05, p.TimeOnBypassSeconds, 1e-9);
    }

    [Test]
    public void Tick_TwentyTimes_AdvancesOneSecond()
    {
        var p = new PatientState { OnBypass = true };
        for (int i = 0; i < 20; i++) p.Tick(0.05);   // 20 x 50ms = 1.0s
        Assert.AreEqual(1.0, p.TimeOnBypassSeconds, 1e-9);
    }

    [Test]
    public void Tick_NegativeDt_Throws()
    {
        var p = new PatientState { OnBypass = true };
        Assert.Throws<System.ArgumentOutOfRangeException>(() => p.Tick(-0.01));
    }

    [Test]
    public void Tick_DoesNotMutateOtherVariables()
    {
        var p = new PatientState { OnBypass = true };
        double hr = p.HeartRate, bp = p.BloodPressure, temp = p.Temperature;
        for (int i = 0; i < 100; i++) p.Tick(0.05);
        // Week 4: no physiology coupling — only time should move.
        Assert.AreEqual(hr,   p.HeartRate,    1e-9);
        Assert.AreEqual(bp,   p.BloodPressure,1e-9);
        Assert.AreEqual(temp, p.Temperature,  1e-9);
    }

    [Test]
    public void Reset_ZeroesTimeAndBypass()
    {
        var p = new PatientState { OnBypass = true };
        for (int i = 0; i < 40; i++) p.Tick(0.05);
        p.ResetToRestingDefaults();
        Assert.AreEqual(0, p.TimeOnBypassSeconds, 1e-9);
        Assert.IsFalse(p.OnBypass);
    }

    [Test]
    public void TimeOnBypassClock_FormatsMmSs()
    {
        var p = new PatientState { OnBypass = true };
        p.Tick(75.0);   // one tick of 75s — no float accumulation
        Assert.AreEqual("01:15", p.TimeOnBypassClock());
    }
}